using System;
using System.Collections.Generic;
using System.Numerics;

namespace TopSpeed.Tracks.Topology
{
    public sealed class TrackPathInstance
    {
        public TrackPathInstance(PathDefinition definition, ShapeDefinition shape, float widthMeters)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Shape = shape ?? throw new ArgumentNullException(nameof(shape));
            WidthMeters = widthMeters;
        }

        public PathDefinition Definition { get; }
        public ShapeDefinition Shape { get; }
        public float WidthMeters { get; }
    }

    public sealed class TrackPathManager
    {
        private readonly List<TrackPathInstance> _paths = new List<TrackPathInstance>();

        public TrackPathManager(
            IEnumerable<PathDefinition> paths,
            IEnumerable<ShapeDefinition> shapes,
            TrackPortalManager portalManager,
            float defaultWidthMeters)
        {
            if (portalManager == null)
                throw new ArgumentNullException(nameof(portalManager));

            var shapeLookup = new Dictionary<string, ShapeDefinition>(StringComparer.OrdinalIgnoreCase);
            if (shapes != null)
            {
                foreach (var shape in shapes)
                {
                    if (shape == null)
                        continue;
                    shapeLookup[shape.Id] = shape;
                }
            }

            if (paths == null)
                return;

            foreach (var path in paths)
            {
                if (path == null)
                    continue;

                var width = path.WidthMeters > 0f ? path.WidthMeters : Math.Max(0.5f, defaultWidthMeters);
                if (!string.IsNullOrWhiteSpace(path.ShapeId) && shapeLookup.TryGetValue(path.ShapeId!, out var shape))
                {
                    _paths.Add(new TrackPathInstance(path, shape, width));
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(path.FromPortalId) &&
                    !string.IsNullOrWhiteSpace(path.ToPortalId) &&
                    portalManager.TryGetPortal(path.FromPortalId!, out var fromPortal) &&
                    portalManager.TryGetPortal(path.ToPortalId!, out var toPortal))
                {
                    var points = new List<Vector2>
                    {
                        new Vector2(fromPortal.X, fromPortal.Z),
                        new Vector2(toPortal.X, toPortal.Z)
                    };
                    var shapeId = $"{path.Id}_auto";
                    var autoShape = new ShapeDefinition(shapeId, ShapeType.Polyline, points: points);
                    _paths.Add(new TrackPathInstance(path, autoShape, width));
                }
            }
        }

        public IReadOnlyList<TrackPathInstance> Paths => _paths;

        public bool HasPaths => _paths.Count > 0;

        public IReadOnlyList<TrackPathInstance> FindPathsContaining(Vector2 position)
        {
            if (_paths.Count == 0)
                return Array.Empty<TrackPathInstance>();

            var hits = new List<TrackPathInstance>();
            foreach (var path in _paths)
            {
                if (Contains(path, position))
                    hits.Add(path);
            }
            return hits;
        }

        public bool ContainsAny(Vector2 position)
        {
            foreach (var path in _paths)
            {
                if (Contains(path, position))
                    return true;
            }
            return false;
        }

        private static bool Contains(TrackPathInstance path, Vector2 position)
        {
            if (path == null || path.Shape == null)
                return false;

            var shape = path.Shape;
            var width = Math.Abs(path.WidthMeters);

            switch (shape.Type)
            {
                case ShapeType.Rectangle:
                    return width > 0f
                        ? ContainsRectanglePath(shape, position, width)
                        : ContainsRectangle(shape, position);
                case ShapeType.Circle:
                    return width > 0f
                        ? ContainsCirclePath(shape, position, width)
                        : ContainsCircle(shape, position);
                case ShapeType.Ring:
                    return width > 0f
                        ? ContainsRingPath(shape, position, width)
                        : ContainsRing(shape, position);
                case ShapeType.Polygon:
                    return ContainsPolygonPath(shape.Points, position, width, path.Definition);
                case ShapeType.Polyline:
                    return ContainsPolylinePath(shape.Points, position, width, path.Definition);
                default:
                    return false;
            }
        }

        private static bool ContainsRectangle(ShapeDefinition shape, Vector2 position)
        {
            var minX = shape.X;
            var minZ = shape.Z;
            var maxX = shape.X + shape.Width;
            var maxZ = shape.Z + shape.Height;
            return position.X >= minX && position.X <= maxX &&
                   position.Y >= minZ && position.Y <= maxZ;
        }

        private static bool ContainsCircle(ShapeDefinition shape, Vector2 position)
        {
            var dx = position.X - shape.X;
            var dz = position.Y - shape.Z;
            return (dx * dx + dz * dz) <= (shape.Radius * shape.Radius);
        }

        private static bool ContainsRectanglePath(ShapeDefinition shape, Vector2 position, float widthMeters)
        {
            if (widthMeters <= 0f)
                return false;

            var minX = Math.Min(shape.X, shape.X + shape.Width);
            var maxX = Math.Max(shape.X, shape.X + shape.Width);
            var minZ = Math.Min(shape.Z, shape.Z + shape.Height);
            var maxZ = Math.Max(shape.Z, shape.Z + shape.Height);
            var centerX = (minX + maxX) * 0.5f;
            var centerZ = (minZ + maxZ) * 0.5f;
            var lengthX = Math.Abs(shape.Width);
            var lengthZ = Math.Abs(shape.Height);

            Vector2 a;
            Vector2 b;
            if (lengthX >= lengthZ)
            {
                a = new Vector2(minX, centerZ);
                b = new Vector2(maxX, centerZ);
            }
            else
            {
                a = new Vector2(centerX, minZ);
                b = new Vector2(centerX, maxZ);
            }

            var radius = widthMeters * 0.5f;
            return DistanceToSegmentSquared(a, b, position) <= (radius * radius);
        }

        private static bool ContainsCirclePath(ShapeDefinition shape, Vector2 position, float widthMeters)
        {
            var radius = Math.Abs(shape.Radius);
            if (radius <= 0f || widthMeters <= 0f)
                return false;

            var dist = Vector2.Distance(new Vector2(shape.X, shape.Z), position);
            var inner = Math.Max(0f, radius - widthMeters);
            return dist >= inner && dist <= radius;
        }

        private static bool ContainsRing(ShapeDefinition shape, Vector2 position)
        {
            var ringWidth = Math.Abs(shape.RingWidth);
            if (ringWidth <= 0f)
                return false;

            if (shape.Radius > 0f)
                return ContainsRingCircle(shape, position, ringWidth);

            return ContainsRingRectangle(shape, position, ringWidth);
        }

        private static bool ContainsRingPath(ShapeDefinition shape, Vector2 position, float widthMeters)
        {
            var ringWidth = Math.Abs(widthMeters);
            if (ringWidth <= 0f)
                return false;

            if (shape.Radius > 0f)
                return ContainsRingCircle(shape, position, ringWidth);

            return ContainsRingRectangle(shape, position, ringWidth);
        }

        private static bool ContainsRingCircle(ShapeDefinition shape, Vector2 position, float ringWidth)
        {
            var dx = position.X - shape.X;
            var dz = position.Y - shape.Z;
            var distSq = dx * dx + dz * dz;
            var inner = Math.Abs(shape.Radius);
            var outer = inner + ringWidth;
            return distSq >= (inner * inner) && distSq <= (outer * outer);
        }

        private static bool ContainsRingRectangle(ShapeDefinition shape, Vector2 position, float ringWidth)
        {
            var innerMinX = shape.X;
            var innerMinZ = shape.Z;
            var innerMaxX = shape.X + shape.Width;
            var innerMaxZ = shape.Z + shape.Height;
            if (innerMaxX <= innerMinX || innerMaxZ <= innerMinZ)
                return false;

            var outerMinX = innerMinX - ringWidth;
            var outerMinZ = innerMinZ - ringWidth;
            var outerMaxX = innerMaxX + ringWidth;
            var outerMaxZ = innerMaxZ + ringWidth;

            var insideOuter = position.X >= outerMinX && position.X <= outerMaxX &&
                              position.Y >= outerMinZ && position.Y <= outerMaxZ;
            if (!insideOuter)
                return false;

            var insideInner = position.X >= innerMinX && position.X <= innerMaxX &&
                              position.Y >= innerMinZ && position.Y <= innerMaxZ;
            return !insideInner;
        }

        private static bool ContainsPolygon(IReadOnlyList<Vector2> points, Vector2 position)
        {
            if (points == null || points.Count < 3)
                return false;

            var inside = false;
            var j = points.Count - 1;
            for (var i = 0; i < points.Count; i++)
            {
                var xi = points[i].X;
                var zi = points[i].Y;
                var xj = points[j].X;
                var zj = points[j].Y;

                var intersect = ((zi > position.Y) != (zj > position.Y)) &&
                                (position.X < (xj - xi) * (position.Y - zi) / (zj - zi + float.Epsilon) + xi);
                if (intersect)
                    inside = !inside;
                j = i;
            }

            return inside;
        }

        private static bool ContainsPolygonPath(
            IReadOnlyList<Vector2> points,
            Vector2 position,
            float widthMeters,
            PathDefinition? definition)
        {
            if (points == null || points.Count < 3)
                return false;

            var width = Math.Abs(widthMeters);
            if (width <= 0f)
                return ContainsPolygon(points, position);

            var centered = IsCenteredClosedWidth(definition);
            if (centered)
            {
                var radius = width * 0.5f;
                return DistanceToPolylineSquared(points, position, true) <= (radius * radius);
            }

            if (!ContainsPolygon(points, position))
                return false;
            return DistanceToPolylineSquared(points, position, true) <= (width * width);
        }

        private static bool ContainsPolylinePath(
            IReadOnlyList<Vector2> points,
            Vector2 position,
            float widthMeters,
            PathDefinition? definition)
        {
            if (points == null || points.Count < 2)
                return false;

            var width = Math.Abs(widthMeters);
            if (width <= 0f)
                return false;

            var closed = IsClosedPolyline(points);
            if (!closed)
            {
                var radius = width * 0.5f;
                return DistanceToPolylineSquared(points, position, false) <= (radius * radius);
            }

            var centered = IsCenteredClosedWidth(definition);
            if (centered)
            {
                var radius = width * 0.5f;
                return DistanceToPolylineSquared(points, position, true) <= (radius * radius);
            }

            if (!ContainsPolygon(points, position))
                return false;
            return DistanceToPolylineSquared(points, position, true) <= (width * width);
        }

        private static float DistanceToSegmentSquared(Vector2 a, Vector2 b, Vector2 p)
        {
            var ab = b - a;
            var ap = p - a;
            var abLenSq = Vector2.Dot(ab, ab);
            if (abLenSq <= float.Epsilon)
                return Vector2.Dot(ap, ap);

            var t = Vector2.Dot(ap, ab) / abLenSq;
            if (t < 0f)
                t = 0f;
            else if (t > 1f)
                t = 1f;

            var closest = a + ab * t;
            var delta = p - closest;
            return Vector2.Dot(delta, delta);
        }

        private static float DistanceToPolylineSquared(IReadOnlyList<Vector2> points, Vector2 position, bool closed)
        {
            if (points == null || points.Count < 2)
                return float.MaxValue;

            var count = points.Count;
            var lastIndex = count - 1;
            var lastEqualsFirst = count > 1 && Vector2.DistanceSquared(points[0], points[lastIndex]) <= 0.0001f;
            var segmentCount = lastEqualsFirst ? lastIndex : count - 1;

            var best = float.MaxValue;
            for (var i = 0; i < segmentCount; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var dist = DistanceToSegmentSquared(a, b, position);
                if (dist < best)
                    best = dist;
            }

            if (closed && !lastEqualsFirst)
            {
                var dist = DistanceToSegmentSquared(points[lastIndex], points[0], position);
                if (dist < best)
                    best = dist;
            }

            return best;
        }

        private static bool IsClosedPolyline(IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count < 3)
                return false;
            return Vector2.DistanceSquared(points[0], points[points.Count - 1]) <= 0.0001f;
        }

        private static bool IsCenteredClosedWidth(PathDefinition? definition)
        {
            if (definition?.Metadata == null || definition.Metadata.Count == 0)
                return false;

            if (!TryGetMetadataValue(definition.Metadata, out var mode, "width_mode", "path_width_mode", "width_align", "width_alignment"))
                return false;

            var trimmed = mode.Trim().ToLowerInvariant();
            return trimmed.Contains("center") ||
                   trimmed.Contains("centre") ||
                   trimmed.Contains("both") ||
                   trimmed.Contains("sym");
        }

        private static bool TryGetMetadataValue(
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
    }
}
