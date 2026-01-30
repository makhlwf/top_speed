using System;

namespace TopSpeed.Tracks.Rooms
{
    public sealed class TrackRoomDefinition
    {
        public TrackRoomDefinition(
            string id,
            string? name,
            float reverbTimeSeconds,
            float reverbGain,
            float reflectionWet,
            float hfDecayRatio,
            float earlyReflectionsGain,
            float lateReverbGain,
            float diffusion,
            float airAbsorption,
            float occlusionScale,
            float transmissionScale,
            float? occlusionOverride = null,
            float? transmissionOverrideLow = null,
            float? transmissionOverrideMid = null,
            float? transmissionOverrideHigh = null,
            float? airAbsorptionOverrideLow = null,
            float? airAbsorptionOverrideMid = null,
            float? airAbsorptionOverrideHigh = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Room id is required.", nameof(id));

            Id = id.Trim();
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            ReverbTimeSeconds = Math.Max(0f, reverbTimeSeconds);
            ReverbGain = Clamp01(reverbGain);
            ReflectionWet = Clamp01(reflectionWet);
            HfDecayRatio = Clamp01(hfDecayRatio);
            EarlyReflectionsGain = Clamp01(earlyReflectionsGain);
            LateReverbGain = Clamp01(lateReverbGain);
            Diffusion = Clamp01(diffusion);
            AirAbsorption = Clamp01(airAbsorption);
            OcclusionScale = Clamp01(occlusionScale);
            TransmissionScale = Clamp01(transmissionScale);
            OcclusionOverride = ClampOptional01(occlusionOverride);
            TransmissionOverrideLow = ClampOptional01(transmissionOverrideLow);
            TransmissionOverrideMid = ClampOptional01(transmissionOverrideMid);
            TransmissionOverrideHigh = ClampOptional01(transmissionOverrideHigh);
            AirAbsorptionOverrideLow = ClampOptional01(airAbsorptionOverrideLow);
            AirAbsorptionOverrideMid = ClampOptional01(airAbsorptionOverrideMid);
            AirAbsorptionOverrideHigh = ClampOptional01(airAbsorptionOverrideHigh);
        }

        public string Id { get; }
        public string? Name { get; }
        public float ReverbTimeSeconds { get; }
        public float ReverbGain { get; }
        public float ReflectionWet { get; }
        public float HfDecayRatio { get; }
        public float EarlyReflectionsGain { get; }
        public float LateReverbGain { get; }
        public float Diffusion { get; }
        public float AirAbsorption { get; }
        public float OcclusionScale { get; }
        public float TransmissionScale { get; }
        public float? OcclusionOverride { get; }
        public float? TransmissionOverrideLow { get; }
        public float? TransmissionOverrideMid { get; }
        public float? TransmissionOverrideHigh { get; }
        public float? AirAbsorptionOverrideLow { get; }
        public float? AirAbsorptionOverrideMid { get; }
        public float? AirAbsorptionOverrideHigh { get; }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        private static float? ClampOptional01(float? value)
        {
            if (!value.HasValue)
                return null;
            return Clamp01(value.Value);
        }
    }

    public sealed class TrackRoomOverrides
    {
        public float? ReverbTimeSeconds { get; set; }
        public float? ReverbGain { get; set; }
        public float? ReflectionWet { get; set; }
        public float? HfDecayRatio { get; set; }
        public float? EarlyReflectionsGain { get; set; }
        public float? LateReverbGain { get; set; }
        public float? Diffusion { get; set; }
        public float? AirAbsorption { get; set; }
        public float? OcclusionScale { get; set; }
        public float? TransmissionScale { get; set; }
        public float? OcclusionOverride { get; set; }
        public float? TransmissionOverrideLow { get; set; }
        public float? TransmissionOverrideMid { get; set; }
        public float? TransmissionOverrideHigh { get; set; }
        public float? AirAbsorptionOverrideLow { get; set; }
        public float? AirAbsorptionOverrideMid { get; set; }
        public float? AirAbsorptionOverrideHigh { get; set; }

        public bool HasAny =>
            ReverbTimeSeconds.HasValue ||
            ReverbGain.HasValue ||
            ReflectionWet.HasValue ||
            HfDecayRatio.HasValue ||
            EarlyReflectionsGain.HasValue ||
            LateReverbGain.HasValue ||
            Diffusion.HasValue ||
            AirAbsorption.HasValue ||
            OcclusionScale.HasValue ||
            TransmissionScale.HasValue ||
            OcclusionOverride.HasValue ||
            TransmissionOverrideLow.HasValue ||
            TransmissionOverrideMid.HasValue ||
            TransmissionOverrideHigh.HasValue ||
            AirAbsorptionOverrideLow.HasValue ||
            AirAbsorptionOverrideMid.HasValue ||
            AirAbsorptionOverrideHigh.HasValue;
    }
}
