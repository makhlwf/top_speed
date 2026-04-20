using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using MiniAudioEx.Native;

namespace TS.Audio
{
    public sealed partial class AudioSourceHandle : IDisposable
    {
        private const float MaxDistanceInfinite = 1000000000f;
        private static int s_nextId;

        private readonly AudioOutput _output;
        private readonly AudioAsset _asset;
        private readonly bool _ownsAsset;
        private readonly SourcePlayback _playback;
        private readonly AudioBus _bus;
        private readonly IntPtr _sourceHandle;
        private readonly ma_sound_group_ptr _group;
        private readonly AudioSourceSpatialParams _spatial;
        private readonly AudioSourceEnvelopeParams _envelope;
        private readonly bool _spatialize;
        private readonly SourceGraph _graph;
        private readonly int _id;
        private bool _disposed;
        private bool _disposeRequested;
        private bool _looping;
        private bool _notifiedEnd;
        private Action? _onEnd;
        private float _basePitch = 1.0f;
        private float _dopplerFactor = 1.0f;
        private float _pan;
        private float _userVolume = 1.0f;
        private float _currentVolume = 1.0f;
        private float _fadeDuration;
        private float _fadeRemaining;
        private float _fadeStartVolume;
        private float _fadeTargetVolume;
        private bool _stopAfterFade;
        private DateTime? _startRequestedUtc;
        private bool _silentStartReported;
        private bool _paused;

        internal AudioSourceHandle(AudioOutput output, AudioAsset asset, bool spatialize, bool useHrtf, AudioBus bus, bool ownsAsset = true)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _asset = asset ?? throw new ArgumentNullException(nameof(asset));
            _ownsAsset = ownsAsset;
            _playback = asset.CreatePlayback();
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _spatial = new AudioSourceSpatialParams();
            _envelope = new AudioSourceEnvelopeParams();
            _spatialize = spatialize;
            _id = Interlocked.Increment(ref s_nextId);
            _sourceHandle = MiniAudioExNative.ma_ex_audio_source_init(output.Runtime.ContextHandle);
            if (_sourceHandle == IntPtr.Zero)
                throw new InvalidOperationException("Failed to initialize audio source.");

            _group = new ma_sound_group_ptr(true);
            var groupInit = MiniAudioNative.ma_sound_group_init(output.Runtime.EngineHandle, 0, default, _group);
            if (groupInit != ma_result.success)
            {
                MiniAudioExNative.ma_ex_audio_source_uninit(_sourceHandle);
                throw new InvalidOperationException("Failed to initialize audio source group: " + groupInit);
            }

            var setGroup = MiniAudioExNative.ma_ex_audio_source_set_group(_sourceHandle, _group.pointer);
            if (setGroup != ma_result.success)
            {
                MiniAudioNative.ma_sound_group_uninit(_group);
                _group.Free();
                MiniAudioExNative.ma_ex_audio_source_uninit(_sourceHandle);
                throw new InvalidOperationException("Failed to bind audio source group: " + setGroup);
            }

            _graph = new SourceGraph(output, bus, _group, _spatial, _envelope, spatialize, useHrtf);

            InitializeVolumeState();
            _graph.Configure();
            ApplyPersistedState();
            Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceCreated,
                "Audio source created.",
                new Dictionary<string, object?>
                {
                    ["assetType"] = _asset.GetType().Name,
                    ["inputChannels"] = InputChannels,
                    ["inputSampleRate"] = InputSampleRate,
                    ["spatialized"] = _spatialize,
                    ["usesHrtf"] = _graph.UsesHrtf
                });
            MaybeEmitSuspiciousConfig();
        }

        public bool IsPlaying => !_disposeRequested && MiniAudioExNative.ma_ex_audio_source_get_is_playing(_sourceHandle) > 0;
        public bool IsPaused => _paused;
        public int Id => _id;
        public int InputChannels => _asset.InputChannels;
        public int InputSampleRate => _asset.InputSampleRate;
        internal bool UsesSteamAudio => _graph.UsesHrtf;
        internal bool IsSpatialized => _spatialize;
        internal AudioSourceSpatialParams SpatialParams => _spatial;

        public void Play(bool loop)
        {
            Play(loop, 0f);
        }

        public void Play(bool loop, float fadeInSeconds)
        {
            ThrowIfDisposed();

            _looping = loop;
            _paused = false;
            _notifiedEnd = false;
            SetLooping(loop);

            if (UsePreciseFade(fadeInSeconds))
            {
                CancelFade();
                _currentVolume = _userVolume;
                SetRuntimeVolume(_currentVolume);
                _graph.BeginEnvelope(0f, 1f, fadeInSeconds, stopAfter: false);
                Emit(
                    AudioDiagnosticLevel.Trace,
                    AudioDiagnosticKind.SourceFadeStarted,
                    "Audio source precise fade-in started.",
                    new Dictionary<string, object?>
                    {
                        ["fadeSeconds"] = fadeInSeconds
                    });
                StartPlayback();
                return;
            }

            if (fadeInSeconds > 0f)
            {
                CancelFade();
                _currentVolume = 0f;
                SetRuntimeVolume(0f);
                _graph.ResetEnvelope(1f);
                StartPlayback();
                BeginFade(_userVolume, fadeInSeconds, stopAfter: false);
                Emit(
                    AudioDiagnosticLevel.Trace,
                    AudioDiagnosticKind.SourceFadeStarted,
                    "Audio source fade-in started.",
                    new Dictionary<string, object?>
                    {
                        ["fadeSeconds"] = fadeInSeconds
                    });
                return;
            }

            CancelFade();
            _currentVolume = _userVolume;
            SetRuntimeVolume(_currentVolume);
            _graph.ResetEnvelope(1f);
            StartPlayback();
        }

        public void Stop()
        {
            Stop(0f);
        }

        public void Stop(float fadeOutSeconds)
        {
            ThrowIfDisposed();

            if (fadeOutSeconds <= 0f || !IsPlaying)
            {
                CancelFade();
                MiniAudioExNative.ma_ex_audio_source_stop(_sourceHandle);
                _paused = false;
                _graph.ResetEnvelope(1f);
                _notifiedEnd = false;
                _startRequestedUtc = null;
                _silentStartReported = false;
                Emit(AudioDiagnosticLevel.Debug, AudioDiagnosticKind.SourceStopped, "Audio source stopped.");
                return;
            }

            if (UsePreciseFade(fadeOutSeconds))
            {
                CancelFade();
                _graph.BeginEnvelope(Volatile.Read(ref _envelope.CurrentGain), 0f, fadeOutSeconds, stopAfter: true);
                Emit(
                    AudioDiagnosticLevel.Trace,
                    AudioDiagnosticKind.SourceFadeStarted,
                    "Audio source precise fade-out started.",
                    new Dictionary<string, object?>
                    {
                        ["fadeSeconds"] = fadeOutSeconds
                    });
                return;
            }

            BeginFade(0f, fadeOutSeconds, stopAfter: true);
            Emit(
                AudioDiagnosticLevel.Trace,
                AudioDiagnosticKind.SourceFadeStarted,
                "Audio source fade-out started.",
                new Dictionary<string, object?>
                {
                    ["fadeSeconds"] = fadeOutSeconds
                });
        }

        public void FadeIn(float seconds)
        {
            ThrowIfDisposed();

            if (seconds <= 0f)
            {
                CancelFade();
                _currentVolume = _userVolume;
                SetRuntimeVolume(_currentVolume);
                _graph.ResetEnvelope(1f);
                return;
            }

            if (UsePreciseFade(seconds))
            {
                if (!IsPlaying)
                {
                    Play(_looping, seconds);
                    return;
                }

                CancelFade();
                _graph.BeginEnvelope(Volatile.Read(ref _envelope.CurrentGain), 1f, seconds, stopAfter: false);
                return;
            }

            if (!IsPlaying)
                Play(_looping, seconds);
            else
                BeginFade(_userVolume, seconds, stopAfter: false);
        }

        public void FadeOut(float seconds)
        {
            ThrowIfDisposed();
            if (seconds <= 0f)
            {
                Stop();
                return;
            }

            if (UsePreciseFade(seconds))
            {
                Stop(seconds);
                return;
            }

            BeginFade(0f, seconds, stopAfter: true);
        }

        public void Pause()
        {
            ThrowIfDisposed();
            if (_paused || !IsPlaying)
                return;

            CancelFade();
            MiniAudioExNative.ma_ex_audio_source_stop(_sourceHandle);
            _graph.ResetEnvelope(1f);
            _notifiedEnd = false;
            _startRequestedUtc = null;
            _silentStartReported = false;
            _paused = true;
            Emit(AudioDiagnosticLevel.Debug, AudioDiagnosticKind.SourceStopped, "Audio source paused.");
        }

        public void Resume()
        {
            ThrowIfDisposed();
            if (!_paused)
                return;

            _startRequestedUtc = DateTime.UtcNow;
            _silentStartReported = false;
            MiniAudioExNative.ma_ex_audio_source_apply_settings(_sourceHandle);
            ApplyPersistedState();
            var result = MiniAudioExNative.ma_ex_audio_source_start(_sourceHandle);
            if (result != ma_result.success)
            {
                Emit(
                    AudioDiagnosticLevel.Error,
                    AudioDiagnosticKind.SourceStarted,
                    "Audio source resume failed.",
                    new Dictionary<string, object?>
                    {
                        ["result"] = result.ToString()
                    },
                    new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
                throw new InvalidOperationException("Failed to resume audio playback: " + result);
            }

            _paused = false;
            Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceStarted,
                "Audio source resumed.",
                new Dictionary<string, object?>
                {
                    ["looping"] = _looping,
                    ["isPlaying"] = IsPlaying
                },
                new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
        }

        public void SetVolume(float volume)
        {
            ThrowIfDisposed();
            _userVolume = Math.Max(0f, volume);
            if (_fadeRemaining > 0f && !_stopAfterFade)
            {
                _fadeTargetVolume = _userVolume;
                return;
            }

            _currentVolume = _userVolume;
            SetRuntimeVolume(_currentVolume);
            Emit(
                AudioDiagnosticLevel.Trace,
                AudioDiagnosticKind.SourceVolumeChanged,
                "Audio source volume changed.",
                new Dictionary<string, object?>
                {
                    ["volume"] = _currentVolume,
                    ["volumeDb"] = AudioMath.GainToDecibels(_currentVolume),
                    ["busEffectiveVolume"] = _bus.GetEffectiveVolume(),
                    ["busEffectiveVolumeDb"] = AudioMath.GainToDecibels(_bus.GetEffectiveVolume()),
                    ["estimatedMixVolume"] = _currentVolume * _bus.GetEffectiveVolume(),
                    ["estimatedMixVolumeDb"] = AudioMath.GainToDecibels(_currentVolume * _bus.GetEffectiveVolume())
                });
        }

        public float GetVolume()
        {
            return MiniAudioNative.ma_sound_group_get_volume(_group);
        }

        internal float GetCurrentVolumeLinear()
        {
            return _currentVolume;
        }

        internal float GetCurrentVolumeDb()
        {
            return AudioMath.GainToDecibels(_currentVolume);
        }

        internal float GetBusEffectiveVolumeLinear()
        {
            return _bus.GetEffectiveVolume();
        }

        internal float GetBusEffectiveVolumeDb()
        {
            return AudioMath.GainToDecibels(_bus.GetEffectiveVolume());
        }

        internal float GetEstimatedMixVolumeLinear()
        {
            return _currentVolume * _bus.GetEffectiveVolume();
        }

        internal float GetEstimatedMixVolumeDb()
        {
            return AudioMath.GainToDecibels(GetEstimatedMixVolumeLinear());
        }

        public void SetPitch(float pitch)
        {
            ThrowIfDisposed();
            _basePitch = pitch;
            MiniAudioNative.ma_sound_group_set_pitch(_group, pitch);
            Emit(
                AudioDiagnosticLevel.Trace,
                AudioDiagnosticKind.SourcePitchChanged,
                "Audio source pitch changed.",
                new Dictionary<string, object?>
                {
                    ["pitch"] = pitch
                });
        }

        public float GetPitch()
        {
            return MiniAudioNative.ma_sound_group_get_pitch(_group);
        }

        public void SetPan(float pan)
        {
            ThrowIfDisposed();
            _pan = pan;
            if (_spatialize)
                return;

            MiniAudioNative.ma_sound_group_set_pan(_group, pan);
            Emit(
                AudioDiagnosticLevel.Trace,
                AudioDiagnosticKind.SourcePanChanged,
                "Audio source pan changed.",
                new Dictionary<string, object?>
                {
                    ["pan"] = pan
                });
        }

        public void SetStereoWidening(bool enabled)
        {
            if (!_spatialize)
                return;

            Volatile.Write(ref _spatial.StereoWidening, enabled ? 1 : 0);
            Emit(
                AudioDiagnosticLevel.Trace,
                AudioDiagnosticKind.SourceStereoWideningChanged,
                enabled ? "Audio source stereo widening enabled." : "Audio source stereo widening disabled.",
                new Dictionary<string, object?>
                {
                    ["enabled"] = enabled
                });
        }

        public void SetLooping(bool loop)
        {
            _looping = loop;
            MiniAudioExNative.ma_ex_audio_source_set_loop(_sourceHandle, loop ? 1u : 0u);
            Emit(
                AudioDiagnosticLevel.Trace,
                AudioDiagnosticKind.SourceLoopingChanged,
                "Audio source looping changed.",
                new Dictionary<string, object?>
                {
                    ["looping"] = loop
                });
        }

        public void SeekToStart()
        {
            if (!_playback.SupportsSeeking)
                return;

            MiniAudioExNative.ma_ex_audio_source_set_pcm_position(_sourceHandle, 0);
            Emit(AudioDiagnosticLevel.Trace, AudioDiagnosticKind.SourceSeeked, "Audio source seeked to start.");
        }

        public void Restart(bool loop, float fadeInSeconds)
        {
            ThrowIfDisposed();

            // If we are already playing, we must ensure we don't click when seeking.
            // We use a tiny 1ms snap-fade to zero before seeking if precise fades are enabled.
            if (IsPlaying && UsePreciseFade(fadeInSeconds))
            {
                _graph.BeginEnvelope(Volatile.Read(ref _envelope.CurrentGain), 0f, 0.001f, stopAfter: false);
            }

            SeekToStart();
            Play(loop, fadeInSeconds);
        }

        public float GetLengthSeconds()
        {
            if (_asset.LengthSeconds > 0f)
                return _asset.LengthSeconds;

            var frames = MiniAudioExNative.ma_ex_audio_source_get_pcm_length(_sourceHandle);
            if (frames > 0 && _asset.InputSampleRate > 0)
                return (float)(frames / (double)_asset.InputSampleRate);

            return 0f;
        }

        public void SetOnEnd(Action onEnd)
        {
            _onEnd = onEnd;
        }

        internal AudioSourceSnapshot CaptureSnapshot()
        {
            var busEffective = _bus.GetEffectiveVolume();
            var estimatedMix = _currentVolume * busEffective;
            return new AudioSourceSnapshot(
                _id,
                _bus.Name,
                IsPlaying,
                _spatialize,
                _graph.UsesHrtf,
                InputChannels,
                InputSampleRate,
                _looping,
                _currentVolume,
                AudioMath.GainToDecibels(_currentVolume),
                _basePitch,
                _pan,
                busEffective,
                AudioMath.GainToDecibels(busEffective),
                estimatedMix,
                AudioMath.GainToDecibels(estimatedMix),
                _bus.CaptureGainStages(),
                GetLengthSeconds());
        }

        private void ThrowIfDisposed()
        {
            if (_disposed || _disposeRequested)
                throw new ObjectDisposedException(nameof(AudioSourceHandle));
        }

        private void Emit(AudioDiagnosticLevel level, AudioDiagnosticKind kind, string message, IReadOnlyDictionary<string, object?>? data = null, AudioDiagnosticSnapshot? snapshot = null)
        {
            _output.Diagnostics.Emit(level, kind, AudioDiagnosticEntityType.Source, _output.Name, _bus.Name, _id, message, data, snapshot);
        }

        private void MaybeEmitSuspiciousConfig()
        {
            if (InputChannels > 0 && InputSampleRate > 0)
                return;

            Emit(
                AudioDiagnosticLevel.Warn,
                AudioDiagnosticKind.AnomalySuspiciousSourceConfig,
                "Audio source has suspicious input format.",
                new Dictionary<string, object?>
                {
                    ["inputChannels"] = InputChannels,
                    ["inputSampleRate"] = InputSampleRate
                },
                new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
        }
    }
}
