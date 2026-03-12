using System;
using TopSpeed.Core.Multiplayer.Chat;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private bool ApplyCurrentRoomEvent(PacketRoomEvent roomEvent, out bool beginLoadout, out bool localHostChanged)
        {
            beginLoadout = false;
            localHostChanged = false;

            if (!_roomState.InRoom || _roomState.RoomId != roomEvent.RoomId)
                return false;

            var previousIsHost = _roomState.IsHost;
            var session = SessionOrNull();

            _roomState.RoomVersion = roomEvent.RoomVersion;
            if (!string.IsNullOrWhiteSpace(roomEvent.RoomName))
                _roomState.RoomName = roomEvent.RoomName;
            _roomState.HostPlayerId = roomEvent.HostPlayerId;
            _roomState.RoomType = roomEvent.RoomType;
            _roomState.PlayersToStart = roomEvent.PlayersToStart;
            _roomState.RaceStarted = roomEvent.RaceStarted;
            _roomState.PreparingRace = roomEvent.PreparingRace;
            _roomState.TrackName = roomEvent.TrackName ?? string.Empty;
            _roomState.Laps = roomEvent.Laps;
            _roomState.IsHost = session != null && roomEvent.HostPlayerId == session.PlayerId;
            var localPlayerId = session?.PlayerId ?? 0u;

            switch (roomEvent.Kind)
            {
                case RoomEventKind.ParticipantJoined:
                    if (roomEvent.SubjectPlayerId != 0 && roomEvent.SubjectPlayerId != localPlayerId)
                    {
                        PlayNetworkSound("room_join.ogg");
                        AddRoomEventMessage(HistoryText.ParticipantJoined(roomEvent));
                    }
                    UpsertCurrentRoomParticipant(roomEvent);
                    break;

                case RoomEventKind.BotAdded:
                    UpsertCurrentRoomParticipant(roomEvent);
                    break;

                case RoomEventKind.ParticipantLeft:
                    if (roomEvent.SubjectPlayerId != 0 && roomEvent.SubjectPlayerId != localPlayerId)
                    {
                        PlayNetworkSound("room_leave.ogg");
                        AddRoomEventMessage(HistoryText.ParticipantLeft(roomEvent));
                    }
                    RemoveCurrentRoomParticipant(roomEvent.SubjectPlayerId);
                    break;

                case RoomEventKind.BotRemoved:
                    RemoveCurrentRoomParticipant(roomEvent.SubjectPlayerId);
                    break;

                case RoomEventKind.ParticipantStateChanged:
                    UpsertCurrentRoomParticipant(roomEvent);
                    break;

                case RoomEventKind.PrepareStarted:
                    beginLoadout = true;
                    break;
            }

            localHostChanged = previousIsHost != _roomState.IsHost;
            if (localHostChanged &&
                _roomState.IsHost &&
                (roomEvent.Kind == RoomEventKind.ParticipantLeft || roomEvent.Kind == RoomEventKind.HostChanged) &&
                (roomEvent.PlayerCount <= 1 || (_roomState.Players?.Length ?? int.MaxValue) <= 1))
            {
                var hostText = HistoryText.BecameHost();
                _speech.Speak(hostText);
                AddRoomEventMessage(hostText);
            }

            _wasHost = _roomState.IsHost;
            return true;
        }
    }
}
