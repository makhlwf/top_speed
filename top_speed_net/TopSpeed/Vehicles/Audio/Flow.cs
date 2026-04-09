using System;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TS.Audio;

namespace TopSpeed.Vehicles.Audio
{
    internal sealed class Flow : IFlow
    {
        private const int MaxSurfaceFreq = 100000;

        public void RefreshVolumes(
            RaceSettings settings,
            bool force,
            int throttleVolume,
            AudioSourceHandle soundEngine,
            AudioSourceHandle soundStart,
            AudioSourceHandle? soundThrottle,
            AudioSourceHandle soundHorn,
            AudioSourceHandle soundBrake,
            AudioSourceHandle soundMiniCrash,
            AudioSourceHandle soundBump,
            AudioSourceHandle soundBadSwitch,
            AudioSourceHandle? soundWipers,
            AudioSourceHandle soundCrash,
            AudioSourceHandle? soundBackfire,
            AudioSourceHandle[] soundCrashVariants,
            AudioSourceHandle[] soundBackfireVariants,
            AudioSourceHandle soundAsphalt,
            AudioSourceHandle soundGravel,
            AudioSourceHandle soundWater,
            AudioSourceHandle soundSand,
            AudioSourceHandle soundSnow,
            ref int lastPlayerEngineVolumePercent,
            ref int lastPlayerEventsVolumePercent,
            ref int lastSurfaceLoopVolumePercent)
        {
            var enginePercent = settings.AudioVolumes?.PlayerVehicleEnginePercent ?? 100;
            var eventsPercent = settings.AudioVolumes?.PlayerVehicleEventsPercent ?? 100;
            var surfacePercent = settings.AudioVolumes?.SurfaceLoopsPercent ?? 70;

            if (!force &&
                enginePercent == lastPlayerEngineVolumePercent &&
                eventsPercent == lastPlayerEventsVolumePercent &&
                surfacePercent == lastSurfaceLoopVolumePercent)
            {
                return;
            }

            lastPlayerEngineVolumePercent = enginePercent;
            lastPlayerEventsVolumePercent = eventsPercent;
            lastSurfaceLoopVolumePercent = surfacePercent;

            SetPlayerEngineVolumePercent(settings, soundEngine, 90);
            SetPlayerEngineVolumePercent(settings, soundStart, 100);
            SetPlayerEngineVolumePercent(settings, soundThrottle, throttleVolume);
            SetPlayerEventVolumePercent(settings, soundHorn, 100);
            SetPlayerEventVolumePercent(settings, soundBrake, 100);
            SetPlayerEventVolumePercent(settings, soundMiniCrash, 100);
            SetPlayerEventVolumePercent(settings, soundBump, 100);
            SetPlayerEventVolumePercent(settings, soundBadSwitch, 100);
            SetPlayerEventVolumePercent(settings, soundWipers, 100);
            SetPlayerEventVolumePercent(settings, soundCrash, 100);
            SetPlayerEventVolumePercent(settings, soundBackfire, 100);
            for (var i = 0; i < soundCrashVariants.Length; i++)
                SetPlayerEventVolumePercent(settings, soundCrashVariants[i], 100);
            for (var i = 0; i < soundBackfireVariants.Length; i++)
                SetPlayerEventVolumePercent(settings, soundBackfireVariants[i], 100);

            SetSurfaceLoopVolumePercent(settings, soundAsphalt, 90);
            SetSurfaceLoopVolumePercent(settings, soundGravel, 90);
            SetSurfaceLoopVolumePercent(settings, soundWater, 90);
            SetSurfaceLoopVolumePercent(settings, soundSand, 90);
            SetSurfaceLoopVolumePercent(settings, soundSnow, 90);
        }

        public void UpdateHorn(AudioSourceHandle soundHorn, CarState state, bool horning)
        {
            if (horning && state != CarState.Crashing)
            {
                if (!soundHorn.IsPlaying)
                    soundHorn.Play(loop: true);
            }
            else if (soundHorn.IsPlaying)
            {
                soundHorn.Stop();
            }
        }

        public void UpdateRoad(
            TrackSurface surface,
            float speed,
            ref int surfaceFrequency,
            ref int prevSurfaceFrequency,
            AudioSourceHandle soundAsphalt,
            AudioSourceHandle soundGravel,
            AudioSourceHandle soundWater,
            AudioSourceHandle soundSand,
            AudioSourceHandle soundSnow)
        {
            surfaceFrequency = (int)(speed * 500);
            if (surfaceFrequency == prevSurfaceFrequency)
                return;

            switch (surface)
            {
                case TrackSurface.Asphalt:
                    soundAsphalt.SetFrequency(Math.Min(surfaceFrequency, MaxSurfaceFreq));
                    break;
                case TrackSurface.Gravel:
                    soundGravel.SetFrequency(Math.Min(surfaceFrequency, MaxSurfaceFreq));
                    break;
                case TrackSurface.Water:
                    soundWater.SetFrequency(Math.Min(surfaceFrequency, MaxSurfaceFreq));
                    break;
                case TrackSurface.Sand:
                    soundSand.SetFrequency((int)(surfaceFrequency / 2.5f));
                    break;
                case TrackSurface.Snow:
                    soundSnow.SetFrequency(Math.Min(surfaceFrequency, MaxSurfaceFreq));
                    break;
            }

            prevSurfaceFrequency = surfaceFrequency;
        }

        public void ApplyPan(
            TrackSurface surface,
            int pan,
            AudioSourceHandle soundHorn,
            AudioSourceHandle soundBrake,
            AudioSourceHandle? soundBackfire,
            AudioSourceHandle? soundWipers,
            AudioSourceHandle soundAsphalt,
            AudioSourceHandle soundGravel,
            AudioSourceHandle soundWater,
            AudioSourceHandle soundSand,
            AudioSourceHandle soundSnow)
        {
            soundHorn.SetPanPercent(pan);
            soundBrake.SetPanPercent(pan);
            soundBackfire?.SetPanPercent(pan);
            soundWipers?.SetPanPercent(pan);

            switch (surface)
            {
                case TrackSurface.Asphalt:
                    soundAsphalt.SetPanPercent(pan);
                    break;
                case TrackSurface.Gravel:
                    soundGravel.SetPanPercent(pan);
                    break;
                case TrackSurface.Water:
                    soundWater.SetPanPercent(pan);
                    break;
                case TrackSurface.Sand:
                    soundSand.SetPanPercent(pan);
                    break;
                case TrackSurface.Snow:
                    soundSnow.SetPanPercent(pan);
                    break;
            }
        }

        public int CalculatePan(float relativePosition)
        {
            var pan = (relativePosition - 0.5f) * 200.0f;
            if (pan < -100.0f)
                pan = -100.0f;
            if (pan > 100.0f)
                pan = 100.0f;
            return (int)pan;
        }

        public void Pause(
            TrackSurface surface,
            AudioSourceHandle soundEngine,
            AudioSourceHandle? soundThrottle,
            AudioSourceHandle soundBrake,
            AudioSourceHandle soundHorn,
            AudioSourceHandle? soundWipers,
            AudioSourceHandle soundAsphalt,
            AudioSourceHandle soundGravel,
            AudioSourceHandle soundWater,
            AudioSourceHandle soundSand,
            AudioSourceHandle soundSnow,
            Action stopResetBackfireVariants)
        {
            soundEngine.Stop();
            soundThrottle?.Stop();
            if (soundBrake.IsPlaying)
                soundBrake.Stop();
            if (soundHorn.IsPlaying)
                soundHorn.Stop();
            stopResetBackfireVariants();
            soundWipers?.Stop();
            switch (surface)
            {
                case TrackSurface.Asphalt:
                    soundAsphalt.Stop();
                    break;
                case TrackSurface.Gravel:
                    soundGravel.Stop();
                    break;
                case TrackSurface.Water:
                    soundWater.Stop();
                    break;
                case TrackSurface.Sand:
                    soundSand.Stop();
                    break;
                case TrackSurface.Snow:
                    soundSnow.Stop();
                    break;
            }
        }

        public void Unpause(
            TrackSurface surface,
            AudioSourceHandle soundEngine,
            AudioSourceHandle? soundThrottle,
            AudioSourceHandle? soundWipers,
            AudioSourceHandle soundAsphalt,
            AudioSourceHandle soundGravel,
            AudioSourceHandle soundWater,
            AudioSourceHandle soundSand,
            AudioSourceHandle soundSnow)
        {
            soundEngine.Play(loop: true);
            soundThrottle?.Play(loop: true);
            soundWipers?.Play(loop: true);
            switch (surface)
            {
                case TrackSurface.Asphalt:
                    soundAsphalt.Play(loop: true);
                    break;
                case TrackSurface.Gravel:
                    soundGravel.Play(loop: true);
                    break;
                case TrackSurface.Water:
                    soundWater.Play(loop: true);
                    break;
                case TrackSurface.Sand:
                    soundSand.Play(loop: true);
                    break;
                case TrackSurface.Snow:
                    soundSnow.Play(loop: true);
                    break;
            }
        }

        private static void SetPlayerEngineVolumePercent(RaceSettings settings, AudioSourceHandle? sound, int percent)
        {
            sound.SetVolumePercent(settings, AudioVolumeCategory.PlayerVehicleEngine, percent);
        }

        private static void SetPlayerEventVolumePercent(RaceSettings settings, AudioSourceHandle? sound, int percent)
        {
            sound.SetVolumePercent(settings, AudioVolumeCategory.PlayerVehicleEvents, percent);
        }

        private static void SetSurfaceLoopVolumePercent(RaceSettings settings, AudioSourceHandle? sound, int percent)
        {
            sound.SetVolumePercent(settings, AudioVolumeCategory.SurfaceLoops, percent);
        }
    }
}

