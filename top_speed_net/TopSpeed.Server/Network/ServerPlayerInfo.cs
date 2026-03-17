using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal readonly struct ServerPlayerInfo
    {
        public ServerPlayerInfo(string name, ProtocolVer protocolVersion)
        {
            Name = name;
            ProtocolVersion = protocolVersion;
        }

        public string Name { get; }
        public ProtocolVer ProtocolVersion { get; }
    }
}
