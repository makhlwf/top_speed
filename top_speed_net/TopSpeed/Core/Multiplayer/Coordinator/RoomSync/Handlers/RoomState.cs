using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void HandleRoomState(PacketRoomState roomState)
        {
            _roomsFlow.HandleRoomState(roomState);
        }

        internal void HandleRoomStateCore(PacketRoomState roomState)
        {
            var change = _state.Rooms.ApplyRoomState(roomState);
            _roomUi.HandleRoomState(change);
        }
    }
}

