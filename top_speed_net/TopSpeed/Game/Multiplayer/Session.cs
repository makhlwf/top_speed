using System;
using TopSpeed.Data;
using TopSpeed.Network;

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
            _multiplayerRace?.FinalizeMultiplayerMode();
            _multiplayerRace?.Dispose();
            _multiplayerRace = null;
            _multiplayerRaceQuitConfirmActive = false;

            ResetPendingMultiplayerState();
            ClearSession();
            _state = AppState.Menu;
            _menu.ShowRoot("main");
            _menu.FadeInMenuMusic();
        }
    }
}

