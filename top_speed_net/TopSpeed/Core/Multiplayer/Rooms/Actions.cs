using System;
using TopSpeed.Data;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
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
                QuestionId.No,
                HandleLeaveRoomQuestionResult,
                new QuestionButton(QuestionId.Yes, "Yes, leave this game room"),
                new QuestionButton(QuestionId.No, "No, stay in this game room", flags: QuestionButtonFlags.Default)));
        }

        private void HandleLeaveRoomQuestionResult(int resultId)
        {
            if (resultId == QuestionId.Yes)
                ConfirmLeaveRoom();
        }

        private void ConfirmLeaveRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!TrySend(session.SendRoomLeave(), "room leave request"))
                return;
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

            TrySend(session.SendRoomStartRace(), "race start request");
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

            TrySend(session.SendRoomSetTrack(RoomTrackOptions[index].Key), "track change request");
        }

        private void SetLaps(byte laps)
        {
            var session = SessionOrNull();
            if (session == null || !_roomState.IsHost || !_roomState.InRoom)
                return;
            if (laps < 1 || laps > 16)
                return;

            TrySend(session.SendRoomSetLaps(laps), "lap count change request");
        }

        private void SetPlayersToStart(byte playersToStart)
        {
            var session = SessionOrNull();
            if (session == null || !_roomState.IsHost || !_roomState.InRoom)
                return;

            if (playersToStart < 2)
                playersToStart = 2;
            TrySend(session.SendRoomSetPlayersToStart(playersToStart), "player count change request");
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

            TrySend(session.SendRoomAddBot(), "add bot request");
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

            TrySend(session.SendRoomRemoveBot(), "remove bot request");
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
            if (!TrySend(session.SendRoomPlayerReady(selectedCar, automaticTransmission), "ready state"))
                return;
            _speech.Speak("Ready. Waiting for other players.");
            _menu.ShowRoot(MultiplayerRoomControlsMenuId);
        }
    }
}
