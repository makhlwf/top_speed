using System;
using TopSpeed.Input;
using TS.Audio;

namespace TopSpeed.Audio
{
    internal static class AudioHelpers
    {
        public static void SetVolumePercent(this Source handle, int percent)
        {
            var clamped = Math.Max(0, Math.Min(100, percent));
            handle.SetVolume(clamped / 100f);
        }

        public static void SetVolumePercent(this Source? handle, DriveSettings settings, AudioVolumeCategory category, int percent)
        {
            if (handle == null)
                return;

            var clamped = Math.Max(0, Math.Min(100, percent));
            var scale = settings.GetCategoryScalar(category);
            handle.SetVolume((clamped / 100f) * scale);
        }

        public static void SetVolumeUnit(this Source? handle, DriveSettings settings, AudioVolumeCategory category, float normalizedVolume)
        {
            if (handle == null)
                return;

            var clamped = Math.Max(0f, Math.Min(1f, normalizedVolume));
            var scale = settings.GetCategoryScalar(category);
            handle.SetVolume(clamped * scale);
        }

        public static void SetPanPercent(this Source handle, int pan)
        {
            var clamped = Math.Max(-100, Math.Min(100, pan));
            handle.SetPan(clamped / 100f);
        }

        public static void SetFrequency(this Source handle, int frequency)
        {
            if (frequency <= 0)
            {
                handle.SetPitch(0.001f);
                return;
            }

            var sampleRate = handle.InputSampleRate > 0 ? handle.InputSampleRate : 44100;
            var pitch = frequency / (float)sampleRate;
            if (pitch < 0.001f)
                pitch = 0.001f;
            handle.SetPitch(pitch);
        }

        public static void Restart(this Source handle, bool loop)
        {
            handle.Stop();
            handle.SeekToStart();
            handle.Play(loop);
        }

        public static void Restart(this Source handle, bool loop, float fadeInSeconds)
        {
            handle.Stop(fadeInSeconds);
            handle.SeekToStart();
            handle.Play(loop, fadeInSeconds);
        }
    }
}


