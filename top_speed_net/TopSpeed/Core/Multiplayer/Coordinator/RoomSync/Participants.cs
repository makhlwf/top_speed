using System;
using System.Collections.Generic;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void UpsertCurrentRoomParticipant(PacketRoomEvent roomEvent)
        {
            if (roomEvent.SubjectPlayerId == 0)
                return;

            var players = new List<PacketRoomPlayer>(_roomState.Players ?? Array.Empty<PacketRoomPlayer>());
            var index = players.FindIndex(p => p.PlayerId == roomEvent.SubjectPlayerId);
            var name = string.IsNullOrWhiteSpace(roomEvent.SubjectPlayerName)
                ? $"Player {roomEvent.SubjectPlayerNumber + 1}"
                : roomEvent.SubjectPlayerName;
            var item = new PacketRoomPlayer
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
            _roomState.Players = players.ToArray();
        }

        private void RemoveCurrentRoomParticipant(uint playerId)
        {
            if (playerId == 0)
                return;

            var players = new List<PacketRoomPlayer>(_roomState.Players ?? Array.Empty<PacketRoomPlayer>());
            var removed = players.RemoveAll(p => p.PlayerId == playerId);
            if (removed == 0)
                return;

            players.Sort((a, b) => a.PlayerNumber.CompareTo(b.PlayerNumber));
            _roomState.Players = players.ToArray();
        }
    }
}
