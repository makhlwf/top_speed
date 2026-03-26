using System;
using System.Linq;
using System.Net;
using LiteNetLib;
using TopSpeed.Localization;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private static readonly string SnapshotServerName = LocalizationService.Mark("TopSpeed Server");

        public void Start()
        {
            ResetStreamTxMetrics();
            _transport.Start(_config.Port);
            _logger.Info(LocalizationService.Mark("Race server started."));
        }

        public void Stop()
        {
            lock (_lock)
            {
                _rooms.Clear();
                _players.Clear();
                _endpointIndex.Clear();
                ResetStreamTxMetrics();
            }

            _transport.Stop();
            _logger.Info(LocalizationService.Mark("Race server stopped."));
        }

        public void Update(float deltaSeconds)
        {
            lock (_lock)
            {
                if (deltaSeconds <= 0f)
                    return;

                _simulationAccumulator += deltaSeconds;
                while (_simulationAccumulator >= ServerSimulationStepSeconds)
                {
                    _simulationAccumulator -= ServerSimulationStepSeconds;
                    _simulationTick++;
                    _cleanupAccumulator += ServerSimulationStepSeconds;
                    _snapshotAccumulator += ServerSimulationStepSeconds;

                    if (_cleanupAccumulator >= CleanupIntervalSeconds)
                    {
                        _cleanupAccumulator -= CleanupIntervalSeconds;
                        CleanupConnections();
                    }

                    UpdateBots(ServerSimulationStepSeconds);
                    CheckForBumps();
                    UpdateRaceCompletions(ServerSimulationStepSeconds);

                    if (_snapshotAccumulator >= ServerSnapshotIntervalSeconds)
                    {
                        _snapshotAccumulator -= ServerSnapshotIntervalSeconds;
                        BroadcastPlayerData();
                    }
                }
            }
        }

        private void CleanupConnections()
        {
            var expired = _players.Values.Where(p => DateTime.UtcNow - p.LastSeenUtc > ConnectionTimeout).Select(p => p.Id).ToList();
            foreach (var id in expired)
            {
                if (!_players.TryGetValue(id, out var player))
                    continue;

                RemoveConnection(player, notifyRoom: true, sendDisconnectPacket: true, reason: "timeout");
            }

            CleanupLiveStreams();
        }

        private void OnPeerDisconnected(IPEndPoint endpoint)
        {
            lock (_lock)
            {
                var key = endpoint.ToString();
                if (!_endpointIndex.TryGetValue(key, out var id))
                    return;
                if (!_players.TryGetValue(id, out var player))
                    return;

                RemoveConnection(player, notifyRoom: true, sendDisconnectPacket: false, reason: "peer_disconnect");
            }
        }

        private void RemoveConnection(
            PlayerConnection player,
            bool notifyRoom,
            bool sendDisconnectPacket,
            string reason,
            string? disconnectMessage = null,
            bool announcePresenceDisconnect = true)
        {
            var roomId = player.RoomId;
            if (player.RoomId.HasValue)
                HandleLeaveRoom(player, notifyRoom);
            if (announcePresenceDisconnect && player.ServerPresenceAnnounced)
                BroadcastServerDisconnectAnnouncement(player, reason);
            if (sendDisconnectPacket)
            {
                var message = string.IsNullOrWhiteSpace(disconnectMessage)
                    ? BuildDisconnectMessage(reason)
                    : disconnectMessage;
                SendStream(player, TopSpeed.Server.Protocol.PacketSerializer.WriteDisconnect(message), TopSpeed.Protocol.PacketStream.Control);
            }
            _endpointIndex.Remove(player.EndPoint.ToString());
            _players.Remove(player.Id);
            _logger.Info(LocalizationService.Format(
                LocalizationService.Mark("Connection removed: player={0}, endpoint={1}, room={2}, reason={3}."),
                player.Id,
                player.EndPoint,
                roomId?.ToString() ?? LocalizationService.Translate(LocalizationService.Mark("none")),
                reason));
        }

        private static string BuildDisconnectMessage(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return LocalizationService.Mark("The server closed the connection.");

            switch (reason)
            {
                case "timeout":
                    return LocalizationService.Mark("Connection timed out.");
                case "protocol_mismatch":
                    return LocalizationService.Mark("Connection refused due to protocol mismatch.");
                case "protocol_rejected":
                    return LocalizationService.Mark("Connection refused due to invalid protocol negotiation.");
                case "server_full":
                    return LocalizationService.Mark("This server is full.");
                case "host_shutdown":
                    return LocalizationService.Mark("The server will be shut down immediately by the host.");
                case "peer_disconnect":
                    return LocalizationService.Mark("Connection closed.");
                default:
                    return LocalizationService.Format(LocalizationService.Mark("Connection closed by server. Reason: {0}."), reason);
            }
        }

        public ServerSnapshot GetSnapshot()
        {
            lock (_lock)
            {
                var raceStarted = _rooms.Values.Any(r => r.RaceStarted);
                var trackSelected = _rooms.Values.Any(r => r.TrackSelected);
                var trackName = _rooms.Count == 1
                    ? _rooms.Values.First().TrackName
                    : (_rooms.Count > 1 ? LocalizationService.Mark("multiple") : string.Empty);
                return new ServerSnapshot(SnapshotServerName, _config.Port, _config.MaxPlayers, _players.Count, raceStarted, trackSelected, trackName);
            }
        }

        public void Dispose()
        {
            _transport.Dispose();
        }
    }
}
