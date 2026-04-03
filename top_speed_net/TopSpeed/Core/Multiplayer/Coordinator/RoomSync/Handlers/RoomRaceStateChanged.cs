using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        internal void HandleRoomRaceStateChangedCore(PacketRoomRaceStateChanged roomRaceStateChanged)
        {
            var change = _state.Rooms.ApplyRaceState(roomRaceStateChanged);
            _roomUi.HandleRoomRaceStateChanged(change);
        }
    }
}
