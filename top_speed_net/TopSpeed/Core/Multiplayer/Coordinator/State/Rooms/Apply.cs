using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class RoomStore
    {
        public void ApplyRoomListEvent(RoomEventInfo roomEvent)
        {
            if (roomEvent.Kind == RoomEventKind.None)
                return;

            var rooms = new List<RoomSummaryInfo>(RoomList.Rooms ?? Array.Empty<RoomSummaryInfo>());
            var index = rooms.FindIndex(r => r.RoomId == roomEvent.RoomId);

            switch (roomEvent.Kind)
            {
                case RoomEventKind.RoomRemoved:
                    if (index >= 0)
                        rooms.RemoveAt(index);
                    break;

                case RoomEventKind.RoomCreated:
                case RoomEventKind.RoomSummaryUpdated:
                case RoomEventKind.ParticipantJoined:
                case RoomEventKind.ParticipantLeft:
                case RoomEventKind.BotAdded:
                case RoomEventKind.BotRemoved:
                case RoomEventKind.PlayersToStartChanged:
                    var summary = new RoomSummaryInfo
                    {
                        RoomId = roomEvent.RoomId,
                        RoomName = roomEvent.RoomName ?? string.Empty,
                        RoomType = roomEvent.RoomType,
                        PlayerCount = roomEvent.PlayerCount,
                        PlayersToStart = roomEvent.PlayersToStart,
                        RaceState = roomEvent.RaceState,
                        TrackName = roomEvent.TrackName ?? string.Empty
                    };
                    if (index >= 0)
                        rooms[index] = summary;
                    else if (roomEvent.Kind != RoomEventKind.RoomSummaryUpdated || roomEvent.RoomId != 0)
                        rooms.Add(summary);
                    break;
            }

            rooms.Sort((a, b) => a.RoomId.CompareTo(b.RoomId));
            RoomList = new RoomListInfo { Rooms = rooms.ToArray() };
        }

        public bool TryApplyCurrentRoomEvent(
            RoomEventInfo roomEvent,
            uint localPlayerId,
            out bool localHostChanged,
            out bool becameHost)
        {
            localHostChanged = false;
            becameHost = false;

            if (!CurrentRoom.InRoom || CurrentRoom.RoomId != roomEvent.RoomId)
                return false;

            var previousIsHost = CurrentRoom.IsHost;

            CurrentRoom.RoomVersion = roomEvent.RoomVersion;
            if (!string.IsNullOrWhiteSpace(roomEvent.RoomName))
                CurrentRoom.RoomName = roomEvent.RoomName;
            CurrentRoom.HostPlayerId = roomEvent.HostPlayerId;
            CurrentRoom.RoomType = roomEvent.RoomType;
            CurrentRoom.PlayersToStart = roomEvent.PlayersToStart;
            CurrentRoom.RaceInstanceId = roomEvent.RaceInstanceId;
            CurrentRoom.RaceState = roomEvent.RaceState;
            CurrentRoom.TrackName = roomEvent.TrackName ?? string.Empty;
            CurrentRoom.Laps = roomEvent.Laps;
            CurrentRoom.GameRulesFlags = roomEvent.GameRulesFlags;
            CurrentRoom.IsHost = localPlayerId != 0 && roomEvent.HostPlayerId == localPlayerId;

            switch (roomEvent.Kind)
            {
                case RoomEventKind.ParticipantJoined:
                case RoomEventKind.BotAdded:
                case RoomEventKind.ParticipantStateChanged:
                    UpsertCurrentRoomParticipant(roomEvent);
                    break;

                case RoomEventKind.ParticipantLeft:
                case RoomEventKind.BotRemoved:
                    RemoveCurrentRoomParticipant(roomEvent.SubjectPlayerId);
                    break;
            }

            localHostChanged = previousIsHost != CurrentRoom.IsHost;
            becameHost = localHostChanged &&
                CurrentRoom.IsHost &&
                (roomEvent.Kind == RoomEventKind.ParticipantLeft || roomEvent.Kind == RoomEventKind.HostChanged) &&
                (roomEvent.PlayerCount <= 1 || (CurrentRoom.Players?.Length ?? int.MaxValue) <= 1);
            WasHost = CurrentRoom.IsHost;
            return true;
        }

        private void UpdateRoomListRaceState(uint roomId, RoomRaceState state)
        {
            var rooms = RoomList.Rooms ?? Array.Empty<RoomSummaryInfo>();
            if (rooms.Length == 0)
                return;

            var updated = false;
            var copy = new RoomSummaryInfo[rooms.Length];
            for (var i = 0; i < rooms.Length; i++)
            {
                var source = rooms[i];
                if (source.RoomId == roomId)
                {
                    source.RaceState = state;
                    updated = true;
                }
                copy[i] = source;
            }

            if (updated)
                RoomList = new RoomListInfo { Rooms = copy };
        }

        private void UpsertCurrentRoomParticipant(RoomEventInfo roomEvent)
        {
            if (roomEvent.SubjectPlayerId == 0)
                return;

            var players = new List<RoomParticipant>(CurrentRoom.Players ?? Array.Empty<RoomParticipant>());
            var index = players.FindIndex(p => p.PlayerId == roomEvent.SubjectPlayerId);
            var name = string.IsNullOrWhiteSpace(roomEvent.SubjectPlayerName)
                ? LocalizationService.Translate(LocalizationService.Mark("Player ")) + (roomEvent.SubjectPlayerNumber + 1)
                : roomEvent.SubjectPlayerName;
            var item = new RoomParticipant
            {
                PlayerId = roomEvent.SubjectPlayerId,
                PlayerNumber = roomEvent.SubjectPlayerNumber,
                State = roomEvent.SubjectPlayerState,
                Name = name
            };

            if (index >= 0)
                players[index] = item;
            else
                players.Add(item);

            players.Sort((a, b) => a.PlayerNumber.CompareTo(b.PlayerNumber));
            CurrentRoom.Players = players.ToArray();
        }

        private void RemoveCurrentRoomParticipant(uint playerId)
        {
            if (playerId == 0)
                return;

            var players = new List<RoomParticipant>(CurrentRoom.Players ?? Array.Empty<RoomParticipant>());
            var removed = players.RemoveAll(p => p.PlayerId == playerId);
            if (removed == 0)
                return;

            players.Sort((a, b) => a.PlayerNumber.CompareTo(b.PlayerNumber));
            CurrentRoom.Players = players.ToArray();
        }
    }
}
