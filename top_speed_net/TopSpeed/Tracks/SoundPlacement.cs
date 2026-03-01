using System;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Data;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        private void UpdateTrackSoundPlacement(RuntimeTrackSound runtime, float position, int segmentIndex)
        {
            var handle = runtime.Handle;
            if (handle == null)
                return;

            var definition = runtime.ActiveDefinition;
            if (!definition.Spatial)
            {
                handle.SetPan(definition.Pan);
                return;
            }

            var sourcePos = ComputeTrackSoundPosition(runtime, position, segmentIndex);
            handle.SetPosition(sourcePos);
            handle.SetVelocity(Vector3.Zero);
        }

        private Vector3 ComputeTrackSoundPosition(RuntimeTrackSound runtime, float playerPosition, int segmentIndex)
        {
            var definition = runtime.ActiveDefinition;
            var lapPos = _lapDistance > 0f ? WrapPosition(playerPosition) : playerPosition;
            var segmentStart = GetSegmentStartDistance(segmentIndex);
            var segmentLength = segmentIndex >= 0 && segmentIndex < _definition.Length
                ? _definition[segmentIndex].Length
                : MinPartLengthMeters;
            var segmentCenter = segmentStart + (segmentLength * 0.5f);

            if (definition.Type == TrackSoundSourceType.Moving &&
                TryComputeMovingSoundPosition(definition, playerPosition, segmentIndex, out var movingPosition))
            {
                var wrappedZ = WrapWorldZ(movingPosition.Z, lapPos, playerPosition);
                return new Vector3(
                    AudioWorld.ToMeters(movingPosition.X),
                    AudioWorld.ToMeters(movingPosition.Y),
                    AudioWorld.ToMeters(wrappedZ));
            }

            if (definition.StartPosition.HasValue && definition.EndPosition.HasValue)
            {
                var t = ComputeAreaProgress(segmentIndex, definition);
                if (t <= 0f &&
                    definition.SpeedMetersPerSecond.HasValue &&
                    Math.Abs(definition.SpeedMetersPerSecond.Value) > 0.0001f &&
                    _lapDistance > 0f &&
                    definition.StartAreaId == null &&
                    definition.EndAreaId == null)
                {
                    var phase = (WrapPosition(playerPosition) * definition.SpeedMetersPerSecond.Value) / _lapDistance;
                    t = phase - (float)Math.Floor(phase);
                }

                var start = definition.StartPosition.Value;
                var end = definition.EndPosition.Value;
                var x = Lerp(start.X, end.X, t);
                var y = Lerp(start.Y, end.Y, t);
                var z = Lerp(start.Z, end.Z, t);
                var wrappedZ = WrapWorldZ(z, lapPos, playerPosition);
                return new Vector3(AudioWorld.ToMeters(x), AudioWorld.ToMeters(y), AudioWorld.ToMeters(wrappedZ));
            }

            if (definition.Position.HasValue)
            {
                var pos = definition.Position.Value;
                var wrappedZ = WrapWorldZ(pos.Z, lapPos, playerPosition);
                return new Vector3(AudioWorld.ToMeters(pos.X), AudioWorld.ToMeters(pos.Y), AudioWorld.ToMeters(wrappedZ));
            }

            var xDefault = 0f;
            var zDefault = WrapWorldZ(segmentCenter, lapPos, playerPosition);
            return new Vector3(AudioWorld.ToMeters(xDefault), 0f, AudioWorld.ToMeters(zDefault));
        }

        private bool TryComputeMovingSoundPosition(
            TrackSoundSourceDefinition definition,
            float playerPosition,
            int segmentIndex,
            out Vector3 position)
        {
            position = default;
            var speed = definition.SpeedMetersPerSecond ?? 0f;
            if (Math.Abs(speed) <= 0.0001f)
                return false;

            var pathLength = _lapDistance > 0f ? _lapDistance : 0f;
            var hasAreaSpan = TryResolveAreaSpan(definition, out var areaStartZ, out _, out var areaLength);
            if (hasAreaSpan)
                pathLength = areaLength;
            if (pathLength <= 0f)
                return false;

            var phase = (WrapPosition(playerPosition) * speed) / pathLength;
            phase -= (float)Math.Floor(phase);
            if (phase < 0f)
                phase += 1f;

            if (definition.StartPosition.HasValue && definition.EndPosition.HasValue)
            {
                var start = definition.StartPosition.Value;
                var end = definition.EndPosition.Value;
                position = new Vector3(
                    Lerp(start.X, end.X, phase),
                    Lerp(start.Y, end.Y, phase),
                    Lerp(start.Z, end.Z, phase));
                return true;
            }

            if (definition.Position.HasValue)
            {
                var anchor = definition.Position.Value;
                var travel = pathLength * phase;
                var z = hasAreaSpan ? (areaStartZ + travel) : (anchor.Z + travel);
                if (_lapDistance > 0f)
                    z = WrapPosition(z);

                position = new Vector3(anchor.X, anchor.Y, z);
                return true;
            }

            var fallbackZ = GetSegmentCenterDistance(segmentIndex) + (pathLength * phase);
            if (_lapDistance > 0f)
                fallbackZ = WrapPosition(fallbackZ);
            position = new Vector3(0f, 0f, fallbackZ);
            return true;
        }

        private bool TryResolveAreaSpan(TrackSoundSourceDefinition definition, out float startZ, out float endZ, out float pathLength)
        {
            startZ = 0f;
            endZ = 0f;
            pathLength = 0f;
            if (string.IsNullOrWhiteSpace(definition.StartAreaId) || string.IsNullOrWhiteSpace(definition.EndAreaId))
                return false;

            if (!_segmentIndexById.TryGetValue(definition.StartAreaId!, out var startIndex))
                return false;
            if (!_segmentIndexById.TryGetValue(definition.EndAreaId!, out var endIndex))
                return false;

            startZ = GetSegmentCenterDistance(startIndex);
            endZ = GetSegmentCenterDistance(endIndex);

            if (_lapDistance > 0f)
            {
                pathLength = endZ - startZ;
                if (pathLength < 0f)
                    pathLength += _lapDistance;
                if (pathLength <= 0f)
                    pathLength = _definition[endIndex].Length;
            }
            else
            {
                pathLength = Math.Max(0.001f, Math.Abs(endZ - startZ));
            }

            return pathLength > 0f;
        }

        private float GetSegmentStartDistance(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= _definition.Length)
                return 0f;

            if (_lapDistance > 0f && segmentIndex < _segmentStartDistances.Length)
                return _segmentStartDistances[segmentIndex];

            var start = 0f;
            for (var i = 0; i < segmentIndex; i++)
                start += _definition[i].Length;
            return start;
        }

        private float GetSegmentCenterDistance(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= _definition.Length)
                return 0f;
            return GetSegmentStartDistance(segmentIndex) + (_definition[segmentIndex].Length * 0.5f);
        }

        private float ComputeAreaProgress(int segmentIndex, TrackSoundSourceDefinition definition)
        {
            if (segmentIndex < 0 || segmentIndex >= _segmentCount)
                return 0f;

            if (definition.StartAreaId == null || definition.EndAreaId == null)
                return 0f;

            if (!_segmentIndexById.TryGetValue(definition.StartAreaId, out var startIndex))
                return 0f;
            if (!_segmentIndexById.TryGetValue(definition.EndAreaId, out var endIndex))
                return 0f;

            if (startIndex == endIndex)
                return 0f;

            var span = (endIndex - startIndex + _segmentCount) % _segmentCount;
            if (span == 0)
                return 0f;

            var delta = (segmentIndex - startIndex + _segmentCount) % _segmentCount;
            var t = delta / (float)span;
            if (t < 0f)
                t = 0f;
            if (t > 1f)
                t = 1f;
            return t;
        }
    }
}
