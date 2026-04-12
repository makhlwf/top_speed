using System;
using System.Collections.Generic;
using System.Numerics;

namespace TS.Audio
{
    public sealed partial class AudioOutput : IDisposable
    {
        private readonly AudioOutputConfig _config;
        private readonly AudioSystemConfig _systemConfig;
        private readonly bool _trueStereoHrtf;
        private readonly HrtfDownmixMode _downmixMode;
        private readonly OutputRuntime _runtime;
        private readonly AudioDiagnostics _diagnostics;
        private readonly List<AudioSourceHandle> _sources;
        private readonly List<TrackStream> _streams;
        private readonly List<RetiredSource> _retired;
        private readonly List<RetiredEffect> _retiredEffects;
        private readonly Dictionary<string, AudioBus> _buses;
        private readonly object _sourceLock = new object();
        private readonly object _busLock = new object();
        private readonly SteamAudioContext? _steamAudio;
        private RoomAcoustics _roomAcoustics;
        private readonly AudioBus _mainBus;
        private Vector3 _listenerPosition;
        private Vector3 _listenerVelocity;
        private bool _disposed;

        public string Name => _config.Name;
        public int SampleRate => (int)_config.SampleRate;
        public int Channels => (int)_config.Channels;
        public uint PeriodSizeInFrames => _config.PeriodSizeInFrames;
        public SteamAudioContext? SteamAudio => _steamAudio;
        public bool TrueStereoHrtf => _trueStereoHrtf;
        public HrtfDownmixMode DownmixMode => _downmixMode;
        public bool IsHrtfActive => _steamAudio != null;
        public AudioBus MainBus => _mainBus;
        internal AudioDiagnostics Diagnostics => _diagnostics;

        private sealed class RetiredSource
        {
            public readonly AudioSourceHandle Source;
            public readonly DateTime DisposeAfterUtc;

            public RetiredSource(AudioSourceHandle source, DateTime disposeAfterUtc)
            {
                Source = source;
                DisposeAfterUtc = disposeAfterUtc;
            }
        }

        private sealed class RetiredEffect
        {
            public readonly BusEffect Effect;
            public readonly DateTime DisposeAfterUtc;

            public RetiredEffect(BusEffect effect, DateTime disposeAfterUtc)
            {
                Effect = effect;
                DisposeAfterUtc = disposeAfterUtc;
            }
        }

        public AudioOutput(AudioOutputConfig config, AudioSystemConfig systemConfig, AudioDiagnostics diagnostics)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _systemConfig = systemConfig ?? throw new ArgumentNullException(nameof(systemConfig));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            _sources = new List<AudioSourceHandle>();
            _streams = new List<TrackStream>();
            _retired = new List<RetiredSource>();
            _retiredEffects = new List<RetiredEffect>();
            _buses = new Dictionary<string, AudioBus>(StringComparer.OrdinalIgnoreCase);
            _trueStereoHrtf = _systemConfig.HrtfMode == HrtfMode.Stereo;
            _downmixMode = _systemConfig.HrtfDownmixMode;
            _roomAcoustics = RoomAcoustics.Default;

            if (_config.SampleRate == 0)
                _config.SampleRate = _systemConfig.SampleRate;
            if (_config.Channels == 0)
                _config.Channels = _systemConfig.UseHrtf ? 2u : _systemConfig.Channels;
            if (_config.PeriodSizeInFrames == 0)
                _config.PeriodSizeInFrames = _systemConfig.PeriodSizeInFrames;

            _runtime = new OutputRuntime(_config);
            _runtime.BindOwner(this);
            _mainBus = CreateBusInternal("main", null, null);
            _mainBus.AddEffect(_runtime.ProcessMasterLimiter, "master");

            _steamAudio = _systemConfig.UseHrtf
                ? new SteamAudioContext((int)_config.SampleRate, (int)_config.PeriodSizeInFrames, _systemConfig.HrtfSofaPath)
                : null;
        }

        public void SetMasterVolume(float volume)
        {
            _runtime.SetMasterVolume(volume);
            _diagnostics.Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.OutputMasterVolumeChanged,
                AudioDiagnosticEntityType.Output,
                Name,
                null,
                null,
                "Audio output master volume changed.",
                new Dictionary<string, object?>
                {
                    ["masterVolume"] = _runtime.GetMasterVolume(),
                    ["masterVolumeDb"] = AudioMath.GainToDecibels(_runtime.GetMasterVolume()),
                    ["lastPreLimiterPeak"] = _runtime.GetLastPreLimiterPeak(),
                    ["lastPreLimiterPeakDbfs"] = AudioMath.GainToDecibels(_runtime.GetLastPreLimiterPeak()),
                    ["lastPostLimiterPeak"] = _runtime.GetLastPostLimiterPeak(),
                    ["lastPostLimiterPeakDbfs"] = AudioMath.GainToDecibels(_runtime.GetLastPostLimiterPeak())
                },
                new AudioDiagnosticSnapshot(output: CaptureSnapshot()));
        }

        public float GetMasterVolume()
        {
            return _runtime.GetMasterVolume();
        }

        internal OutputRuntime Runtime => _runtime;
        internal AudioSystemConfig SystemConfig => _systemConfig;

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AudioOutput));
        }
    }
}
