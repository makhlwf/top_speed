using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private sealed partial class MultiplayerDispatch
        {
            private void RegisterMedia()
            {
                _reg.Add("media", Command.PlayerMediaBegin, HandlePlayerMediaBegin);
                _reg.Add("media", Command.PlayerMediaChunk, HandlePlayerMediaChunk);
                _reg.Add("media", Command.PlayerMediaEnd, HandlePlayerMediaEnd);
            }

            private bool HandlePlayerMediaBegin(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadPlayerMediaBegin(packet.Payload, out var mediaBegin))
                    _owner._multiplayerRaceRuntime.Mode.ApplyRemoteMediaBegin(mediaBegin);
                return true;
            }

            private bool HandlePlayerMediaChunk(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadPlayerMediaChunk(packet.Payload, out var mediaChunk))
                    _owner._multiplayerRaceRuntime.Mode.ApplyRemoteMediaChunk(mediaChunk);
                return true;
            }

            private bool HandlePlayerMediaEnd(IncomingPacket packet)
            {
                if (_owner._multiplayerRaceRuntime.Mode == null)
                    return true;

                if (ClientPacketSerializer.TryReadPlayerMediaEnd(packet.Payload, out var mediaEnd))
                    _owner._multiplayerRaceRuntime.Mode.ApplyRemoteMediaEnd(mediaEnd);
                return true;
            }
        }
    }
}
