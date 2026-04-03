using System;
using MiniAudioEx.Core.AdvancedAPI;
using MiniAudioEx.Native;
using TopSpeed.Protocol;

namespace TopSpeed.Network.Live
{
    internal sealed class Source : IDisposable
    {
        private readonly MaDecoder _decoder;
        private readonly int _channels;
        private readonly int _framesPerPacket;
        private readonly short[] _sampleBuffer;

        private Source(MaDecoder decoder, int channels, int framesPerPacket)
        {
            _decoder = decoder;
            _channels = channels;
            _framesPerPacket = framesPerPacket;
            _sampleBuffer = new short[_channels * _framesPerPacket];
        }

        public static bool TryOpen(string filePath, out Source? source)
        {
            source = null;
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var decoder = new MaDecoder();
            var config = decoder.GetConfig(ma_format.s16, (uint)ProtocolConstants.LiveChannelsMax, (uint)ProtocolConstants.LiveSampleRate);
            var initResult = decoder.InitializeFromFile(filePath, config);
            if (initResult != ma_result.success)
            {
                decoder.Dispose();
                return false;
            }

            if (decoder.GetLengthInPCMFrames(out var totalFrames) != ma_result.success || totalFrames == 0)
            {
                decoder.Dispose();
                return false;
            }

            var frameCount = ProtocolConstants.LiveSampleRate * ProtocolConstants.LiveFrameMs / 1000;
            source = new Source(decoder, ProtocolConstants.LiveChannelsMax, frameCount);
            return true;
        }

        public bool TryRead(out short[] samples)
        {
            samples = _sampleBuffer;
            var targetFrames = (ulong)_framesPerPacket;
            ulong writtenFrames = 0;
            var wraps = 0;
            var stalledReads = 0;

            while (writtenFrames < targetFrames)
            {
                var sampleOffset = (int)(writtenFrames * (ulong)_channels);
                var framesToRead = targetFrames - writtenFrames;
                var readResult = _decoder.ReadPCMFrames(_sampleBuffer, sampleOffset, framesToRead, out var readFrames);

                if (readFrames > 0)
                {
                    writtenFrames += readFrames;
                    stalledReads = 0;
                    continue;
                }

                if (readResult != ma_result.success && readResult != ma_result.at_end)
                    return false;

                if (_decoder.SeekToPCMFrame(0) != ma_result.success)
                    return false;

                wraps++;
                if (wraps > _framesPerPacket)
                    return false;

                stalledReads++;
                if (stalledReads > 2)
                    return false;
            }

            return true;
        }

        public void Dispose()
        {
            _decoder.Dispose();
        }
    }
}

