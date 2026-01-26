using System;
using System.Collections.Generic;
using System.Numerics;
using TopSpeed.Tracks.Map;
using TopSpeed.Tracks.Topology;

namespace TopSpeed.Tracks.Guidance
{
    internal readonly struct TrackApproachCue
    {
        public TrackApproachCue(
            string sectorId,
            TrackApproachSide side,
            string portalId,
            Vector2 portalPosition,
            float targetHeadingDegrees,
            float deltaDegrees,
            float distanceMeters,
            float? widthMeters,
            float? lengthMeters,
            float? toleranceDegrees,
            bool passed)
        {
            SectorId = sectorId;
            Side = side;
            PortalId = portalId;
            PortalPosition = portalPosition;
            TargetHeadingDegrees = targetHeadingDegrees;
            DeltaDegrees = deltaDegrees;
            DistanceMeters = distanceMeters;
            WidthMeters = widthMeters;
            LengthMeters = lengthMeters;
            ToleranceDegrees = toleranceDegrees;
            Passed = passed;
        }

        public string SectorId { get; }
        public TrackApproachSide Side { get; }
        public string PortalId { get; }
        public Vector2 PortalPosition { get; }
        public float TargetHeadingDegrees { get; }
        public float DeltaDegrees { get; }
        public float DistanceMeters { get; }
        public float? WidthMeters { get; }
        public float? LengthMeters { get; }
        public float? ToleranceDegrees { get; }
        public bool Passed { get; }
    }

    internal sealed class TrackApproachBeacon
    {
        private readonly TrackPortalManager _portalManager;
        private readonly TrackApproachManager _approachManager;
        private readonly float _rangeMeters;

        public TrackApproachBeacon(TrackMap map, float rangeMeters = 50f)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            _portalManager = map.BuildPortalManager();
            _approachManager = new TrackApproachManager(map.Sectors, map.Approaches, _portalManager);
            _rangeMeters = Math.Max(1f, rangeMeters);
        }

        public float RangeMeters => _rangeMeters;

        public bool TryGetCue(Vector3 worldPosition, float headingDegrees, out TrackApproachCue cue)
        {
            cue = default;
            if (_approachManager.Approaches.Count == 0)
                return false;

            var position = new Vector2(worldPosition.X, worldPosition.Z);
            var best = default(Candidate);
            var hasBest = false;

            foreach (var approach in _approachManager.Approaches)
            {
                if (approach == null)
                    continue;

                var range = GetApproachRange(approach, _rangeMeters);
                if (IsSideEnabled(approach, TrackApproachSide.Entry))
                {
                    if (TryBuildCandidate(approach, TrackApproachSide.Entry, position, range, ref best, ref hasBest))
                        continue;
                }
                if (IsSideEnabled(approach, TrackApproachSide.Exit))
                    TryBuildCandidate(approach, TrackApproachSide.Exit, position, range, ref best, ref hasBest);
            }

            if (!hasBest)
                return false;

            var delta = DeltaDegrees(headingDegrees, best.TargetHeadingDegrees);
            var forward = HeadingToVector(best.TargetHeadingDegrees);
            var toPlayer = position - best.PortalPosition;
            var passed = Vector2.Dot(forward, toPlayer) > 0f;

            var sectorId = best.SectorId ?? string.Empty;
            var portalId = best.PortalId ?? string.Empty;
            cue = new TrackApproachCue(
                sectorId,
                best.Side,
                portalId,
                best.PortalPosition,
                best.TargetHeadingDegrees,
                delta,
                best.DistanceMeters,
                best.WidthMeters,
                best.LengthMeters,
                best.ToleranceDegrees,
                passed);
            return true;
        }

        private bool TryBuildCandidate(
            TrackApproachDefinition approach,
            TrackApproachSide side,
            Vector2 position,
            float rangeMeters,
            ref Candidate best,
            ref bool hasBest)
        {
            var portalId = side == TrackApproachSide.Entry ? approach.EntryPortalId : approach.ExitPortalId;
            var heading = side == TrackApproachSide.Entry ? approach.EntryHeadingDegrees : approach.ExitHeadingDegrees;
            if (!heading.HasValue || string.IsNullOrWhiteSpace(portalId))
                return false;
            if (!_portalManager.TryGetPortal(portalId!, out var portal))
                return false;

            var portalPos = new Vector2(portal.X, portal.Z);
            var distance = Vector2.Distance(position, portalPos);
            if (distance > rangeMeters)
                return false;

            if (!hasBest || distance < best.DistanceMeters)
            {
                best = new Candidate
                {
                    SectorId = approach.SectorId,
                    Side = side,
                    PortalId = portal.Id,
                    PortalPosition = portalPos,
                    TargetHeadingDegrees = heading.Value,
                    DistanceMeters = distance,
                    WidthMeters = approach.WidthMeters,
                    LengthMeters = approach.LengthMeters,
                    ToleranceDegrees = approach.AlignmentToleranceDegrees
                };
                hasBest = true;
            }

            return true;
        }

        private static float GetApproachRange(TrackApproachDefinition approach, float defaultRange)
        {
            if (approach?.Metadata == null || approach.Metadata.Count == 0)
                return defaultRange;

            if (TryGetFloat(approach.Metadata, out var range, "approach_range", "beacon_range", "range"))
                return Math.Max(1f, range);

            return defaultRange;
        }

        private static bool IsSideEnabled(TrackApproachDefinition approach, TrackApproachSide side)
        {
            if (approach?.Metadata == null || approach.Metadata.Count == 0)
                return true;

            if (TryGetString(approach.Metadata, out var raw, "approach_side", "approach_sides", "side"))
            {
                var trimmed = raw.Trim().ToLowerInvariant();
                if (trimmed.Contains("none") || trimmed.Contains("off") || trimmed.Contains("disabled"))
                    return false;
                var hasEntry = trimmed.Contains("entry");
                var hasExit = trimmed.Contains("exit");
                if (hasEntry || hasExit)
                    return side == TrackApproachSide.Entry ? hasEntry : hasExit;
            }

            if (TryGetBool(approach.Metadata, out var entryEnabled, "approach_entry", "entry_enabled", "entry_beacon"))
            {
                if (side == TrackApproachSide.Entry)
                    return entryEnabled;
            }
            if (TryGetBool(approach.Metadata, out var exitEnabled, "approach_exit", "exit_enabled", "exit_beacon"))
            {
                if (side == TrackApproachSide.Exit)
                    return exitEnabled;
            }

            return true;
        }

        private static bool TryGetFloat(
            IReadOnlyDictionary<string, string> metadata,
            out float value,
            params string[] keys)
        {
            value = 0f;
            if (metadata == null || metadata.Count == 0)
                return false;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw))
                    continue;
                if (float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                    return true;
            }
            return false;
        }

        private static bool TryGetBool(
            IReadOnlyDictionary<string, string> metadata,
            out bool value,
            params string[] keys)
        {
            value = false;
            if (metadata == null || metadata.Count == 0)
                return false;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw))
                    continue;
                if (TryParseBool(raw, out value))
                    return true;
            }
            return false;
        }

        private static bool TryGetString(
            IReadOnlyDictionary<string, string> metadata,
            out string value,
            params string[] keys)
        {
            value = string.Empty;
            if (metadata == null || metadata.Count == 0)
                return false;
            foreach (var key in keys)
            {
                if (metadata.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
                {
                    value = raw;
                    return true;
                }
            }
            return false;
        }

        private static bool TryParseBool(string raw, out bool value)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            var trimmed = raw.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                    value = true;
                    return true;
                case "0":
                case "false":
                case "no":
                case "off":
                    value = false;
                    return true;
            }
            return bool.TryParse(raw, out value);
        }

        private static float NormalizeDegrees(float degrees)
        {
            var result = degrees % 360f;
            if (result < 0f)
                result += 360f;
            return result;
        }

        private static float DeltaDegrees(float current, float target)
        {
            var diff = Math.Abs(NormalizeDegrees(current - target));
            return diff > 180f ? 360f - diff : diff;
        }

        private static Vector2 HeadingToVector(float headingDegrees)
        {
            var radians = headingDegrees * (float)Math.PI / 180f;
            return new Vector2((float)Math.Sin(radians), (float)Math.Cos(radians));
        }

        private struct Candidate
        {
            public string? SectorId;
            public TrackApproachSide Side;
            public string? PortalId;
            public Vector2 PortalPosition;
            public float TargetHeadingDegrees;
            public float DistanceMeters;
            public float? WidthMeters;
            public float? LengthMeters;
            public float? ToleranceDegrees;
        }
    }
}
