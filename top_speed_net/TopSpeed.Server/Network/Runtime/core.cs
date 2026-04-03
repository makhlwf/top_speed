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
            _runtime.Update(deltaSeconds);
        }

        private void CleanupConnections()
        {
            _session.CleanupExpiredConnections();
        }

        private void OnPeerDisconnected(IPEndPoint endpoint)
        {
            _session.HandlePeerDisconnected(endpoint);
        }

        private void RemoveConnection(
            PlayerConnection player,
            bool notifyRoom,
            bool sendDisconnectPacket,
            string reason,
            string? disconnectMessage = null,
            bool announcePresenceDisconnect = true)
        {
            _session.RemoveConnection(player, notifyRoom, sendDisconnectPacket, reason, disconnectMessage, announcePresenceDisconnect);
        }

        private static string BuildDisconnectMessage(string reason)
        {
            return Session.BuildDisconnectMessage(reason);
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

