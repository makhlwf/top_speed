using System;
using System.Collections.Generic;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Menu;
using TopSpeed.Protocol;
using TopSpeed.Localization;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void RebuildLobbyMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Create a new game room"), MenuAction.None, onActivate: OpenCreateRoomMenu),
                new MenuItem(LocalizationService.Mark("Join an existing game"), MenuAction.None, onActivate: OpenRoomBrowser),
                new MenuItem(LocalizationService.Mark("Who is online"), MenuAction.None, onActivate: OpenOnlinePlayersMenu),
                new MenuItem(LocalizationService.Mark("Options"), MenuAction.None, nextMenuId: "options_main"),
                new MenuItem(LocalizationService.Mark("Disconnect from server"), MenuAction.None, flags: MenuItemFlags.Close)
            };

            _menu.UpdateItems(MultiplayerMenuKeys.Lobby, items);
        }

        private void RebuildRoomControlsMenu()
        {
            var items = new List<MenuItem>();
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                items.Add(new MenuItem(LocalizationService.Mark("You are not currently inside a game room."), MenuAction.None));
                _menu.UpdateItems(MultiplayerMenuKeys.RoomControls, items);
                return;
            }

            if (_state.Rooms.CurrentRoom.IsHost)
                items.Add(new MenuItem(LocalizationService.Mark("Start the game"), MenuAction.None, onActivate: StartGame));
            if (_state.Rooms.CurrentRoom.IsHost)
                items.Add(new MenuItem(LocalizationService.Mark("Change game options"), MenuAction.None, onActivate: OpenRoomOptionsMenu));
            if (_state.Rooms.CurrentRoom.IsHost && _state.Rooms.CurrentRoom.RoomType == GameRoomType.BotsRace)
                items.Add(new MenuItem(LocalizationService.Mark("Add a bot"), MenuAction.None, onActivate: AddBotToRoom));
            if (_state.Rooms.CurrentRoom.IsHost && _state.Rooms.CurrentRoom.RoomType == GameRoomType.BotsRace)
                items.Add(new MenuItem(LocalizationService.Mark("Remove a bot"), MenuAction.None, onActivate: RemoveLastBotFromRoom));
            items.Add(new MenuItem(LocalizationService.Mark("View game rules"), MenuAction.None, onActivate: AnnounceCurrentRoomGameRules));
            items.Add(new MenuItem(LocalizationService.Mark("Who is currently present in this game room"), MenuAction.None, onActivate: OpenRoomPlayersMenu));
            items.Add(new MenuItem(LocalizationService.Mark("Leave this game room"), MenuAction.None, flags: MenuItemFlags.Close));
            _menu.UpdateItems(MultiplayerMenuKeys.RoomControls, items);
        }

        private void RebuildRoomOptionsMenu()
        {
            var items = new List<MenuItem>();
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                items.Add(new MenuItem(LocalizationService.Mark("You are not currently inside a game room."), MenuAction.None));
                _menu.UpdateItems(MultiplayerMenuKeys.RoomOptions, items);
                return;
            }

            if (!_state.Rooms.CurrentRoom.IsHost)
            {
                items.Add(new MenuItem(LocalizationService.Mark("Only the host can change game options."), MenuAction.None));
                _menu.UpdateItems(MultiplayerMenuKeys.RoomOptions, items);
                return;
            }

            items.Add(new MenuItem(LocalizationService.Mark("Game rules"), MenuAction.None, onActivate: OpenRoomGameRulesMenu));

            items.Add(new MenuItem(
                () => GetRoomOptionsTrackText(),
                MenuAction.None,
                onActivate: OpenRoomTrackTypeMenu,
                hint: InteractionHints.ForPlatform(
                    LocalizationService.Mark("Press ENTER to change the track used during the race."),
                    LocalizationService.Mark("Swipe up to change the track used during the race."))));

            items.Add(new RadioButton(LocalizationService.Mark("Number of laps"),
                LapCountOptions,
                GetRoomOptionsLapsIndex,
                value => SetRoomOptionsLaps((byte)(value + 1)),
                hint: InteractionHints.ForPlatform(
                    LocalizationService.Mark("Choose the number of laps for this room. Use LEFT or RIGHT to change."),
                    LocalizationService.Mark("Choose the number of laps for this room. Swipe left or right with two fingers to change."))));

            var maxPlayersItem = new RadioButton(LocalizationService.Mark("Maximum players allowed in this room"),
                RoomCapacityOptions,
                GetRoomOptionsPlayersToStartIndex,
                value => SetRoomOptionsPlayersToStart((byte)(value + 2)),
                hint: InteractionHints.ForPlatform(
                    LocalizationService.Mark("Select the player capacity for this room. The host can start with fewer players than the specified maximum players, so this is not a hard requirement. Use LEFT or RIGHT to change."),
                    LocalizationService.Mark("Select the player capacity for this room. The host can start with fewer players than the specified maximum players, so this is not a hard requirement. Swipe left or right with two fingers to change.")))
            {
                Hidden = _state.Rooms.CurrentRoom.RoomType == GameRoomType.OneOnOne
            };
            items.Add(maxPlayersItem);

            items.Add(new MenuItem(LocalizationService.Mark("Confirm game options"), MenuAction.None, onActivate: ConfirmRoomOptionsChanges));
            items.Add(new MenuItem(LocalizationService.Mark("Cancel and discard changes"), MenuAction.Back, onActivate: CancelRoomOptionsChanges));
            var preserveSelection = string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomOptions, StringComparison.Ordinal);
            _menu.UpdateItems(MultiplayerMenuKeys.RoomOptions, items, preserveSelection);
        }

        private void RebuildRoomGameRulesMenu()
        {
            var items = new List<MenuItem>();
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                items.Add(new MenuItem(LocalizationService.Mark("You are not currently inside a game room."), MenuAction.None));
                _menu.UpdateItems(MultiplayerMenuKeys.RoomGameRules, items);
                return;
            }

            if (!_state.Rooms.CurrentRoom.IsHost)
            {
                items.Add(new MenuItem(LocalizationService.Mark("Only the host can change game rules."), MenuAction.None));
                _menu.UpdateItems(MultiplayerMenuKeys.RoomGameRules, items);
                return;
            }

            items.Add(new CheckBox(
                LocalizationService.Mark("Ghost mode"),
                GetRoomOptionsGhostModeEnabled,
                SetRoomOptionsGhostModeEnabled,
                hint: LocalizationService.Mark("When enabled, vehicle collisions are disabled and vehicles can pass through each other.")));

            var preserveSelection = string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomGameRules, StringComparison.Ordinal);
            _menu.UpdateItems(MultiplayerMenuKeys.RoomGameRules, items, preserveSelection);
        }

        private void RebuildLoadoutVehicleMenu()
        {
            var items = new List<MenuItem>();
            for (var i = 0; i < VehicleCatalog.VehicleCount; i++)
            {
                var vehicleIndex = i;
                var vehicleName = VehicleCatalog.Vehicles[i].Name;
                items.Add(new MenuItem(vehicleName, MenuAction.None, onActivate: () => CompleteLoadoutVehicleSelection(vehicleIndex)));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Random vehicle"), MenuAction.None, onActivate: () => CompleteLoadoutVehicleSelection(Algorithm.RandomInt(VehicleCatalog.VehicleCount))));
            _menu.UpdateItems(MultiplayerMenuKeys.LoadoutVehicle, items);
        }

        private void RebuildLoadoutTransmissionMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Automatic transmission"), MenuAction.None, onActivate: () => SubmitLoadoutReady(true)),
                new MenuItem(LocalizationService.Mark("Manual transmission"), MenuAction.None, onActivate: () => SubmitLoadoutReady(false)),
                new MenuItem(LocalizationService.Mark("Random transmission mode"), MenuAction.None, onActivate: () => SubmitLoadoutReady(PickRandomLoadoutTransmission(_state.RoomDrafts.PendingLoadoutVehicleIndex)))
            };
            _menu.UpdateItems(MultiplayerMenuKeys.LoadoutTransmission, items);
        }

        private void RebuildRoomPlayersMenu()
        {
            var items = new List<MenuItem>();
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                items.Add(new MenuItem(LocalizationService.Mark("You are not currently inside a game room."), MenuAction.None));
                _menu.UpdateItems(MultiplayerMenuKeys.RoomPlayers, items);
                return;
            }

            var players = _state.Rooms.CurrentRoom.Players ?? Array.Empty<RoomParticipant>();
            if (players.Length == 0)
            {
                items.Add(new MenuItem(LocalizationService.Mark("No players are currently in this game room."), MenuAction.None));
            }
            else
            {
                foreach (var player in players)
                {
                    var name = string.IsNullOrWhiteSpace(player.Name)
                        ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1)
                        : player.Name;
                    var label = player.PlayerId == _state.Rooms.CurrentRoom.HostPlayerId
                        ? LocalizationService.Format(LocalizationService.Mark("{0}, host"), name)
                        : name;
                    items.Add(new MenuItem(label, MenuAction.None));
                }
            }

            var preserveSelection = string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomPlayers, StringComparison.Ordinal);
            _menu.UpdateItems(MultiplayerMenuKeys.RoomPlayers, items, preserveSelection);
        }

        private void OpenRoomPlayersMenu()
        {
            RebuildRoomPlayersMenu();
            _menu.Push(MultiplayerMenuKeys.RoomPlayers);
        }
    }
}







