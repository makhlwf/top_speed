using System;
using System.Linq;
using System.Net;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Session
        {
            public void CleanupExpiredConnections()
            {
                var expired = _owner._players.Values
                    .Where(p => DateTime.UtcNow - p.LastSeenUtc > ConnectionTimeout)
                    .Select(p => p.Id)
                    .ToList();

                foreach (var id in expired)
                {
                    if (!_owner._players.TryGetValue(id, out var player))
                        continue;

                    RemoveConnection(player, notifyRoom: true, sendDisconnectPacket: true, reason: "timeout");
                }

                _owner.CleanupLiveStreams();
            }

            public void HandlePeerDisconnected(IPEndPoint endpoint)
            {
                lock (_owner._lock)
                {
                    var key = endpoint.ToString();
                    if (!_owner._endpointIndex.TryGetValue(key, out var id))
                        return;
                    if (!_owner._players.TryGetValue(id, out var player))
                        return;

                    RemoveConnection(player, notifyRoom: true, sendDisconnectPacket: false, reason: "peer_disconnect");
                }
            }

            public void RemoveConnection(
                PlayerConnection player,
                bool notifyRoom,
                bool sendDisconnectPacket,
                string reason,
                string? disconnectMessage = null,
                bool announcePresenceDisconnect = true)
            {
                var roomId = player.RoomId;
                if (player.RoomId.HasValue)
                    _owner._room.Leave(player, notifyRoom);
                if (announcePresenceDisconnect && player.ServerPresenceAnnounced)
                    _owner.BroadcastServerDisconnectAnnouncement(player, reason);
                if (sendDisconnectPacket)
                {
                    var message = string.IsNullOrWhiteSpace(disconnectMessage)
                        ? BuildDisconnectMessage(reason)
                        : disconnectMessage;
                    _owner.SendStream(player, PacketSerializer.WriteDisconnect(message), PacketStream.Control);
                }

                _owner._endpointIndex.Remove(player.EndPoint.ToString());
                _owner._players.Remove(player.Id);
                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Connection removed: player={0}, endpoint={1}, room={2}, reason={3}."),
                    player.Id,
                    player.EndPoint,
                    roomId?.ToString() ?? LocalizationService.Translate(LocalizationService.Mark("none")),
                    reason));
            }
        }
    }
}
