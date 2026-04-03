using System;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class RoomStore
    {
        public RoomListInfo RoomList = new RoomListInfo();
        public RoomSnapshot CurrentRoom = new RoomSnapshot { InRoom = false, Players = Array.Empty<RoomParticipant>() };
        public bool WasInRoom;
        public uint LastRoomId;
        public bool WasHost;
        public OnlineListInfo OnlinePlayers = new OnlineListInfo();

        public void Reset()
        {
            RoomList = new RoomListInfo();
            CurrentRoom = new RoomSnapshot { InRoom = false, Players = Array.Empty<RoomParticipant>() };
            WasInRoom = false;
            LastRoomId = 0;
            WasHost = false;
            OnlinePlayers = new OnlineListInfo();
        }

        public RoomStateChange ApplyRoomState(PacketRoomState roomState)
        {
            var change = new RoomStateChange(WasInRoom, LastRoomId, WasHost, CurrentRoom.RoomType);
            CurrentRoom = RoomMap.ToSnapshot(roomState);
            WasInRoom = CurrentRoom.InRoom;
            LastRoomId = CurrentRoom.RoomId;
            WasHost = CurrentRoom.IsHost;
            return change;
        }

        public RoomRaceChange ApplyRaceState(PacketRoomRaceStateChanged roomRaceStateChanged)
        {
            if (roomRaceStateChanged == null || roomRaceStateChanged.RoomId == 0)
                return default;

            var beginLoadout = false;
            var leaveLoadout = false;

            if (CurrentRoom.InRoom && CurrentRoom.RoomId == roomRaceStateChanged.RoomId)
            {
                var previousRaceState = CurrentRoom.RaceState;
                CurrentRoom.RoomVersion = roomRaceStateChanged.RoomVersion;
                CurrentRoom.RaceInstanceId = roomRaceStateChanged.RaceInstanceId;
                CurrentRoom.RaceState = roomRaceStateChanged.State;
                beginLoadout = roomRaceStateChanged.State == RoomRaceState.Preparing && previousRaceState != RoomRaceState.Preparing;
                leaveLoadout = previousRaceState == RoomRaceState.Preparing && roomRaceStateChanged.State != RoomRaceState.Preparing;
            }

            UpdateRoomListRaceState(roomRaceStateChanged.RoomId, roomRaceStateChanged.State);
            return new RoomRaceChange(beginLoadout, leaveLoadout);
        }

        public void ApplyRoomList(PacketRoomList roomList)
        {
            RoomList = RoomMap.ToList(roomList);
        }

        public void ApplyOnlinePlayers(PacketOnlinePlayers onlinePlayers)
        {
            OnlinePlayers = OnlineMap.ToList(onlinePlayers);
        }

        public string ResolvePlayerName(byte playerNumber)
        {
            var players = CurrentRoom.Players ?? Array.Empty<RoomParticipant>();
            for (var i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (player == null || player.PlayerNumber != playerNumber)
                    continue;
                if (!string.IsNullOrWhiteSpace(player.Name))
                    return player.Name.Trim();
                break;
            }

            return LocalizationService.Format(LocalizationService.Mark("Player {0}"), playerNumber + 1);
        }
    }

    internal readonly struct RoomStateChange
    {
        public RoomStateChange(bool wasInRoom, uint previousRoomId, bool previousIsHost, GameRoomType previousRoomType)
        {
            WasInRoom = wasInRoom;
            PreviousRoomId = previousRoomId;
            PreviousIsHost = previousIsHost;
            PreviousRoomType = previousRoomType;
        }

        public bool WasInRoom { get; }
        public uint PreviousRoomId { get; }
        public bool PreviousIsHost { get; }
        public GameRoomType PreviousRoomType { get; }
    }

    internal readonly struct RoomRaceChange
    {
        public RoomRaceChange(bool beginLoadout, bool leaveLoadout)
        {
            BeginLoadout = beginLoadout;
            LeaveLoadout = leaveLoadout;
        }

        public bool BeginLoadout { get; }
        public bool LeaveLoadout { get; }
    }
}
