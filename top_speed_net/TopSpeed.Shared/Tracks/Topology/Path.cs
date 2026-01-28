using System;
using System.Collections.Generic;

namespace TopSpeed.Tracks.Topology
{
    public sealed class PathDefinition
    {
        private readonly List<PathLaneDefinition> _lanes;
        private readonly Dictionary<string, string> _metadata;

        public PathDefinition(
            string id,
            PathType type,
            string? shapeId,
            string? fromPortalId,
            string? toPortalId,
            float widthMeters,
            string? name = null,
            IReadOnlyDictionary<string, string>? metadata = null,
            IReadOnlyList<PathLaneDefinition>? lanes = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Path id is required.", nameof(id));

            Id = id.Trim();
            Type = type;
            ShapeId = string.IsNullOrWhiteSpace(shapeId) ? null : shapeId!.Trim();
            FromPortalId = string.IsNullOrWhiteSpace(fromPortalId) ? null : fromPortalId!.Trim();
            ToPortalId = string.IsNullOrWhiteSpace(toPortalId) ? null : toPortalId!.Trim();
            WidthMeters = widthMeters;
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            _metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (metadata != null)
            {
                foreach (var pair in metadata)
                    _metadata[pair.Key] = pair.Value;
            }
            _lanes = lanes != null ? new List<PathLaneDefinition>(lanes) : new List<PathLaneDefinition>();
        }

        public string Id { get; }
        public PathType Type { get; }
        public string? ShapeId { get; }
        public string? FromPortalId { get; }
        public string? ToPortalId { get; }
        public float WidthMeters { get; }
        public string? Name { get; }
        public IReadOnlyDictionary<string, string> Metadata => _metadata;
        public IReadOnlyList<PathLaneDefinition> Lanes => _lanes;

        public bool TryAddLane(PathLaneDefinition lane)
        {
            if (lane == null)
                throw new ArgumentNullException(nameof(lane));
            foreach (var existing in _lanes)
            {
                if (string.Equals(existing.Id, lane.Id, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            _lanes.Add(lane);
            return true;
        }
    }
}
