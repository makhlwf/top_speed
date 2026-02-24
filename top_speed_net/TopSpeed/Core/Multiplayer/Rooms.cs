using System;
using System.Collections.Generic;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Speech;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public bool IsInRoom => _roomState.InRoom;

        public bool IsRoomMenu(string? currentMenuId)
        {
            if (!_roomState.InRoom)
                return false;
            return string.Equals(currentMenuId, MultiplayerRoomControlsMenuId, StringComparison.Ordinal);
        }

        public bool TryHandleEscapeFromRoomMenu(string? currentMenuId)
        {
            if (!IsRoomMenu(currentMenuId))
            {
                return false;
            }

            OpenLeaveRoomConfirmation();
            return true;
        }

        public bool TryHandleExitFromRaceLoadoutMenu(string? currentMenuId)
        {
            if (string.Equals(currentMenuId, MultiplayerLoadoutTransmissionMenuId, StringComparison.Ordinal))
            {
                _menu.ShowRoot(MultiplayerLoadoutVehicleMenuId);
                return true;
            }

            if (!string.Equals(currentMenuId, MultiplayerLoadoutVehicleMenuId, StringComparison.Ordinal))
                return false;

            _speech.Speak("Choose your vehicle and transmission mode to get ready for the race.");
            _menu.ShowRoot(MultiplayerLoadoutVehicleMenuId);
            return true;
        }

        public void ShowMultiplayerMenuAfterRace()
        {
            if (_roomState.InRoom)
                _menu.ShowRoot(MultiplayerRoomControlsMenuId);
            else
                _menu.ShowRoot(MultiplayerLobbyMenuId);
        }

        public void BeginRaceLoadoutSelection()
        {
            if (!_roomState.InRoom)
                return;

            _pendingLoadoutVehicleIndex = 0;
            RebuildLoadoutVehicleMenu();
            RebuildLoadoutTransmissionMenu();
            _menu.ShowRoot(MultiplayerLoadoutVehicleMenuId);
            _enterMenuState();
        }

        private void RebuildLobbyMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Create a new game room", MenuAction.None, onActivate: OpenCreateRoomMenu),
                new MenuItem("Join an existing game", MenuAction.None, onActivate: OpenRoomBrowser),
                new MenuItem("Options", MenuAction.None, nextMenuId: "options_main"),
                new MenuItem("Disconnect from server", MenuAction.None, onActivate: Disconnect)
            };

            _menu.UpdateItems(MultiplayerLobbyMenuId, items);
        }

        private void RebuildCreateRoomMenu()
        {
            var items = new List<MenuItem>
            {
                new RadioButton(
                    "Game type",
                    RoomTypeOptions,
                    GetCreateRoomTypeIndex,
                    SetCreateRoomType,
                    hint: "Choose whether this room is a race with bots or a one-on-one game. Use LEFT or RIGHT to change."),
                new RadioButton(
                    "Maximum players allowed in this room",
                    PlayerCountOptions,
                    GetCreateRoomPlayersToStartIndex,
                    SetCreateRoomPlayersToStart,
                    hint: "Choose the player capacity from 1 to 10. Use LEFT or RIGHT to change."),
                new MenuItem(
                    () => string.IsNullOrWhiteSpace(_createRoomName)
                        ? "Room name, currently automatic"
                        : $"Room name, currently {_createRoomName}",
                    MenuAction.None,
                    onActivate: UpdateCreateRoomName,
                    hint: "Press ENTER to enter a room name. Leave it empty to use an automatic name."),
                new MenuItem("Create this game room", MenuAction.None, onActivate: ConfirmCreateRoom),
                new MenuItem("Cancel room creation", MenuAction.Back)
            };

            _menu.UpdateItems(MultiplayerCreateRoomMenuId, items);
        }

        private void RebuildRoomControlsMenu()
        {
            var items = new List<MenuItem>();
            if (!_roomState.InRoom)
            {
                items.Add(new MenuItem("You are not currently inside a game room.", MenuAction.None));
                items.Add(new MenuItem("Return to multiplayer lobby", MenuAction.None, onActivate: () => _menu.ShowRoot(MultiplayerLobbyMenuId)));
                _menu.UpdateItems(MultiplayerRoomControlsMenuId, items);
                return;
            }

            if (_roomState.IsHost)
                items.Add(new MenuItem("Start this game now", MenuAction.None, onActivate: StartGame));
            if (_roomState.IsHost)
                items.Add(new MenuItem("Change game options", MenuAction.None, nextMenuId: "multiplayer_room_options"));
            if (_roomState.IsHost && _roomState.RoomType == GameRoomType.BotsRace)
                items.Add(new MenuItem("Add a bot to this game room", MenuAction.None, onActivate: AddBotToRoom));
            if (_roomState.IsHost && _roomState.RoomType == GameRoomType.BotsRace)
                items.Add(new MenuItem("Remove the last bot that was added", MenuAction.None, onActivate: RemoveLastBotFromRoom));
            items.Add(new MenuItem("Who is currently present in this game room", MenuAction.None, onActivate: SpeakPresentPlayers));
            items.Add(new MenuItem("Leave this game room", MenuAction.None, onActivate: OpenLeaveRoomConfirmation));
            _menu.UpdateItems(MultiplayerRoomControlsMenuId, items);
        }

        private void RebuildRoomOptionsMenu()
        {
            var items = new List<MenuItem>();
            if (!_roomState.InRoom)
            {
                items.Add(new MenuItem("You are not currently inside a game room.", MenuAction.None));
                items.Add(new MenuItem("Return to room controls", MenuAction.Back));
                _menu.UpdateItems(MultiplayerRoomOptionsMenuId, items);
                return;
            }

            if (!_roomState.IsHost)
            {
                items.Add(new MenuItem("Only the host can change game options.", MenuAction.None));
                items.Add(new MenuItem("Return to room controls", MenuAction.Back));
                _menu.UpdateItems(MultiplayerRoomOptionsMenuId, items);
                return;
            }

            items.Add(new RadioButton(
                "Track",
                RoomTrackLabels,
                GetCurrentRoomTrackIndex,
                SetRoomTrackByIndex,
                hint: "Choose which track this room will use. Use LEFT or RIGHT to change."));

            items.Add(new RadioButton(
                "Number of laps",
                LapCountOptions,
                () => Math.Max(0, Math.Min(LapCountOptions.Length - 1, (_roomState.Laps > 0 ? _roomState.Laps : (byte)1) - 1)),
                value => SetLaps((byte)(value + 1)),
                hint: "Choose the number of laps for this room. Use LEFT or RIGHT to change."));

            items.Add(new RadioButton(
                "Players required before the host can start",
                PlayerCountOptions,
                () => Math.Max(0, Math.Min(PlayerCountOptions.Length - 1, (_roomState.PlayersToStart > 0 ? _roomState.PlayersToStart : (byte)1) - 1)),
                value => SetPlayersToStart((byte)(value + 1)),
                hint: "Select how many players are required before the host can start this game. Use LEFT or RIGHT to change."));

            items.Add(new MenuItem("Return to room controls", MenuAction.Back));
            var preserveSelection = string.Equals(_menu.CurrentId, MultiplayerRoomOptionsMenuId, StringComparison.Ordinal);
            _menu.UpdateItems(MultiplayerRoomOptionsMenuId, items, preserveSelection);
        }

        private void RebuildLoadoutVehicleMenu()
        {
            var items = new List<MenuItem>();
            for (var i = 0; i < VehicleCatalog.VehicleCount; i++)
            {
                var vehicleIndex = i;
                var vehicleName = VehicleCatalog.Vehicles[i].Name;
                items.Add(new MenuItem(vehicleName, MenuAction.None, nextMenuId: MultiplayerLoadoutTransmissionMenuId, onActivate: () => _pendingLoadoutVehicleIndex = vehicleIndex));
            }

            items.Add(new MenuItem("Random vehicle", MenuAction.None, nextMenuId: MultiplayerLoadoutTransmissionMenuId, onActivate: () => _pendingLoadoutVehicleIndex = Algorithm.RandomInt(VehicleCatalog.VehicleCount)));
            _menu.UpdateItems(MultiplayerLoadoutVehicleMenuId, items);
        }

        private void RebuildLoadoutTransmissionMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Automatic transmission", MenuAction.None, onActivate: () => SubmitLoadoutReady(true)),
                new MenuItem("Manual transmission", MenuAction.None, onActivate: () => SubmitLoadoutReady(false)),
                new MenuItem("Random transmission mode", MenuAction.None, onActivate: () => SubmitLoadoutReady(Algorithm.RandomInt(2) == 0)),
                new MenuItem("Go back to vehicle selection", MenuAction.Back)
            };
            _menu.UpdateItems(MultiplayerLoadoutTransmissionMenuId, items);
        }

        private void OpenRoomBrowser()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (_roomBrowserOpenPending)
                return;

            _roomBrowserOpenPending = true;
            session.SendRoomListRequest();
        }

        private void OpenCreateRoomMenu()
        {
            if (SessionOrNull() == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            ResetCreateRoomDraft();
            RebuildCreateRoomMenu();
            _menu.Push(MultiplayerCreateRoomMenuId);
        }

        private void UpdateCreateRoomName()
        {
            var result = _promptTextInput(
                "Enter a room name. Leave this field empty to use an automatic room name.",
                _createRoomName,
                SpeechService.SpeakFlag.None,
                true);

            if (result.Cancelled)
                return;

            _createRoomName = (result.Text ?? string.Empty).Trim();
            RebuildCreateRoomMenu();

            if (string.IsNullOrWhiteSpace(_createRoomName))
            {
                _speech.Speak("Automatic room name selected.");
                return;
            }

            _speech.Speak($"Room name set to {_createRoomName}.");
        }

        private void ConfirmCreateRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            var playersToStart = _createRoomPlayersToStart;
            if (playersToStart < 1 || playersToStart > ProtocolConstants.MaxRoomPlayersToStart)
                playersToStart = 2;

            session.SendRoomCreate(_createRoomName, _createRoomType, playersToStart);
            _speech.Speak("Creating game room.");
            _menu.ShowRoot(MultiplayerLobbyMenuId);
        }

        private int GetCreateRoomTypeIndex()
        {
            return _createRoomType == GameRoomType.OneOnOne ? 1 : 0;
        }

        private void SetCreateRoomType(int index)
        {
            _createRoomType = index == 1 ? GameRoomType.OneOnOne : GameRoomType.BotsRace;
        }

        private int GetCreateRoomPlayersToStartIndex()
        {
            var playersToStart = _createRoomPlayersToStart;
            if (playersToStart < 1 || playersToStart > ProtocolConstants.MaxRoomPlayersToStart)
                playersToStart = 2;
            return playersToStart - 1;
        }

        private void SetCreateRoomPlayersToStart(int index)
        {
            var playersToStart = (byte)(index + 1);
            if (playersToStart < 1 || playersToStart > ProtocolConstants.MaxRoomPlayersToStart)
                return;
            _createRoomPlayersToStart = playersToStart;
        }

        private void ResetCreateRoomDraft()
        {
            _createRoomType = GameRoomType.BotsRace;
            _createRoomPlayersToStart = 2;
            _createRoomName = string.Empty;
        }

        private void JoinRoom(uint roomId)
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            session.SendRoomJoin(roomId);
        }

        private void OpenLeaveRoomConfirmation()
        {
            if (!_roomState.InRoom)
            {
                _speech.Speak("You are not currently inside a game room.");
                return;
            }

            if (_questions.IsQuestionMenu(_menu.CurrentId))
                return;

            _questions.Show(new Question(
                "Leave this game room?",
                "Are you sure you want to leave the current room?",
                new QuestionButton("Yes, leave this game room", ConfirmLeaveRoom),
                new QuestionButton("No, stay in this game room", () => _menu.PopToPrevious(), QuestionButtonFlags.Default)));
        }

        private void ConfirmLeaveRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            session.SendRoomLeave();
            _speech.Speak("Leaving game room.");
            _menu.ShowRoot(MultiplayerLobbyMenuId);
        }

        private void StartGame()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_roomState.InRoom || !_roomState.IsHost)
            {
                _speech.Speak("Only the host can start the game.");
                return;
            }

            session.SendRoomStartRace();
        }

        private int GetCurrentRoomTrackIndex()
        {
            var currentTrack = string.IsNullOrWhiteSpace(_roomState.TrackName) ? TrackList.RaceTracks[0].Key : _roomState.TrackName;
            for (var i = 0; i < RoomTrackOptions.Length; i++)
            {
                if (string.Equals(RoomTrackOptions[i].Key, currentTrack, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return 0;
        }

        private void SetRoomTrackByIndex(int index)
        {
            var session = SessionOrNull();
            if (session == null)
                return;
            if (!_roomState.InRoom || !_roomState.IsHost)
                return;
            if (index < 0 || index >= RoomTrackOptions.Length)
                return;

            session.SendRoomSetTrack(RoomTrackOptions[index].Key);
        }

        private void SetLaps(byte laps)
        {
            var session = SessionOrNull();
            if (session == null || !_roomState.IsHost || !_roomState.InRoom)
                return;
            if (laps < 1 || laps > 16)
                return;

            session.SendRoomSetLaps(laps);
        }

        private void SetPlayersToStart(byte playersToStart)
        {
            var session = SessionOrNull();
            if (session == null || !_roomState.IsHost || !_roomState.InRoom)
                return;

            session.SendRoomSetPlayersToStart(playersToStart);
        }

        private void AddBotToRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_roomState.InRoom || !_roomState.IsHost || _roomState.RoomType != GameRoomType.BotsRace)
            {
                _speech.Speak("Bots can only be managed by the host in race-with-bots rooms.");
                return;
            }

            session.SendRoomAddBot();
        }

        private void RemoveLastBotFromRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_roomState.InRoom || !_roomState.IsHost || _roomState.RoomType != GameRoomType.BotsRace)
            {
                _speech.Speak("Bots can only be managed by the host in race-with-bots rooms.");
                return;
            }

            session.SendRoomRemoveBot();
        }

        private void SubmitLoadoutReady(bool automaticTransmission)
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_roomState.InRoom)
            {
                _speech.Speak("You are not in a game room.");
                return;
            }

            var vehicleIndex = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, _pendingLoadoutVehicleIndex));
            var selectedCar = (CarType)vehicleIndex;
            _setLocalMultiplayerLoadout(vehicleIndex, automaticTransmission);
            session.SendRoomPlayerReady(selectedCar, automaticTransmission);
            _speech.Speak("Ready. Waiting for other players.");
            _menu.ShowRoot(MultiplayerRoomControlsMenuId);
        }

        private void SpeakPresentPlayers()
        {
            if (!_roomState.InRoom)
            {
                _speech.Speak("You are not in a game room.");
                return;
            }

            if (_roomState.Players == null || _roomState.Players.Length == 0)
            {
                _speech.Speak("No players are in this game.");
                return;
            }

            var parts = new List<string>();
            foreach (var player in _roomState.Players)
            {
                var name = string.IsNullOrWhiteSpace(player.Name) ? $"Player {player.PlayerNumber + 1}" : player.Name;
                if (player.PlayerId == _roomState.HostPlayerId)
                    parts.Add($"{name}, host");
                else
                    parts.Add(name);
            }

            _speech.Speak(string.Join(". ", parts));
        }

        private void Disconnect()
        {
            _clearSession();
            _speech.Speak("Disconnected from server.");
            _menu.ShowRoot("main");
            _enterMenuState();
        }

        private void UpdateRoomBrowserMenu()
        {
            var items = new List<MenuItem>();
            var rooms = _roomList.Rooms ?? Array.Empty<PacketRoomSummary>();
            if (rooms.Length == 0)
            {
                items.Add(new MenuItem("No game rooms found", MenuAction.None));
            }
            else
            {
                foreach (var room in rooms)
                {
                    var roomCopy = room;
                    var typeText = roomCopy.RoomType == GameRoomType.OneOnOne ? "one-on-one" : "race with bots";
                    var label = $"{typeText} game with {roomCopy.PlayerCount} people";
                    label += $", maximum {roomCopy.PlayersToStart} players";
                    if (roomCopy.RaceStarted)
                        label += ", in progress";
                    else if (roomCopy.PlayerCount >= roomCopy.PlayersToStart)
                        label += ", room is full";
                    items.Add(new MenuItem(label, MenuAction.None, onActivate: () => JoinRoom(roomCopy.RoomId)));
                }
            }

            items.Add(new MenuItem("Return to multiplayer lobby", MenuAction.Back));
            _menu.UpdateItems(MultiplayerRoomBrowserMenuId, items);
        }
    }
}
