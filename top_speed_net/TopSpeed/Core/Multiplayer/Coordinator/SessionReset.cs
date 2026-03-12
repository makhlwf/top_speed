using System;
using TopSpeed.Input;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void OnSessionCleared()
        {
            StopConnectingPulse();
            _roomList = new PacketRoomList();
            _roomState = new PacketRoomState { InRoom = false, Players = Array.Empty<PacketRoomPlayer>() };
            _wasInRoom = false;
            _wasHost = false;
            _lastRoomId = 0;
            _roomBrowserOpenPending = false;
            ResetCreateRoomDraft();
            _pendingLoadoutVehicleIndex = 0;
            _roomOptionsDraftActive = false;
            _roomOptionsTrackName = string.Empty;
            _roomOptionsTrackRandom = false;
            _roomOptionsLaps = 1;
            _roomOptionsPlayersToStart = 2;
            _savedServerDraft = new SavedServerEntry();
            _savedServerOriginal = null;
            _savedServerEditIndex = -1;
            _pendingDeleteServerIndex = -1;
            _hasPendingCompatibilityResult = false;
            _pendingCompatibilityResult = default;
            _pingPending = false;
            _pingStartedAtMs = 0;
            _historyBuffers.Clear();
            RebuildLobbyMenu();
            RebuildCreateRoomMenu();
            RebuildSavedServersMenu();
            RebuildSavedServerFormMenu();
            RebuildRoomControlsMenu();
            RebuildRoomOptionsMenu();
            RebuildRoomPlayersMenu();
            RebuildLoadoutVehicleMenu();
            RebuildLoadoutTransmissionMenu();
            UpdateRoomBrowserMenu();
            UpdateHistoryScreens();
        }
    }
}
