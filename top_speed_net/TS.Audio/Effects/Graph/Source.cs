using System;
using MiniAudioEx.Core.AdvancedAPI;
using MiniAudioEx.Native;
using System.Threading;

namespace TS.Audio
{
    internal sealed class SourceGraph : IDisposable
    {
        private readonly AudioOutput _output;
        private readonly AudioBus _bus;
        private readonly ma_sound_group_ptr _group;
        private readonly AudioSourceSpatialParams _spatial;
        private readonly AudioSourceEnvelopeParams _envelope;
        private readonly bool _spatialize;
        private readonly bool _useHrtf;
        private SteamAudioSpatializer? _spatializer;
        private MaEffectNode? _envelopeNode;
        private MaEffectNode? _effectNode;
        private bool _disposed;

        public bool UsesHrtf => _useHrtf;

        public SourceGraph(AudioOutput output, AudioBus bus, ma_sound_group_ptr group, AudioSourceSpatialParams spatial, AudioSourceEnvelopeParams envelope, bool spatialize, bool useHrtf)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _group = group;
            _spatial = spatial ?? throw new ArgumentNullException(nameof(spatial));
            _envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
            _spatialize = spatialize;
            _useHrtf = _spatialize && useHrtf && output.SteamAudio != null;
            _spatializer = output.SteamAudio != null
                ? new SteamAudioSpatializer(output.SteamAudio, output.PeriodSizeInFrames, output.TrueStereoHrtf, output.DownmixMode)
                : null;
        }

        public void Configure()
        {
            var groupNode = new ma_node_ptr(_group.pointer);
            MiniAudioNative.ma_node_detach_all_output_buses(groupNode);
            EnsureEnvelopeNode();

            if (_useHrtf)
            {
                if (_effectNode == null)
                {
                    _effectNode = new MaEffectNode();
                    var init = _effectNode.Initialize(_output.Runtime.EngineHandle, (uint)_output.SampleRate, (uint)_output.Channels);
                    if (init != ma_result.success)
                        throw new InvalidOperationException("Failed to initialize HRTF effect node: " + init);

                    _effectNode.Process += OnHrtfProcess;
                    _effectNode.AttachOutputBus(0, _bus.NodeHandle, 0);
                }

                MiniAudioNative.ma_sound_group_set_spatialization_enabled(_group, 0);
                _envelopeNode!.DetachAllOutputBuses();
                _envelopeNode.AttachOutputBus(0, _effectNode.NodeHandle, 0);
                MiniAudioNative.ma_node_attach_output_bus(groupNode, 0, _envelopeNode.NodeHandle, 0);
                return;
            }

            MiniAudioNative.ma_sound_group_set_spatialization_enabled(_group, _spatialize ? 1u : 0u);
            _envelopeNode!.DetachAllOutputBuses();
            _envelopeNode.AttachOutputBus(0, _bus.NodeHandle, 0);
            MiniAudioNative.ma_node_attach_output_bus(groupNode, 0, _envelopeNode.NodeHandle, 0);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _envelopeNode?.Dispose();
            _envelopeNode = null;
            _effectNode?.Dispose();
            _effectNode = null;
            _spatializer?.Dispose();
            _spatializer = null;
        }

        public void ResetEnvelope(float gain)
        {
            Volatile.Write(ref _envelope.CurrentGain, gain);
            Volatile.Write(ref _envelope.TargetGain, gain);
            Volatile.Write(ref _envelope.RemainingFrames, 0);
            Volatile.Write(ref _envelope.StopWhenDone, 0);
            Volatile.Write(ref _envelope.StopRequested, 0);
        }

        public void BeginEnvelope(float currentGain, float targetGain, float seconds, bool stopAfter)
        {
            var frames = Math.Max(1, (int)Math.Round(seconds * _output.SampleRate));
            Volatile.Write(ref _envelope.CurrentGain, currentGain);
            Volatile.Write(ref _envelope.TargetGain, targetGain);
            Volatile.Write(ref _envelope.RemainingFrames, frames);
            Volatile.Write(ref _envelope.StopWhenDone, stopAfter ? 1 : 0);
            Volatile.Write(ref _envelope.StopRequested, 0);
        }

        public bool ConsumeStopRequested()
        {
            if (Volatile.Read(ref _envelope.StopRequested) == 0)
                return false;

            Volatile.Write(ref _envelope.StopRequested, 0);
            return true;
        }

        private void OnHrtfProcess(MaEffectNode sender, NativeArray<float> framesIn, uint frameCountIn, NativeArray<float> framesOut, ref uint frameCountOut, uint channels)
        {
            if (_disposed)
                return;

            if (_spatializer == null)
            {
                framesIn.CopyTo(framesOut);
                return;
            }

            _spatializer.Process(framesIn, frameCountIn, framesOut, ref frameCountOut, channels, _spatial);
        }

        private void EnsureEnvelopeNode()
        {
            if (_envelopeNode != null)
                return;

            _envelopeNode = new MaEffectNode();
            var init = _envelopeNode.Initialize(_output.Runtime.EngineHandle, (uint)_output.SampleRate, (uint)_output.Channels);
            if (init != ma_result.success)
                throw new InvalidOperationException("Failed to initialize envelope effect node: " + init);

            _envelopeNode.Process += OnEnvelopeProcess;
        }

        private void OnEnvelopeProcess(MaEffectNode sender, NativeArray<float> framesIn, uint frameCountIn, NativeArray<float> framesOut, ref uint frameCountOut, uint channels)
        {
            frameCountOut = frameCountIn;

            var currentGain = Volatile.Read(ref _envelope.CurrentGain);
            var targetGain = Volatile.Read(ref _envelope.TargetGain);
            var remainingFrames = Volatile.Read(ref _envelope.RemainingFrames);

            if (remainingFrames <= 0)
            {
                if (Math.Abs(currentGain - 1f) < 0.0001f)
                {
                    framesIn.CopyTo(framesOut);
                    return;
                }

                ApplyGain(framesIn, framesOut, frameCountIn, channels, currentGain);
                return;
            }

            var frameSamples = (int)channels;
            var sampleIndex = 0;
            for (var frame = 0; frame < frameCountIn; frame++)
            {
                if (remainingFrames > 0)
                {
                    currentGain += (targetGain - currentGain) / remainingFrames;
                    remainingFrames--;
                    if (remainingFrames == 0)
                        currentGain = targetGain;
                }

                for (var channel = 0; channel < frameSamples; channel++)
                {
                    framesOut[sampleIndex] = framesIn[sampleIndex] * currentGain;
                    sampleIndex++;
                }
            }

            Volatile.Write(ref _envelope.CurrentGain, currentGain);
            Volatile.Write(ref _envelope.RemainingFrames, remainingFrames);

            if (remainingFrames == 0 && Volatile.Read(ref _envelope.StopWhenDone) != 0 && currentGain <= 0.0001f)
            {
                Volatile.Write(ref _envelope.StopWhenDone, 0);
                Volatile.Write(ref _envelope.StopRequested, 1);
            }
        }

        private static void ApplyGain(NativeArray<float> framesIn, NativeArray<float> framesOut, uint frameCountIn, uint channels, float gain)
        {
            var sampleCount = (int)(frameCountIn * channels);
            for (var i = 0; i < sampleCount; i++)
                framesOut[i] = framesIn[i] * gain;
        }
    }
}
