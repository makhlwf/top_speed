using System;
using TopSpeed.Protocol;
using TopSpeed.Race.Multiplayer;

namespace TopSpeed.Race
{
    internal sealed partial class MultiplayerMode
    {
        private static bool TryGetPlayerFrameData(SnapshotFrame frame, byte playerNumber, out PacketPlayerData? data)
        {
            var players = frame.Players ?? Array.Empty<PacketPlayerData>();
            for (var i = 0; i < players.Length; i++)
            {
                var item = players[i];
                if (item == null)
                    continue;
                if (item.PlayerNumber != playerNumber)
                    continue;
                data = item;
                return true;
            }

            data = null;
            return false;
        }

        private static float Lerp(float a, float b, float alpha)
        {
            return a + ((b - a) * alpha);
        }
    }
}


