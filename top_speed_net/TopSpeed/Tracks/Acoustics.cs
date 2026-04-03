using System;
using TopSpeed.Data;
using TS.Audio;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        private void ApplySegmentAcoustics(int segmentIndex)
        {
            _activeAudioSegmentIndex = segmentIndex;
            var room = ResolveRoomAcoustics(segmentIndex);
            if (RoomEquals(_activeRoomAcoustics, room))
                return;

            _activeRoomAcoustics = room;
            _audio.SetRoomAcoustics(room);
        }

        private RoomAcoustics ResolveRoomAcoustics(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= _segmentCount)
                return RoomAcoustics.Default;

            var definition = _definition[segmentIndex];
            TrackRoomDefinition? roomDefinition = null;
            if (!string.IsNullOrWhiteSpace(definition.RoomId))
            {
                if (_roomProfiles.TryGetValue(definition.RoomId!, out var room))
                    roomDefinition = room;
                else if (TrackRoomLibrary.TryGetPreset(definition.RoomId!, out var preset))
                    roomDefinition = preset;
            }

            var acoustics = roomDefinition == null
                ? RoomAcoustics.Default
                : ToRoomAcoustics(roomDefinition);

            if (definition.RoomOverrides != null)
                ApplyRoomOverrides(ref acoustics, definition.RoomOverrides);

            return acoustics;
        }

        private static RoomAcoustics ToRoomAcoustics(TrackRoomDefinition room)
        {
            return new RoomAcoustics
            {
                HasRoom = true,
                ReverbTimeSeconds = room.ReverbTimeSeconds,
                ReverbGain = room.ReverbGain,
                HfDecayRatio = room.HfDecayRatio,
                LateReverbGain = room.LateReverbGain,
                Diffusion = room.Diffusion,
                AirAbsorptionScale = room.AirAbsorption,
                OcclusionScale = room.OcclusionScale,
                TransmissionScale = room.TransmissionScale,
                OcclusionOverride = room.OcclusionOverride,
                TransmissionOverrideLow = room.TransmissionOverrideLow,
                TransmissionOverrideMid = room.TransmissionOverrideMid,
                TransmissionOverrideHigh = room.TransmissionOverrideHigh,
                AirAbsorptionOverrideLow = room.AirAbsorptionOverrideLow,
                AirAbsorptionOverrideMid = room.AirAbsorptionOverrideMid,
                AirAbsorptionOverrideHigh = room.AirAbsorptionOverrideHigh
            };
        }

        private static void ApplyRoomOverrides(ref RoomAcoustics acoustics, TrackRoomOverrides overrides)
        {
            acoustics.HasRoom = true;
            if (overrides.ReverbTimeSeconds.HasValue) acoustics.ReverbTimeSeconds = overrides.ReverbTimeSeconds.Value;
            if (overrides.ReverbGain.HasValue) acoustics.ReverbGain = overrides.ReverbGain.Value;
            if (overrides.HfDecayRatio.HasValue) acoustics.HfDecayRatio = overrides.HfDecayRatio.Value;
            if (overrides.LateReverbGain.HasValue) acoustics.LateReverbGain = overrides.LateReverbGain.Value;
            if (overrides.Diffusion.HasValue) acoustics.Diffusion = overrides.Diffusion.Value;
            if (overrides.AirAbsorption.HasValue) acoustics.AirAbsorptionScale = overrides.AirAbsorption.Value;
            if (overrides.OcclusionScale.HasValue) acoustics.OcclusionScale = overrides.OcclusionScale.Value;
            if (overrides.TransmissionScale.HasValue) acoustics.TransmissionScale = overrides.TransmissionScale.Value;
            if (overrides.OcclusionOverride.HasValue) acoustics.OcclusionOverride = overrides.OcclusionOverride.Value;
            if (overrides.TransmissionOverrideLow.HasValue) acoustics.TransmissionOverrideLow = overrides.TransmissionOverrideLow.Value;
            if (overrides.TransmissionOverrideMid.HasValue) acoustics.TransmissionOverrideMid = overrides.TransmissionOverrideMid.Value;
            if (overrides.TransmissionOverrideHigh.HasValue) acoustics.TransmissionOverrideHigh = overrides.TransmissionOverrideHigh.Value;
            if (overrides.AirAbsorptionOverrideLow.HasValue) acoustics.AirAbsorptionOverrideLow = overrides.AirAbsorptionOverrideLow.Value;
            if (overrides.AirAbsorptionOverrideMid.HasValue) acoustics.AirAbsorptionOverrideMid = overrides.AirAbsorptionOverrideMid.Value;
            if (overrides.AirAbsorptionOverrideHigh.HasValue) acoustics.AirAbsorptionOverrideHigh = overrides.AirAbsorptionOverrideHigh.Value;
        }

        private static bool RoomEquals(RoomAcoustics a, RoomAcoustics b)
        {
            return a.HasRoom == b.HasRoom &&
                   AreClose(a.ReverbTimeSeconds, b.ReverbTimeSeconds) &&
                   AreClose(a.ReverbGain, b.ReverbGain) &&
                   AreClose(a.HfDecayRatio, b.HfDecayRatio) &&
                   AreClose(a.LateReverbGain, b.LateReverbGain) &&
                   AreClose(a.Diffusion, b.Diffusion) &&
                   AreClose(a.AirAbsorptionScale, b.AirAbsorptionScale) &&
                   AreClose(a.OcclusionScale, b.OcclusionScale) &&
                   AreClose(a.TransmissionScale, b.TransmissionScale) &&
                   NullableClose(a.OcclusionOverride, b.OcclusionOverride) &&
                   NullableClose(a.TransmissionOverrideLow, b.TransmissionOverrideLow) &&
                   NullableClose(a.TransmissionOverrideMid, b.TransmissionOverrideMid) &&
                   NullableClose(a.TransmissionOverrideHigh, b.TransmissionOverrideHigh) &&
                   NullableClose(a.AirAbsorptionOverrideLow, b.AirAbsorptionOverrideLow) &&
                   NullableClose(a.AirAbsorptionOverrideMid, b.AirAbsorptionOverrideMid) &&
                   NullableClose(a.AirAbsorptionOverrideHigh, b.AirAbsorptionOverrideHigh);
        }

        private static bool NullableClose(float? a, float? b)
        {
            if (!a.HasValue && !b.HasValue)
                return true;
            if (!a.HasValue || !b.HasValue)
                return false;
            return AreClose(a.Value, b.Value);
        }

        private static bool AreClose(float a, float b)
        {
            return Math.Abs(a - b) < 0.0001f;
        }
    }
}

