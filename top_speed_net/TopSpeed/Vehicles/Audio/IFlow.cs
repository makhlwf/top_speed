using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TS.Audio;

namespace TopSpeed.Vehicles.Audio
{
    internal interface IFlow
    {
        void RefreshVolumes(
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
            ref int lastSurfaceLoopVolumePercent);

        void UpdateHorn(AudioSourceHandle soundHorn, CarState state, bool horning);
        void UpdateRoad(
            TrackSurface surface,
            float speed,
            ref int surfaceFrequency,
            ref int prevSurfaceFrequency,
            AudioSourceHandle soundAsphalt,
            AudioSourceHandle soundGravel,
            AudioSourceHandle soundWater,
            AudioSourceHandle soundSand,
            AudioSourceHandle soundSnow);

        void ApplyPan(
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
            AudioSourceHandle soundSnow);

        int CalculatePan(float relativePosition);

        void Pause(
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
            System.Action stopResetBackfireVariants);

        void Unpause(
            TrackSurface surface,
            AudioSourceHandle soundEngine,
            AudioSourceHandle? soundThrottle,
            AudioSourceHandle? soundWipers,
            AudioSourceHandle soundAsphalt,
            AudioSourceHandle soundGravel,
            AudioSourceHandle soundWater,
            AudioSourceHandle soundSand,
            AudioSourceHandle soundSnow);
    }
}

