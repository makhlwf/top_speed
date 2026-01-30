using System;

namespace TS.Audio
{
    public struct RoomAcoustics
    {
        public bool HasRoom;
        public float ReverbTimeSeconds;
        public float ReverbGain;
        public float ReflectionWet;
        public float HfDecayRatio;
        public float EarlyReflectionsGain;
        public float LateReverbGain;
        public float Diffusion;
        public float AirAbsorptionScale;
        public float OcclusionScale;
        public float TransmissionScale;
        public float? OcclusionOverride;
        public float? TransmissionOverrideLow;
        public float? TransmissionOverrideMid;
        public float? TransmissionOverrideHigh;
        public float? AirAbsorptionOverrideLow;
        public float? AirAbsorptionOverrideMid;
        public float? AirAbsorptionOverrideHigh;

        public static RoomAcoustics Default => new RoomAcoustics
        {
            HasRoom = false,
            ReverbTimeSeconds = 0f,
            ReverbGain = 0f,
            ReflectionWet = 0.35f,
            HfDecayRatio = 1f,
            EarlyReflectionsGain = 0f,
            LateReverbGain = 0f,
            Diffusion = 0f,
            AirAbsorptionScale = 1f,
            OcclusionScale = 1f,
            TransmissionScale = 1f,
            OcclusionOverride = null,
            TransmissionOverrideLow = null,
            TransmissionOverrideMid = null,
            TransmissionOverrideHigh = null,
            AirAbsorptionOverrideLow = null,
            AirAbsorptionOverrideMid = null,
            AirAbsorptionOverrideHigh = null
        };
    }
}
