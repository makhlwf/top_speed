using System;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;

using TopSpeed.Localization;
namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private const int QuitLoadoutQuestionYesId = 2001;
        private const int QuitLoadoutQuestionNoId = 2002;

        private void OpenLeaveRoomConfirmation()
        {
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak(LocalizationService.Mark("You are not currently inside a game room."));
                return;
            }

            if (_questions.IsQuestionMenu(_menu.CurrentId))
                return;

            _questions.Show(new Question(LocalizationService.Mark("Leave this game room?"),
                LocalizationService.Mark("Are you sure you want to leave the current room?"),
                QuestionId.No,
                HandleLeaveRoomQuestionResult,
                new QuestionButton(QuestionId.Yes, LocalizationService.Mark("Yes, leave this game room")),
                new QuestionButton(QuestionId.No, LocalizationService.Mark("No, stay in this game room"), flags: QuestionButtonFlags.Default)));
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
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!TrySend(session.SendRoomLeave(), "room leave request"))
                return;
            _speech.Speak(LocalizationService.Mark("Leaving game room."));
            _menu.ShowRoot(MultiplayerMenuKeys.Lobby);
        }

        private void StartGame()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost)
            {
                _speech.Speak(LocalizationService.Mark("Only the host can start the game."));
                return;
            }

            TrySend(session.SendRoomStartRace(), "race start request");
        }

        private void AddBotToRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost || _state.Rooms.CurrentRoom.RoomType != GameRoomType.BotsRace)
            {
                _speech.Speak(LocalizationService.Mark("Bots can only be managed by the host in race-with-bots rooms."));
                return;
            }

            TrySend(session.SendRoomAddBot(), "add bot request");
        }

        private void RemoveLastBotFromRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost || _state.Rooms.CurrentRoom.RoomType != GameRoomType.BotsRace)
            {
                _speech.Speak(LocalizationService.Mark("Bots can only be managed by the host in race-with-bots rooms."));
                return;
            }

            TrySend(session.SendRoomRemoveBot(), "remove bot request");
        }

        private void SubmitLoadoutReady(bool automaticTransmission)
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak(LocalizationService.Mark("You are not in a game room."));
                return;
            }

            var vehicleIndex = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, _state.RoomDrafts.PendingLoadoutVehicleIndex));
            var parameters = VehicleCatalog.Vehicles[vehicleIndex];
            if (!TransmissionSelect.TryResolveRequested(
                    automaticRequested: automaticTransmission,
                    primary: parameters.PrimaryTransmissionType,
                    supported: parameters.SupportedTransmissionTypes,
                    out _))
            {
                _speech.Speak(LocalizationService.Mark("This vehicle does not support the selected transmission mode."));
                return;
            }

            var selectedCar = (CarType)vehicleIndex;
            _setLocalMultiplayerLoadout(vehicleIndex, automaticTransmission);
            if (!TrySend(session.SendRoomPlayerReady(selectedCar, automaticTransmission), "ready state"))
                return;
            _speech.Speak(LocalizationService.Mark("Ready. Waiting for other players."));
            _menu.ShowRoot(MultiplayerMenuKeys.RoomControls);
        }

        private void CompleteLoadoutVehicleSelection(int vehicleIndex)
        {
            vehicleIndex = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, vehicleIndex));
            _state.RoomDrafts.PendingLoadoutVehicleIndex = vehicleIndex;
            if (TryResolveSingleLoadoutTransmission(vehicleIndex, out var automaticTransmission))
            {
                SubmitLoadoutReady(automaticTransmission);
                return;
            }

            _menu.Push(MultiplayerMenuKeys.LoadoutTransmission);
        }

        private static bool TryResolveSingleLoadoutTransmission(int vehicleIndex, out bool automaticTransmission)
        {
            automaticTransmission = true;
            vehicleIndex = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, vehicleIndex));
            var parameters = VehicleCatalog.Vehicles[vehicleIndex];
            return TransmissionSelect.TryResolveSingleMode(
                parameters.PrimaryTransmissionType,
                parameters.SupportedTransmissionTypes,
                out automaticTransmission);
        }

        private bool PickRandomLoadoutTransmission(int vehicleIndex)
        {
            vehicleIndex = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, vehicleIndex));
            var parameters = VehicleCatalog.Vehicles[vehicleIndex];
            var supportsAutomatic = TransmissionSelect.SupportsAutomatic(parameters.SupportedTransmissionTypes);
            var supportsManual = TransmissionSelect.SupportsManual(parameters.SupportedTransmissionTypes);
            if (supportsAutomatic && supportsManual)
                return Algorithm.RandomInt(2) == 0;
            if (supportsManual)
                return false;
            return true;
        }

        private void OpenLoadoutExitConfirmation()
        {
            if (_questions.IsQuestionMenu(_menu.CurrentId))
                return;

            _questions.Show(new Question(LocalizationService.Mark("Quit race preparation?"),
                LocalizationService.Mark("Do you want to quit race preparation and stay in this game room?"),
                QuitLoadoutQuestionNoId,
                HandleQuitLoadoutQuestionResult,
                new QuestionButton(QuitLoadoutQuestionYesId, LocalizationService.Mark("Yes, quit race preparation")),
                new QuestionButton(QuitLoadoutQuestionNoId, LocalizationService.Mark("No, continue preparing"), flags: QuestionButtonFlags.Default)));
        }

        private void HandleQuitLoadoutQuestionResult(int resultId)
        {
            if (resultId == QuitLoadoutQuestionYesId)
                ConfirmQuitLoadout();
            else
                _menu.ShowRoot(MultiplayerMenuKeys.LoadoutVehicle);
        }

        private void ConfirmQuitLoadout()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak(LocalizationService.Mark("You are not in a game room."));
                return;
            }

            if (_state.Rooms.CurrentRoom.RaceState == RoomRaceState.Preparing)
            {
                if (!TrySend(session.SendRoomPlayerWithdraw(), "race preparation withdrawal"))
                    return;
                _speech.Speak(LocalizationService.Mark("You left race preparation and returned to room controls."));
            }
            else
            {
                _speech.Speak(LocalizationService.Mark("Returned to room controls."));
            }

            _menu.ShowRoot(MultiplayerMenuKeys.RoomControls);
        }
    }
}








