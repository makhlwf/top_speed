using System;
using System.Numerics;

namespace TS.Audio
{
    public sealed class Source : IDisposable
    {
        private readonly AudioSourceHandle _handle;
        private readonly bool _ownsHandle;

        internal Source(AudioSourceHandle handle, bool ownsHandle)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            _ownsHandle = ownsHandle;
        }

        public bool IsPlaying => _handle.IsPlaying;
        public bool IsPaused => _handle.IsPaused;
        public int InputChannels => _handle.InputChannels;
        public int InputSampleRate => _handle.InputSampleRate;
        public float LengthSeconds => _handle.GetLengthSeconds();
        public AudioSourceSnapshot CaptureSnapshot() => _handle.CaptureSnapshot();

        public void Play(bool loop = false) => _handle.Play(loop);
        public void Play(bool loop, float fadeInSeconds) => _handle.Play(loop, fadeInSeconds);
        public void Stop() => _handle.Stop();
        public void Stop(float fadeOutSeconds) => _handle.Stop(fadeOutSeconds);
        public void Pause() => _handle.Pause();
        public void Resume() => _handle.Resume();
        public void FadeIn(float seconds) => _handle.FadeIn(seconds);
        public void FadeOut(float seconds) => _handle.FadeOut(seconds);
        public void SeekToStart() => _handle.SeekToStart();
        public void Restart(bool loop, float fadeInSeconds) => _handle.Restart(loop, fadeInSeconds);
        public void SetLooping(bool looping) => _handle.SetLooping(looping);
        public void SetOnEnd(Action onEnd) => _handle.SetOnEnd(onEnd);

        public void SetVolume(float value) => _handle.SetVolume(value);
        public float GetVolume() => _handle.GetVolume();
        public void SetPitch(float value) => _handle.SetPitch(value);
        public float GetPitch() => _handle.GetPitch();
        public void SetPan(float value) => _handle.SetPan(value);
        public void SetStereoWidening(bool enabled) => _handle.SetStereoWidening(enabled);

        public void SetPosition(Vector3 position) => _handle.SetPosition(position);
        public void SetVelocity(Vector3 velocity) => _handle.SetVelocity(velocity);
        public void SetDistanceModel(DistanceModel model, float minDistance, float maxDistance, float rollOff) => _handle.SetDistanceModel(model, minDistance, maxDistance, rollOff);
        public void SetCurveDistanceScaler(float value) => _handle.ApplyCurveDistanceScaler(value);
        public void SetDopplerFactor(float value) => _handle.SetDopplerFactor(value);
        public void SetRoomAcoustics(RoomAcoustics acoustics) => _handle.SetRoomAcoustics(acoustics);

        public void Dispose()
        {
            if (_ownsHandle)
                _handle.Dispose();
        }
    }
}
