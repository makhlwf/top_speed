using System.Collections.Generic;
using System.Linq;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        public void SetMotd(string motd)
        {
            lock (_lock)
            {
                _config.Motd = motd ?? string.Empty;
            }
        }

        public void SetMaxPlayers(int maxPlayers)
        {
            lock (_lock)
            {
                _config.MaxPlayers = maxPlayers;
            }
        }

        public ServerPlayerInfo[] GetPlayersSnapshot()
        {
            lock (_lock)
            {
                var players = _players.Values
                    .OrderBy(player => player.PlayerNumber)
                    .ToArray();
                var result = new List<ServerPlayerInfo>(players.Length);
                for (var i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    result.Add(new ServerPlayerInfo(GetPlayerDisplayName(player), player.NegotiatedProtocol));
                }

                return result.ToArray();
            }
        }

        public int ShutdownByHost(string announcementMessage)
        {
            var message = string.IsNullOrWhiteSpace(announcementMessage)
                ? "The server will be shut down immediately by the host."
                : announcementMessage.Trim();

            lock (_lock)
            {
                var players = _players.Values
                    .OrderBy(player => player.PlayerNumber)
                    .ToArray();

                for (var i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    if (!_players.ContainsKey(player.Id))
                        continue;

                    RemoveConnection(
                        player,
                        notifyRoom: false,
                        sendDisconnectPacket: true,
                        reason: "host_shutdown",
                        disconnectMessage: message,
                        announcePresenceDisconnect: false);
                }

                return players.Length;
            }
        }

        private static string GetPlayerDisplayName(PlayerConnection player)
        {
            if (!string.IsNullOrWhiteSpace(player.Name))
                return player.Name;
            return $"Player {player.PlayerNumber + 1}";
        }
    }
}
