using System;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Data;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        private void ActivateTrackSoundsForPosition(float position, int segmentIndex)
        {
            if (_segmentTrackSounds.Count == 0)
                return;

            foreach (var runtime in _allTrackSounds)
            {
                var shouldPlay = ShouldPlayRuntimeSound(runtime, position, segmentIndex);
                if (!shouldPlay)
                {
                    runtime.Stop();
                    continue;
                }

                var refreshRandom =
                    runtime.Definition.Type == TrackSoundSourceType.Random &&
                    runtime.Definition.RandomMode == TrackSoundRandomMode.PerArea &&
                    runtime.LastAreaIndex != segmentIndex;

                if (!runtime.EnsureCreated(refreshRandom))
                    continue;

                if (runtime.Handle != null)
                {
                    UpdateTrackSoundPlacement(runtime, position, segmentIndex);
                    runtime.Play();
                }

                runtime.LastAreaIndex = segmentIndex;
            }
        }

        private bool ShouldPlayRuntimeSound(RuntimeTrackSound runtime, float position, int segmentIndex)
        {
            var definition = runtime.Definition;
            if (definition.Global)
                return true;

            var hasStartOrEndConditions = HasStartOrEndConditions(definition);
            if (hasStartOrEndConditions)
                return UpdateTriggerState(runtime, position, segmentIndex);

            if (IsSoundAssignedToSegment(segmentIndex, runtime.Id))
                return true;

            return IsSegmentInSoundArea(segmentIndex, definition);
        }

        private bool IsSoundAssignedToSegment(int segmentIndex, string soundId)
        {
            if (segmentIndex < 0 || segmentIndex >= _definition.Length)
                return false;

            var segment = _definition[segmentIndex];
            for (var i = 0; i < segment.SoundSourceIds.Count; i++)
            {
                if (string.Equals(segment.SoundSourceIds[i], soundId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool HasStartOrEndConditions(TrackSoundSourceDefinition definition)
        {
            return !string.IsNullOrWhiteSpace(definition.StartAreaId) ||
                   !string.IsNullOrWhiteSpace(definition.EndAreaId) ||
                   definition.StartPosition.HasValue ||
                   definition.EndPosition.HasValue;
        }

        private bool UpdateTriggerState(RuntimeTrackSound runtime, float position, int segmentIndex)
        {
            if (!runtime.TriggerInitialized)
            {
                runtime.TriggerInitialized = true;
                runtime.TriggerActive = false;
            }

            if (!runtime.TriggerActive)
            {
                runtime.TriggerActive = IsStartConditionMet(runtime.Definition, position, segmentIndex);
            }
            else if (IsEndConditionMet(runtime.Definition, position, segmentIndex))
            {
                runtime.TriggerActive = false;
            }

            return runtime.TriggerActive;
        }

        private bool IsStartConditionMet(TrackSoundSourceDefinition definition, float position, int segmentIndex)
        {
            var hasStartCondition = !string.IsNullOrWhiteSpace(definition.StartAreaId) || definition.StartPosition.HasValue;
            if (!hasStartCondition)
                return true;

            if (IsAreaConditionMet(segmentIndex, definition.StartAreaId))
                return true;

            if (definition.StartPosition.HasValue &&
                IsPositionConditionMet(position, definition.StartPosition.Value, definition.StartRadiusMeters))
            {
                return true;
            }

            return false;
        }

        private bool IsEndConditionMet(TrackSoundSourceDefinition definition, float position, int segmentIndex)
        {
            if (IsAreaConditionMet(segmentIndex, definition.EndAreaId))
                return true;

            if (definition.EndPosition.HasValue &&
                IsPositionConditionMet(position, definition.EndPosition.Value, definition.EndRadiusMeters))
            {
                return true;
            }

            return false;
        }

        private bool IsAreaConditionMet(int segmentIndex, string? areaId)
        {
            if (string.IsNullOrWhiteSpace(areaId))
                return false;
            if (!_segmentIndexById.TryGetValue(areaId!, out var areaSegment))
                return false;
            return areaSegment == segmentIndex;
        }

        private bool IsPositionConditionMet(float playerPosition, Vector3 targetPosition, float? radiusMeters)
        {
            var radius = radiusMeters ?? 1f;
            if (radius <= 0f)
                radius = 1f;

            var listenerZ = _lapDistance > 0f ? WrapPosition(playerPosition) : playerPosition;
            var dx = targetPosition.X;
            var dy = targetPosition.Y;
            var dz = _lapDistance > 0f
                ? AudioWorld.WrapDelta(targetPosition.Z - listenerZ, _lapDistance)
                : targetPosition.Z - listenerZ;

            var distanceSquared = (dx * dx) + (dy * dy) + (dz * dz);
            return distanceSquared <= (radius * radius);
        }

        private bool IsSegmentInSoundArea(int segmentIndex, TrackSoundSourceDefinition definition)
        {
            if (definition.StartAreaId == null && definition.EndAreaId == null)
                return false;

            if (segmentIndex < 0 || segmentIndex >= _segmentCount)
                return false;

            if (definition.StartAreaId == null || definition.EndAreaId == null)
            {
                if (definition.StartAreaId != null && _segmentIndexById.TryGetValue(definition.StartAreaId, out var startOnly))
                    return startOnly == segmentIndex;
                if (definition.EndAreaId != null && _segmentIndexById.TryGetValue(definition.EndAreaId, out var endOnly))
                    return endOnly == segmentIndex;
                return false;
            }

            if (!_segmentIndexById.TryGetValue(definition.StartAreaId, out var start))
                return false;
            if (!_segmentIndexById.TryGetValue(definition.EndAreaId, out var end))
                return false;

            if (start <= end)
                return segmentIndex >= start && segmentIndex <= end;

            return segmentIndex >= start || segmentIndex <= end;
        }
    }
}
