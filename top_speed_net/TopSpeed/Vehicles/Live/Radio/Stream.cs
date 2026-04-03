using System;
using Concentus.Structs;
using TopSpeed.Audio;
using TopSpeed.Input;
using TopSpeed.Protocol;

namespace TopSpeed.Vehicles.Live
{
    internal sealed partial class LiveRadio
    {
        public bool Start(uint streamId, LiveCodec codec, ushort sampleRate, byte channels, byte frameMs)
        {
            if (streamId == 0)
                return false;
            if (codec != LiveCodec.Opus)
                return false;
            if (channels < ProtocolConstants.LiveChannelsMin || channels > ProtocolConstants.LiveChannelsMax)
                return false;
            if (sampleRate != ProtocolConstants.LiveSampleRate)
                return false;
            if (frameMs != ProtocolConstants.LiveFrameMs)
                return false;

            lock (_lock)
            {
                if (_streamId == streamId && _decoder != null)
                {
                    UpdatePlaybackLocked();
                    return true;
                }

                StopLocked();
                _decoder = OpusDecoder.Create(sampleRate, channels);
                _streamId = streamId;
                _sampleRate = sampleRate;
                _channels = channels;
                _frameMs = frameMs;
                var samplesPerChannel = (_sampleRate * _frameMs) / 1000;
                _decodeBuffer = new short[Math.Max(1, samplesPerChannel * _channels)];
                _source = _audio.CreateProceduralSource(OnRender, _channels, _sampleRate, useHrtf: true);
                _source.SetDopplerFactor(0f);
                _source.SetPosition(_position);
                _source.SetVelocity(_velocity);
                _source.SetVolumePercent(_settings, AudioVolumeCategory.Radio, _volumePercent);
                UpdatePlaybackLocked();
                return true;
            }
        }

        public bool PushFrame(uint streamId, byte[] payload, uint _timestamp)
        {
            if (payload == null || payload.Length == 0)
                return false;
            if (payload.Length > ProtocolConstants.MaxLiveFrameBytes)
                return false;

            lock (_lock)
            {
                if (_decoder == null || _streamId == 0 || streamId != _streamId)
                    return false;

                var samplesPerChannel = (_sampleRate * _frameMs) / 1000;
                if (samplesPerChannel <= 0)
                    return false;

                int decodedPerChannel;
                try
                {
                    decodedPerChannel = _decoder.Decode(payload, 0, payload.Length, _decodeBuffer, 0, samplesPerChannel, false);
                }
                catch
                {
                    _decodeErrors++;
                    return false;
                }

                if (decodedPerChannel <= 0)
                {
                    _decodeErrors++;
                    return false;
                }

                var sampleCount = decodedPerChannel * _channels;
                var frame = new float[sampleCount];
                for (var i = 0; i < sampleCount; i++)
                    frame[i] = _decodeBuffer[i] / 32768f;

                if (_frames.Count >= MaxBufferedFrames)
                {
                    _frames.Dequeue();
                    _droppedFrames++;
                }

                _frames.Enqueue(frame);
                _receivedFrames++;
                _decodedFrames++;
                _lastFrameUtcTicks = DateTime.UtcNow.Ticks;

                if (_source != null && _desiredPlaying && !_pausedByGame && !_source.IsPlaying)
                    _source.Play(loop: true);

                return true;
            }
        }

        public void Stop(uint streamId)
        {
            lock (_lock)
            {
                if (_streamId == 0)
                    return;
                if (streamId != 0 && streamId != _streamId)
                    return;
                StopLocked();
            }
        }
    }
}

