using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void RegisterMultiplayerControlPacketHandlers()
        {
            _mpPktReg.Add("control", Command.Disconnect, HandleMpDisconnectPacket);
            _mpPktReg.Add("control", Command.PlayerNumber, HandleMpPlayerNumberPacket);
            _mpPktReg.Add("control", Command.Pong, HandleMpPongPacket);
        }

        private bool HandleMpDisconnectPacket(IncomingPacket packet)
        {
            var message = "Disconnected from server.";
            if (ClientPacketSerializer.TryReadDisconnect(packet.Payload, out var disconnectMessage) &&
                !string.IsNullOrWhiteSpace(disconnectMessage))
            {
                message = disconnectMessage;
            }

            _speech.Speak(message);
            DisconnectFromServer();
            return true;
        }

        private bool HandleMpPlayerNumberPacket(IncomingPacket packet)
        {
            var session = _session;
            if (session == null)
                return false;

            if (ClientPacketSerializer.TryReadPlayer(packet.Payload, out var assigned) && assigned.PlayerId == session.PlayerId)
                session.UpdatePlayerNumber(assigned.PlayerNumber);
            return true;
        }

        private bool HandleMpPongPacket(IncomingPacket packet)
        {
            _multiplayerCoordinator.HandlePingReply(packet.ReceivedUtcTicks);
            return true;
        }
    }
}
