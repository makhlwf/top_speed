using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using MiniAudioEx.Native;

namespace TS.Audio
{
    internal sealed class OutputRuntime : IDisposable
    {
        private readonly ma_device_data_proc _deviceDataProc;
        private readonly IntPtr _contextHandle;
        private float _masterVolume = 1f;
        private float _limiterGain = 1f;
        private float _lastPreLimiterPeak;
        private float _lastPostLimiterPeak;
        private AudioOutput? _owner;
        private long _nextClippingRiskTicks;

        public AudioOutputConfig Config { get; }
        public IntPtr ContextHandle => _contextHandle;
        public ma_engine_ptr EngineHandle { get; }
        public ma_node_ptr Endpoint => MiniAudioNative.ma_engine_get_endpoint(EngineHandle);

        public OutputRuntime(AudioOutputConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));

            var deviceInfo = ResolveDeviceInfo(config.DeviceIndex);
            var sampleRate = config.SampleRate > 0 ? config.SampleRate : 44100u;
            var channels = config.Channels > 0 ? (byte)config.Channels : (byte)2;
            var periodSize = config.PeriodSizeInFrames;

            _deviceDataProc = OnDeviceData;
            var contextConfig = MiniAudioExNative.ma_ex_context_config_init(sampleRate, channels, periodSize, ref deviceInfo);
            contextConfig = MiniAudioExNative.ma_ex_context_config_set_device_data_proc(contextConfig, _deviceDataProc);

            _contextHandle = MiniAudioExNative.ma_ex_context_init(ref contextConfig);
            if (_contextHandle == IntPtr.Zero)
                throw new InvalidOperationException("Failed to initialize audio output.");

            EngineHandle = new ma_engine_ptr(MiniAudioExNative.ma_ex_context_get_engine(_contextHandle));
            if (EngineHandle.pointer == IntPtr.Zero)
            {
                MiniAudioExNative.ma_ex_context_uninit(_contextHandle);
                throw new InvalidOperationException("Failed to acquire engine from audio output.");
            }

            InitializeListener();
            MiniAudioExNative.ma_ex_context_set_master_volume(_contextHandle, 1f);

            var engineSampleRate = MiniAudioNative.ma_engine_get_sample_rate(EngineHandle);
            if (engineSampleRate > 0)
                Config.SampleRate = engineSampleRate;

            var engineChannels = MiniAudioNative.ma_engine_get_channels(EngineHandle);
            if (engineChannels > 0)
                Config.Channels = engineChannels;

        }

        public void SetMasterVolume(float volume)
        {
            Volatile.Write(ref _masterVolume, Clamp01(volume));
        }

        public void BindOwner(AudioOutput owner)
        {
            _owner = owner;
        }

        public float GetMasterVolume()
        {
            return Volatile.Read(ref _masterVolume);
        }

        public float GetLastPreLimiterPeak()
        {
            return Volatile.Read(ref _lastPreLimiterPeak);
        }

        public float GetLastPostLimiterPeak()
        {
            return Volatile.Read(ref _lastPostLimiterPeak);
        }

        public void UpdateListener(ma_vec3f position, ma_vec3f direction, ma_vec3f worldUp, ma_vec3f velocity)
        {
            const uint listenerIndex = 0;
            MiniAudioNative.ma_engine_listener_set_position(EngineHandle, listenerIndex, position.x, position.y, position.z);
            MiniAudioNative.ma_engine_listener_set_direction(EngineHandle, listenerIndex, direction.x, direction.y, direction.z);
            MiniAudioNative.ma_engine_listener_set_world_up(EngineHandle, listenerIndex, worldUp.x, worldUp.y, worldUp.z);
            MiniAudioNative.ma_engine_listener_set_velocity(EngineHandle, listenerIndex, velocity.x, velocity.y, velocity.z);
        }

        public void Dispose()
        {
            if (_contextHandle != IntPtr.Zero)
                MiniAudioExNative.ma_ex_context_uninit(_contextHandle);
        }

        private void InitializeListener()
        {
            const uint listenerIndex = 0;
            MiniAudioNative.ma_engine_listener_set_enabled(EngineHandle, listenerIndex, 1);
            MiniAudioNative.ma_engine_listener_set_position(EngineHandle, listenerIndex, 0f, 0f, 0f);
            MiniAudioNative.ma_engine_listener_set_direction(EngineHandle, listenerIndex, 0f, 0f, -1f);
            MiniAudioNative.ma_engine_listener_set_world_up(EngineHandle, listenerIndex, 0f, 1f, 0f);
            MiniAudioNative.ma_engine_listener_set_velocity(EngineHandle, listenerIndex, 0f, 0f, 0f);
        }

        private static ma_ex_device_info ResolveDeviceInfo(int? deviceIndex)
        {
            return new ma_ex_device_info
            {
                index = deviceIndex ?? -1,
                pName = IntPtr.Zero,
                nativeDataFormatCount = 0,
                nativeDataFormats = IntPtr.Zero
            };
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        private static void OnDeviceData(ma_device_ptr pDevice, IntPtr pOutput, IntPtr pInput, uint frameCount)
        {
            var engine = MiniAudioExNative.ma_ex_device_get_user_data(pDevice.pointer);
            if (engine == IntPtr.Zero)
                return;

            MiniAudioExNative.ma_engine_read_pcm_frames(engine, pOutput, frameCount, out _);
        }

        internal void ProcessMasterLimiter(NativeArray<float> framesIn, uint frameCountIn, NativeArray<float> framesOut, ref uint frameCountOut, uint channels)
        {
            if (frameCountIn == 0 || channels == 0)
            {
                frameCountOut = 0;
                return;
            }

            frameCountOut = frameCountIn;
            framesIn.CopyTo(framesOut);
            ApplyMasterLimiter(framesOut.Pointer, frameCountIn, channels);
        }

        private unsafe void ApplyMasterLimiter(IntPtr pOutput, uint frameCount, uint channels)
        {
            if (pOutput == IntPtr.Zero || frameCount == 0 || channels == 0)
                return;

            var sampleCountLong = (long)frameCount * channels;
            if (sampleCountLong <= 0 || sampleCountLong > int.MaxValue)
                return;

            var samples = (float*)pOutput;
            var sampleCount = (int)sampleCountLong;
            var master = Volatile.Read(ref _masterVolume);
            if (master < 0f)
                master = 0f;
            else if (master > 1f)
                master = 1f;

            var peak = 0f;
            for (var i = 0; i < sampleCount; i++)
            {
                var abs = Math.Abs(samples[i] * master);
                if (abs > peak)
                    peak = abs;
            }

            var targetLimiterGain = peak > 1f ? 1f / peak : 1f;
            var limiterGain = _limiterGain;
            if (targetLimiterGain < limiterGain)
            {
                limiterGain = targetLimiterGain;
            }
            else
            {
                limiterGain += (targetLimiterGain - limiterGain) * 0.05f;
                if (limiterGain > 1f)
                    limiterGain = 1f;
            }

            _limiterGain = limiterGain;
            Volatile.Write(ref _lastPreLimiterPeak, peak);

            var gain = master * limiterGain;
            var postPeak = 0f;
            for (var i = 0; i < sampleCount; i++)
            {
                var value = samples[i] * gain;
                if (value > 1f)
                    value = 1f;
                else if (value < -1f)
                    value = -1f;
                var abs = Math.Abs(value);
                if (abs > postPeak)
                    postPeak = abs;
                samples[i] = value;
            }

            Volatile.Write(ref _lastPostLimiterPeak, postPeak);

            if (peak > 1f)
                MaybeEmitClippingRisk(peak, postPeak, limiterGain, master);
        }

        private void MaybeEmitClippingRisk(float peak, float postPeak, float limiterGain, float master)
        {
            var owner = _owner;
            if (owner == null)
                return;

            if (!owner.Diagnostics.ShouldEmit(AudioDiagnosticLevel.Warn, AudioDiagnosticKind.AnomalyClippingRisk, AudioDiagnosticEntityType.Output, owner.Name, null, null))
                return;

            var nowTicks = DateTime.UtcNow.Ticks;
            var nextTicks = Interlocked.Read(ref _nextClippingRiskTicks);
            if (nowTicks < nextTicks)
                return;

            var updated = nowTicks + TimeSpan.FromMilliseconds(500).Ticks;
            Interlocked.Exchange(ref _nextClippingRiskTicks, updated);

            owner.Diagnostics.Emit(
                AudioDiagnosticLevel.Warn,
                AudioDiagnosticKind.AnomalyClippingRisk,
                AudioDiagnosticEntityType.Output,
                owner.Name,
                null,
                null,
                "Audio output peak exceeded unity gain.",
                new Dictionary<string, object?>
                {
                    ["peak"] = peak,
                    ["peakDbfs"] = AudioMath.GainToDecibels(peak),
                    ["postLimiterPeak"] = postPeak,
                    ["postLimiterPeakDbfs"] = AudioMath.GainToDecibels(postPeak),
                    ["limiterGain"] = limiterGain,
                    ["limiterGainDb"] = AudioMath.GainToDecibels(limiterGain),
                    ["masterVolume"] = master
                },
                new AudioDiagnosticSnapshot(output: owner.CaptureSnapshot(), mix: owner.CaptureMixSnapshot(peak, postPeak, limiterGain, master)));
        }
    }
}
