using System;
using System.Collections.Generic;

namespace TopSpeed.Tracks.Topology
{
    public sealed class PathLaneDefinition
    {
        public PathLaneDefinition(
            string id,
            string pathId,
            float widthMeters,
            float offsetMeters,
            string? name = null,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Lane id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(pathId))
                throw new ArgumentException("Path id is required.", nameof(pathId));

            Id = id.Trim();
            PathId = pathId.Trim();
            WidthMeters = widthMeters;
            OffsetMeters = offsetMeters;
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string Id { get; }
        public string PathId { get; }
        public float WidthMeters { get; }
        public float OffsetMeters { get; }
        public string? Name { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }
    }
}
