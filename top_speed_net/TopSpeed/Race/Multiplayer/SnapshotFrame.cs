using TopSpeed.Protocol;

namespace TopSpeed.Race.Multiplayer
{
    internal sealed class SnapshotFrame
    {
        public uint Tick { get; set; }
        public PacketPlayerData[] Players { get; set; } = System.Array.Empty<PacketPlayerData>();
    }
}

