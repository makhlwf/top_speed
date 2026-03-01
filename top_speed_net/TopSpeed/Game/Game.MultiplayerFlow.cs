using System;
using SharpDX.DirectInput;
using TopSpeed.Data;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Race;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void SetSession(MultiplayerSession session)
        {
            _session = session;
            ClearQueuedMultiplayerPackets();
            session.SetPacketSink(packet => EnqueueMultiplayerPacket(session, packet));
        }

        private MultiplayerSession? GetSession()
        {
            return _session;
        }

        private void ClearSession()
        {
            var session = _session;
            if (session != null)
                session.SetPacketSink(null);
            session?.Dispose();
            _session = null;
            ClearQueuedMultiplayerPackets();
            _multiplayerCoordinator.OnSessionCleared();
        }

        private void ResetPendingMultiplayerState()
        {
            _pendingMultiplayerTrack = null;
            _pendingMultiplayerTrackName = string.Empty;
            _pendingMultiplayerLaps = 0;
            _pendingMultiplayerStart = false;
            _multiplayerVehicleIndex = 0;
            _multiplayerAutomaticTransmission = true;
        }

        private void SetMultiplayerLoadout(int vehicleIndex, bool automaticTransmission)
        {
            _multiplayerVehicleIndex = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, vehicleIndex));
            _multiplayerAutomaticTransmission = automaticTransmission;
        }

        private void DisconnectFromServer()
        {
            _multiplayerRace?.FinalizeLevelMultiplayer();
            _multiplayerRace?.Dispose();
            _multiplayerRace = null;
            _multiplayerRaceQuitConfirmActive = false;

            ResetPendingMultiplayerState();
            ClearSession();
            _state = AppState.Menu;
            _menu.ShowRoot("main");
            _menu.FadeInMenuMusic();
        }

        private void RunMultiplayerRace(float elapsed)
        {
            if (_multiplayerRace == null)
            {
                EndMultiplayerRace();
                return;
            }

            ProcessMultiplayerPackets();
            if (_multiplayerRace == null)
                return;

            _multiplayerRace.Run(elapsed);
            if (_multiplayerRace.WantsExit)
            {
                EndMultiplayerRace();
                return;
            }

            if (_multiplayerRaceQuitConfirmActive)
            {
                var action = _menu.Update(_input);
                HandleMenuAction(action);
                return;
            }

            if (_input.WasPressed(Key.Escape))
                OpenMultiplayerRaceQuitConfirmation();
        }

        private void ProcessMultiplayerPackets()
        {
            while (_queuedMultiplayerPackets.TryDequeue(out var queued))
            {
                if (!ReferenceEquals(_session, queued.Session))
                    continue;

                _mpPktReg.TryDispatch(queued.Packet);
                if (!ReferenceEquals(_session, queued.Session))
                    return;
            }
        }

        private void EnqueueMultiplayerPacket(MultiplayerSession session, IncomingPacket packet)
        {
            _queuedMultiplayerPackets.Enqueue(new QueuedIncomingPacket(session, packet));
        }

        private void ClearQueuedMultiplayerPackets()
        {
            while (_queuedMultiplayerPackets.TryDequeue(out _))
            {
            }
        }

        private void RegisterMultiplayerPacketHandlers()
        {
            RegisterMultiplayerControlPacketHandlers();
            RegisterMultiplayerRoomPacketHandlers();
            RegisterMultiplayerRaceStatePacketHandlers();
            RegisterMultiplayerRaceEventPacketHandlers();
            RegisterMultiplayerMediaPacketHandlers();
            RegisterMultiplayerChatPacketHandlers();
        }

        private void StartMultiplayerRace()
        {
            if (_session == null)
                return;
            if (_multiplayerRace != null)
                return;
            if (_pendingMultiplayerTrack == null)
            {
                _pendingMultiplayerStart = true;
                return;
            }

            _pendingMultiplayerStart = false;
            FadeOutMenuMusic();
            var trackName = string.IsNullOrWhiteSpace(_pendingMultiplayerTrackName) ? "custom" : _pendingMultiplayerTrackName;
            var laps = _pendingMultiplayerLaps > 0 ? _pendingMultiplayerLaps : _settings.NrOfLaps;
            var vehicleIndex = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, _multiplayerVehicleIndex));
            var automatic = _multiplayerAutomaticTransmission;

            _multiplayerRace?.FinalizeLevelMultiplayer();
            _multiplayerRace?.Dispose();
            _multiplayerRace = new LevelMultiplayer(
                _audio,
                _speech,
                _settings,
                _raceInput,
                _pendingMultiplayerTrack!,
                trackName,
                automatic,
                laps,
                vehicleIndex,
                null,
                _input.VibrationDevice,
                _session,
                _session.PlayerId,
                _session.PlayerNumber);
            _multiplayerRace.Initialize();
            _state = AppState.MultiplayerRace;
        }

        private void EndMultiplayerRace()
        {
            _multiplayerRace?.FinalizeLevelMultiplayer();
            _multiplayerRace?.Dispose();
            _multiplayerRace = null;
            _multiplayerRaceQuitConfirmActive = false;

            if (_session != null)
            {
                TrySendSession(_session.SendPlayerState(PlayerState.NotReady), "not-ready state");
                _state = AppState.Menu;
                _multiplayerCoordinator.ShowMultiplayerMenuAfterRace();
            }
            else
            {
                _state = AppState.Menu;
                _menu.ShowRoot("main");
                _menu.FadeInMenuMusic();
            }
        }

        private void OpenMultiplayerRaceQuitConfirmation()
        {
            if (_multiplayerRace == null)
                return;
            if (_multiplayerRaceQuitConfirmActive)
                return;
            if (_multiplayerCoordinator.Questions.IsQuestionMenu(_menu.CurrentId))
                return;

            _multiplayerRaceQuitConfirmActive = true;

            var question = new Question(
                "Quit race?",
                "Are you sure you want to quit this multiplayer race?",
                QuestionId.No,
                HandleMultiplayerRaceQuitQuestionResult,
                new QuestionButton(QuestionId.Yes, "Yes, quit the race"),
                new QuestionButton(QuestionId.No, "No, continue racing", flags: QuestionButtonFlags.Default))
            {
                OpenAsOverlay = true
            };
            _multiplayerCoordinator.Questions.Show(question);
        }

        private void HandleMultiplayerRaceQuitQuestionResult(int resultId)
        {
            if (resultId == QuestionId.Yes)
                ConfirmQuitMultiplayerRace();
            else if (resultId == QuestionId.No || resultId == QuestionId.Cancel || resultId == QuestionId.Close)
                CancelMultiplayerRaceQuitConfirmation();
        }

        private void CancelMultiplayerRaceQuitConfirmation()
        {
            if (!_multiplayerRaceQuitConfirmActive)
                return;

            _multiplayerRaceQuitConfirmActive = false;
        }

        private void ConfirmQuitMultiplayerRace()
        {
            if (!_multiplayerRaceQuitConfirmActive)
                return;

            _multiplayerRaceQuitConfirmActive = false;
            if (_session != null)
                TrySendSession(_session.SendRoomLeave(), "room leave request");

            _multiplayerRace?.FinalizeLevelMultiplayer();
            _multiplayerRace?.Dispose();
            _multiplayerRace = null;

            ResetPendingMultiplayerState();
            _state = AppState.Menu;
            _menu.ShowRoot("multiplayer_lobby");
        }

        private bool TrySendSession(bool sent, string action)
        {
            if (sent)
                return true;

            _speech.Speak($"Failed to send {action}. Please check your connection.");
            return false;
        }
    }
}
