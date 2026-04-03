using System.Net;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void OnPacket(IPEndPoint endPoint, byte[] payload)
        {
            _session.HandlePacket(endPoint, payload);
        }

        private void RegisterPackets()
        {
            RegisterCorePackets();
            RegisterRacePackets();
            RegisterMediaPackets();
            RegisterLivePackets();
            RegisterRoomPackets();
            RegisterChatPackets();
        }

    }
}
