using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class RoomDraftState
    {
        public bool IsRoomBrowserOpenPending;
        public bool IsOnlinePlayersOpenPending;
        public GameRoomType CreateRoomType = GameRoomType.BotsRace;
        public byte CreateRoomPlayersToStart = 2;
        public string CreateRoomName = string.Empty;
        public int PendingLoadoutVehicleIndex;
        public bool RoomOptionsDraftActive;
        public string RoomOptionsTrackName = string.Empty;
        public bool RoomOptionsTrackRandom;
        public byte RoomOptionsLaps = 1;
        public byte RoomOptionsPlayersToStart = 2;
        public uint RoomOptionsGameRulesFlags;
    }
}
