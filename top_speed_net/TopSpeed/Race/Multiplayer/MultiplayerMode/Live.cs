using TopSpeed.Protocol;

namespace TopSpeed.Race
{
    internal sealed partial class MultiplayerMode
    {
        public void ApplyRemoteLiveStart(PacketPlayerLiveStart start, long receivedUtcTicks)
        {
            if (!CanApplyRemoteLive(start.PlayerNumber))
                return;
            if (!IsValidLiveStart(start))
                return;

            _remoteLiveStates[start.PlayerNumber] = new Multiplayer.LiveState(start, receivedUtcTicks);
            if (_remotePlayers.TryGetValue(start.PlayerNumber, out var remote))
                remote.Player.ApplyLiveStart(start.StreamId, start.Codec, start.SampleRate, start.Channels, start.FrameMs);
        }

        public void ApplyRemoteLiveFrame(PacketPlayerLiveFrame frame, long receivedUtcTicks)
        {
            if (!CanApplyRemoteLive(frame.PlayerNumber))
                return;
            if (!_remoteLiveStates.TryGetValue(frame.PlayerNumber, out var live))
                return;

            live.TryPush(frame, receivedUtcTicks);
        }

        public void ApplyRemoteLiveStop(PacketPlayerLiveStop stop)
        {
            if (!CanApplyRemoteLive(stop.PlayerNumber))
                return;
            if (!_remoteLiveStates.TryGetValue(stop.PlayerNumber, out var live))
                return;
            if (live.StreamId != stop.StreamId)
                return;

            if (_remotePlayers.TryGetValue(stop.PlayerNumber, out var remote))
                remote.Player.ApplyLiveStop(stop.StreamId);
            _remoteLiveStates.Remove(stop.PlayerNumber);
        }

        private void DrainRemoteLiveFrames()
        {
            if (_remoteLiveStates.Count == 0)
                return;

            var nowTicks = System.DateTime.UtcNow.Ticks;
            var timeoutTicks = System.TimeSpan.FromMilliseconds(ProtocolConstants.LiveTimeoutMs).Ticks;
            _expiredLivePlayers.Clear();

            foreach (var pair in _remoteLiveStates)
            {
                var playerNumber = pair.Key;
                var live = pair.Value;
                if (!_remotePlayers.TryGetValue(playerNumber, out var remote))
                    continue;

                if (nowTicks - live.LastReceivedUtcTicks > timeoutTicks)
                {
                    remote.Player.ApplyLiveStop(live.StreamId);
                    _expiredLivePlayers.Add(playerNumber);
                    continue;
                }

                remote.Player.ApplyLiveStart(live.StreamId, live.Codec, live.SampleRate, live.Channels, live.FrameMs);
                while (live.Frames.Count > 0)
                {
                    var frame = live.Frames.Dequeue();
                    if (remote.Player.ApplyLiveFrame(live.StreamId, frame.Payload, frame.Timestamp))
                        live.MarkForwarded();
                    else
                        live.MarkDecodeDropped();
                }
            }

            for (var i = 0; i < _expiredLivePlayers.Count; i++)
                _remoteLiveStates.Remove(_expiredLivePlayers[i]);
        }

        private bool CanApplyRemoteLive(byte playerNumber)
        {
            if (playerNumber == _playerNumber)
                return false;
            if (playerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[playerNumber])
                return false;
            return true;
        }

        private static bool IsValidLiveStart(PacketPlayerLiveStart start)
        {
            if (start.StreamId == 0)
                return false;
            if (start.Codec != LiveCodec.Opus)
                return false;
            if (start.SampleRate != ProtocolConstants.LiveSampleRate)
                return false;
            if (start.FrameMs != ProtocolConstants.LiveFrameMs)
                return false;
            if (start.Channels < ProtocolConstants.LiveChannelsMin || start.Channels > ProtocolConstants.LiveChannelsMax)
                return false;
            return true;
        }
    }
}

