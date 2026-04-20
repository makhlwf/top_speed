using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using MiniAudioEx.Native;

namespace TS.Audio
{
    public sealed partial class AudioSourceHandle
    {
        private const float PreciseFadeMaxSeconds = 0.02f;

        internal void Update(double deltaTime)
        {
            if (_disposeRequested || _disposed)
                return;

            if (_startRequestedUtc.HasValue && IsPlaying)
            {
                _startRequestedUtc = null;
                _silentStartReported = false;
            }

            if (_startRequestedUtc.HasValue && !_silentStartReported)
            {
                var elapsed = DateTime.UtcNow - _startRequestedUtc.Value;
                if (elapsed.TotalSeconds >= 0.25)
                {
                    _silentStartReported = true;
                    Emit(
                        AudioDiagnosticLevel.Warn,
                        AudioDiagnosticKind.AnomalySilentStart,
                        "Audio source did not report playback shortly after start.",
                        new Dictionary<string, object?>
                        {
                            ["startDelayMs"] = elapsed.TotalMilliseconds,
                            ["looping"] = _looping,
                            ["lengthSeconds"] = GetLengthSeconds()
                        },
                        new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
                }
            }

            if (_graph.ConsumeStopRequested())
            {
                MiniAudioExNative.ma_ex_audio_source_stop(_sourceHandle);
                _paused = false;
                _graph.ResetEnvelope(1f);
                _notifiedEnd = false;
                _startRequestedUtc = null;
                _silentStartReported = false;
                Emit(AudioDiagnosticLevel.Trace, AudioDiagnosticKind.SourceStopped, "Audio source stopped after precise fade.");
                return;
            }

            UpdateFade(deltaTime);

            if (_paused)
                return;

            if (_looping)
                return;

            if (MiniAudioExNative.ma_ex_audio_source_get_is_at_end(_sourceHandle) == 0)
            {
                _notifiedEnd = false;
                return;
            }

            if (_notifiedEnd)
                return;

            _notifiedEnd = true;
            Emit(AudioDiagnosticLevel.Trace, AudioDiagnosticKind.SourceEnded, "Audio source reached end.");
            var onEnd = _onEnd;
            if (onEnd != null)
                ThreadPool.QueueUserWorkItem(_ => onEnd());
        }

        private void StartPlayback()
        {
            _startRequestedUtc = DateTime.UtcNow;
            _silentStartReported = false;
            Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourcePlayRequested,
                "Audio source playback requested.",
                new Dictionary<string, object?>
                {
                    ["looping"] = _looping,
                    ["fadeSeconds"] = _fadeRemaining > 0f ? _fadeRemaining : _fadeDuration
                });

            var result = _playback.Prepare(_sourceHandle);
            if (result != ma_result.success)
            {
                Emit(
                    AudioDiagnosticLevel.Error,
                    AudioDiagnosticKind.SourceStarted,
                    "Audio source prepare failed.",
                    new Dictionary<string, object?>
                    {
                        ["result"] = result.ToString()
                    },
                    new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
                throw new InvalidOperationException("Failed to prepare audio playback: " + result);
            }

            MiniAudioExNative.ma_ex_audio_source_apply_settings(_sourceHandle);
            ApplyPersistedState();

            result = MiniAudioExNative.ma_ex_audio_source_start(_sourceHandle);
            if (result != ma_result.success)
            {
                Emit(
                    AudioDiagnosticLevel.Error,
                    AudioDiagnosticKind.SourceStarted,
                    "Audio source start failed.",
                    new Dictionary<string, object?>
                    {
                        ["result"] = result.ToString()
                    },
                    new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
                throw new InvalidOperationException("Failed to start audio playback: " + result);
            }

            _paused = false;
            Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceStarted,
                "Audio source started.",
                new Dictionary<string, object?>
                {
                    ["looping"] = _looping,
                    ["isPlaying"] = IsPlaying
                },
                new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
        }

        private void ApplyPersistedState()
        {
            SetRuntimeVolume(_currentVolume);
            MiniAudioNative.ma_sound_group_set_pitch(_group, _basePitch);
            MiniAudioNative.ma_sound_group_set_doppler_factor(_group, _dopplerFactor);
            MiniAudioExNative.ma_ex_audio_source_set_loop(_sourceHandle, _looping ? 1u : 0u);

            if (!_spatialize)
            {
                MiniAudioNative.ma_sound_group_set_pan(_group, _pan);
                return;
            }

            var refDistance = Volatile.Read(ref _spatial.RefDistance);
            var maxDistance = Volatile.Read(ref _spatial.MaxDistance);
            var rollOff = Volatile.Read(ref _spatial.RollOff);

            MiniAudioNative.ma_sound_group_set_min_distance(_group, refDistance);
            MiniAudioNative.ma_sound_group_set_max_distance(_group, maxDistance);
            MiniAudioNative.ma_sound_group_set_rolloff(_group, rollOff);
            MiniAudioNative.ma_sound_group_set_attenuation_model(_group, ToMaAttenuationModel(_spatial.DistanceModel));

            if (!_graph.UsesHrtf)
            {
                var pos = ToMaVec3(new Vector3(
                    Volatile.Read(ref _spatial.PosX),
                    Volatile.Read(ref _spatial.PosY),
                    Volatile.Read(ref _spatial.PosZ)));
                var vel = ToMaVec3(new Vector3(
                    Volatile.Read(ref _spatial.VelX),
                    Volatile.Read(ref _spatial.VelY),
                    Volatile.Read(ref _spatial.VelZ)));

                MiniAudioNative.ma_sound_group_set_position(_group, pos.x, pos.y, pos.z);
                MiniAudioNative.ma_sound_group_set_velocity(_group, vel.x, vel.y, vel.z);
            }
        }

        private void InitializeVolumeState()
        {
            _userVolume = 1f;
            _currentVolume = _userVolume;
            _fadeDuration = 0f;
            _fadeRemaining = 0f;
            _fadeStartVolume = _currentVolume;
            _fadeTargetVolume = _currentVolume;
            _stopAfterFade = false;
            _graph.ResetEnvelope(1f);
        }

        private void BeginFade(float targetVolume, float durationSeconds, bool stopAfter)
        {
            _fadeDuration = Math.Max(0.0001f, durationSeconds);
            _fadeRemaining = _fadeDuration;
            _fadeStartVolume = _currentVolume;
            _fadeTargetVolume = targetVolume;
            _stopAfterFade = stopAfter;
        }

        private void CancelFade()
        {
            _fadeDuration = 0f;
            _fadeRemaining = 0f;
            _fadeStartVolume = _currentVolume;
            _fadeTargetVolume = _currentVolume;
            _stopAfterFade = false;
        }

        private void UpdateFade(double deltaTime)
        {
            if (_fadeRemaining <= 0f)
                return;

            _fadeRemaining -= (float)deltaTime;
            if (_fadeRemaining <= 0f)
            {
                _currentVolume = _fadeTargetVolume;
                SetRuntimeVolume(_currentVolume);

                var shouldStop = _stopAfterFade;
                CancelFade();
                if (shouldStop)
                {
                    MiniAudioExNative.ma_ex_audio_source_stop(_sourceHandle);
                    _paused = false;
                    _notifiedEnd = false;
                    _startRequestedUtc = null;
                    _silentStartReported = false;
                    Emit(AudioDiagnosticLevel.Trace, AudioDiagnosticKind.SourceStopped, "Audio source stopped after fade.");
                }
                return;
            }

            var progress = 1f - (_fadeRemaining / _fadeDuration);
            _currentVolume = _fadeStartVolume + ((_fadeTargetVolume - _fadeStartVolume) * progress);
            SetRuntimeVolume(_currentVolume);
        }

        private void SetRuntimeVolume(float value)
        {
            var volume = value;
            if (volume < 0f)
                volume = 0f;
            MiniAudioNative.ma_sound_group_set_volume(_group, volume);
        }

        private static bool UsePreciseFade(float seconds)
        {
            return seconds > 0f && seconds <= PreciseFadeMaxSeconds;
        }
    }
}
