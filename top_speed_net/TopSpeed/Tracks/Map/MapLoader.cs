using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Tracks.Areas;
using TopSpeed.Tracks.Topology;

namespace TopSpeed.Tracks.Map
{
    internal static class TrackMapLoader
    {
        private const string MapExtension = ".tsm";

        public static bool LooksLikeMap(string nameOrPath)
        {
            if (string.IsNullOrWhiteSpace(nameOrPath))
                return false;
            if (Path.HasExtension(nameOrPath))
                return string.Equals(Path.GetExtension(nameOrPath), MapExtension, StringComparison.OrdinalIgnoreCase);
            return false;
        }

        public static bool TryResolvePath(string nameOrPath, out string path)
        {
            path = string.Empty;
            if (string.IsNullOrWhiteSpace(nameOrPath))
                return false;

            if (nameOrPath.IndexOfAny(new[] { '\\', '/' }) >= 0)
            {
                path = nameOrPath;
                return File.Exists(path) && LooksLikeMap(path);
            }

            if (!Path.HasExtension(nameOrPath))
            {
                path = Path.Combine(AssetPaths.Root, "Tracks", nameOrPath + MapExtension);
                return File.Exists(path);
            }

            path = Path.Combine(AssetPaths.Root, "Tracks", nameOrPath);
            return File.Exists(path) && LooksLikeMap(path);
        }

        public static TrackMap Load(string nameOrPath)
        {
            var path = ResolvePath(nameOrPath);
            if (!File.Exists(path))
                throw new FileNotFoundException("Track map not found.", path);

            var definition = TrackMapFormat.Parse(path);

            var map = new TrackMap(definition.Metadata.Name, definition.Metadata.CellSizeMeters)
            {
                Weather = definition.Metadata.Weather,
                Ambience = definition.Metadata.Ambience,
                DefaultSurface = definition.Metadata.DefaultSurface,
                DefaultNoise = definition.Metadata.DefaultNoise,
                DefaultWidthMeters = definition.Metadata.DefaultWidthMeters,
                StartX = definition.Metadata.StartX,
                StartZ = definition.Metadata.StartZ,
                StartHeadingDegrees = definition.Metadata.StartHeadingDegrees,
                StartHeading = definition.Metadata.StartHeading
            };

            foreach (var sector in definition.Sectors)
                map.AddSector(sector);
            foreach (var area in definition.Areas)
                map.AddArea(area);
            foreach (var shape in definition.Shapes)
                map.AddShape(shape);
            foreach (var portal in definition.Portals)
                map.AddPortal(portal);
            foreach (var link in definition.Links)
                map.AddLink(link);
            foreach (var pathDef in definition.Paths)
                map.AddPath(pathDef);
            foreach (var beacon in definition.Beacons)
                map.AddBeacon(beacon);
            foreach (var marker in definition.Markers)
                map.AddMarker(marker);
            foreach (var approach in definition.Approaches)
                map.AddApproach(approach);

            AddSafeZoneRing(map, definition.Metadata);
            AddOuterRing(map, definition.Metadata);
            ApplyStartFromAreas(map, definition);
            ApplyFinishFromAreas(map, definition);

            return map;
        }

        private static string ResolvePath(string nameOrPath)
        {
            if (string.IsNullOrWhiteSpace(nameOrPath))
                return nameOrPath;
            if (nameOrPath.IndexOfAny(new[] { '\\', '/' }) >= 0)
                return nameOrPath;
            if (!Path.HasExtension(nameOrPath))
                return Path.Combine(AssetPaths.Root, "Tracks", nameOrPath + MapExtension);
            return Path.Combine(AssetPaths.Root, "Tracks", nameOrPath);
        }

        private static void AddSafeZoneRing(TrackMap map, TrackMapMetadata metadata)
        {
            if (map == null || metadata == null)
                return;

            var ringMeters = metadata.SafeZoneRingMeters;
            if (ringMeters <= 0f)
                return;

            if (!TryGetTopologyBounds(map, out var minX, out var minZ, out var maxX, out var maxZ))
                return;

            var innerMinX = minX;
            var innerMaxX = maxX;
            var innerMinZ = minZ;
            var innerMaxZ = maxZ;

            var name = string.IsNullOrWhiteSpace(metadata.SafeZoneName) ? "Safe zone" : metadata.SafeZoneName!;
            var surface = metadata.SafeZoneSurface;
            var noise = metadata.SafeZoneNoise;
            var flags = TrackAreaFlags.SafeZone;

            AddRingShapeArea(map, "__safe_zone", innerMinX, innerMinZ, innerMaxX - innerMinX, innerMaxZ - innerMinZ, ringMeters, name, surface, noise, TrackAreaType.SafeZone, flags);
        }

        private static void AddOuterRing(TrackMap map, TrackMapMetadata metadata)
        {
            if (map == null || metadata == null)
                return;

            var ringMeters = metadata.OuterRingMeters;
            if (ringMeters <= 0f)
                return;

            if (!TryGetTopologyBounds(map, out var minX, out var minZ, out var maxX, out var maxZ))
                return;

            var innerMinX = minX;
            var innerMaxX = maxX;
            var innerMinZ = minZ;
            var innerMaxZ = maxZ;

            var name = string.IsNullOrWhiteSpace(metadata.OuterRingName) ? "Outer ring" : metadata.OuterRingName!;
            var surface = metadata.OuterRingSurface;
            var noise = metadata.OuterRingNoise;
            var flags = metadata.OuterRingFlags;
            var areaType = metadata.OuterRingType;

            AddRingShapeArea(map, "__outer_ring", innerMinX, innerMinZ, innerMaxX - innerMinX, innerMaxZ - innerMinZ, ringMeters, name, surface, noise, areaType, flags);
        }

        private static void AddRingShapeArea(
            TrackMap map,
            string idPrefix,
            float innerMinX,
            float innerMinZ,
            float innerWidth,
            float innerHeight,
            float ringWidth,
            string name,
            TrackSurface surface,
            TrackNoise noise,
            TrackAreaType areaType,
            TrackAreaFlags flags)
        {
            if (ringWidth <= 0f || innerWidth <= 0f || innerHeight <= 0f)
                return;

            var shapeId = idPrefix + "_shape";
            var areaId = idPrefix + "_area";
            map.AddShape(new ShapeDefinition(shapeId, ShapeType.Ring, innerMinX, innerMinZ, innerWidth, innerHeight, ringWidth: ringWidth));
            map.AddArea(new TrackAreaDefinition(areaId, areaType, shapeId, name, surface, noise, null, flags));
        }

        private static void ApplyStartFromAreas(TrackMap map, TrackMapDefinition definition)
        {
            if (map == null || definition == null)
                return;

            TrackAreaDefinition? startArea = null;
            foreach (var area in definition.Areas)
            {
                if (area != null && area.Type == TrackAreaType.Start)
                {
                    startArea = area;
                    break;
                }
            }

            if (startArea == null)
                return;

            map.StartAreaId = startArea.Id;

            if (TryGetStartPosition(startArea, out var startPos) ||
                TryGetAreaCenter(definition, startArea, out startPos))
            {
                map.StartX = startPos.X;
                map.StartZ = startPos.Y;
            }

            if (TryGetStartHeading(startArea, out var headingDegrees))
            {
                map.StartHeadingDegrees = MapMovement.NormalizeDegrees(headingDegrees);
                map.StartHeading = MapMovement.ToCardinal(map.StartHeadingDegrees);
            }
        }

        private static void ApplyFinishFromAreas(TrackMap map, TrackMapDefinition definition)
        {
            if (map == null || definition == null)
                return;

            foreach (var area in definition.Areas)
            {
                if (area != null && area.Type == TrackAreaType.Finish)
                {
                    map.FinishAreaId = area.Id;
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(map.StartAreaId))
            {
                map.FinishAreaId = map.StartAreaId;
                return;
            }

            foreach (var area in definition.Areas)
            {
                if (area != null && area.Type == TrackAreaType.Start)
                {
                    map.FinishAreaId = area.Id;
                    return;
                }
            }
        }

        private static bool TryGetStartPosition(TrackAreaDefinition area, out System.Numerics.Vector2 position)
        {
            position = default;
            if (area?.Metadata == null || area.Metadata.Count == 0)
                return false;

            if (!TryGetFloat(area.Metadata, out var x, "start_x", "spawn_x", "x") ||
                !TryGetFloat(area.Metadata, out var z, "start_z", "spawn_z", "z"))
                return false;

            position = new System.Numerics.Vector2(x, z);
            return true;
        }

        private static bool TryGetStartHeading(TrackAreaDefinition area, out float headingDegrees)
        {
            headingDegrees = 0f;
            if (area?.Metadata == null || area.Metadata.Count == 0)
                return false;

            if (!TryGetString(area.Metadata, out var raw, "start_heading", "heading", "grid_heading", "orientation"))
                return false;

            if (TryParseHeading(raw, out var parsed))
            {
                headingDegrees = MapMovement.HeadingFromDirection(parsed);
                return true;
            }

            if (TryParseDegrees(raw, out var degrees))
            {
                headingDegrees = MapMovement.NormalizeDegrees(degrees);
                return true;
            }

            return false;
        }

        private static bool TryGetAreaCenter(TrackMapDefinition definition, TrackAreaDefinition area, out System.Numerics.Vector2 center)
        {
            center = default;
            if (definition == null || area == null || string.IsNullOrWhiteSpace(area.ShapeId))
                return false;

            ShapeDefinition? shape = null;
            foreach (var candidate in definition.Shapes)
            {
                if (candidate != null && string.Equals(candidate.Id, area.ShapeId, StringComparison.OrdinalIgnoreCase))
                {
                    shape = candidate;
                    break;
                }
            }

            if (shape == null)
                return false;

            switch (shape.Type)
            {
                case ShapeType.Rectangle:
                    var minX = Math.Min(shape.X, shape.X + shape.Width);
                    var maxX = Math.Max(shape.X, shape.X + shape.Width);
                    var minZ = Math.Min(shape.Z, shape.Z + shape.Height);
                    var maxZ = Math.Max(shape.Z, shape.Z + shape.Height);
                    center = new System.Numerics.Vector2((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f);
                    return true;
                case ShapeType.Circle:
                    center = new System.Numerics.Vector2(shape.X, shape.Z);
                    return true;
                case ShapeType.Ring:
                    if (shape.Radius > 0f)
                    {
                        center = new System.Numerics.Vector2(shape.X, shape.Z);
                        return true;
                    }
                    var rMinX = Math.Min(shape.X, shape.X + shape.Width);
                    var rMaxX = Math.Max(shape.X, shape.X + shape.Width);
                    var rMinZ = Math.Min(shape.Z, shape.Z + shape.Height);
                    var rMaxZ = Math.Max(shape.Z, shape.Z + shape.Height);
                    center = new System.Numerics.Vector2((rMinX + rMaxX) * 0.5f, (rMinZ + rMaxZ) * 0.5f);
                    return true;
                case ShapeType.Polygon:
                case ShapeType.Polyline:
                    if (shape.Points == null || shape.Points.Count == 0)
                        return false;
                    float sumX = 0f;
                    float sumZ = 0f;
                    foreach (var point in shape.Points)
                    {
                        sumX += point.X;
                        sumZ += point.Y;
                    }
                    center = new System.Numerics.Vector2(sumX / shape.Points.Count, sumZ / shape.Points.Count);
                    return true;
            }

            return false;
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
                    value = raw.Trim();
                    return true;
                }
            }
            return false;
        }

        private static bool TryParseHeading(string raw, out MapDirection heading)
        {
            heading = MapDirection.North;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            switch (raw.Trim().ToLowerInvariant())
            {
                case "n":
                case "north":
                    heading = MapDirection.North;
                    return true;
                case "e":
                case "east":
                    heading = MapDirection.East;
                    return true;
                case "s":
                case "south":
                    heading = MapDirection.South;
                    return true;
                case "w":
                case "west":
                    heading = MapDirection.West;
                    return true;
            }
            return false;
        }

        private static bool TryParseDegrees(string raw, out float degrees)
        {
            return float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out degrees);
        }

        private static bool TryGetTopologyBounds(TrackMap map, out float minX, out float minZ, out float maxX, out float maxZ)
        {
            minX = 0f;
            minZ = 0f;
            maxX = 0f;
            maxZ = 0f;
            var hasBounds = false;

            if (map.Shapes.Count > 0)
            {
                foreach (var shape in map.Shapes)
                {
                    if (shape == null || !TryGetShapeBounds(shape, out var sMinX, out var sMinZ, out var sMaxX, out var sMaxZ))
                        continue;
                    if (!hasBounds)
                    {
                        minX = sMinX;
                        minZ = sMinZ;
                        maxX = sMaxX;
                        maxZ = sMaxZ;
                        hasBounds = true;
                    }
                    else
                    {
                        if (sMinX < minX) minX = sMinX;
                        if (sMinZ < minZ) minZ = sMinZ;
                        if (sMaxX > maxX) maxX = sMaxX;
                        if (sMaxZ > maxZ) maxZ = sMaxZ;
                    }
                }
            }

            if (map.Portals.Count > 0)
            {
                foreach (var portal in map.Portals)
                {
                    if (portal == null)
                        continue;
                    if (!hasBounds)
                    {
                        minX = portal.X;
                        maxX = portal.X;
                        minZ = portal.Z;
                        maxZ = portal.Z;
                        hasBounds = true;
                        continue;
                    }
                    if (portal.X < minX) minX = portal.X;
                    if (portal.X > maxX) maxX = portal.X;
                    if (portal.Z < minZ) minZ = portal.Z;
                    if (portal.Z > maxZ) maxZ = portal.Z;
                }
            }

            return hasBounds;
        }

        private static bool TryGetShapeBounds(ShapeDefinition shape, out float minX, out float minZ, out float maxX, out float maxZ)
        {
            minX = 0f;
            minZ = 0f;
            maxX = 0f;
            maxZ = 0f;

            if (shape == null)
                return false;

            switch (shape.Type)
            {
                case ShapeType.Rectangle:
                    minX = Math.Min(shape.X, shape.X + shape.Width);
                    maxX = Math.Max(shape.X, shape.X + shape.Width);
                    minZ = Math.Min(shape.Z, shape.Z + shape.Height);
                    maxZ = Math.Max(shape.Z, shape.Z + shape.Height);
                    return true;
                case ShapeType.Circle:
                    minX = shape.X - shape.Radius;
                    maxX = shape.X + shape.Radius;
                    minZ = shape.Z - shape.Radius;
                    maxZ = shape.Z + shape.Radius;
                    return true;
                case ShapeType.Ring:
                    if (shape.Radius > 0f)
                    {
                        var outer = Math.Abs(shape.Radius) + Math.Abs(shape.RingWidth);
                        minX = shape.X - outer;
                        maxX = shape.X + outer;
                        minZ = shape.Z - outer;
                        maxZ = shape.Z + outer;
                        return true;
                    }
                    var ringMinX = Math.Min(shape.X, shape.X + shape.Width) - Math.Abs(shape.RingWidth);
                    var ringMaxX = Math.Max(shape.X, shape.X + shape.Width) + Math.Abs(shape.RingWidth);
                    var ringMinZ = Math.Min(shape.Z, shape.Z + shape.Height) - Math.Abs(shape.RingWidth);
                    var ringMaxZ = Math.Max(shape.Z, shape.Z + shape.Height) + Math.Abs(shape.RingWidth);
                    minX = ringMinX;
                    maxX = ringMaxX;
                    minZ = ringMinZ;
                    maxZ = ringMaxZ;
                    return true;
                case ShapeType.Polygon:
                case ShapeType.Polyline:
                    if (shape.Points == null || shape.Points.Count == 0)
                        return false;
                    minX = float.MaxValue;
                    minZ = float.MaxValue;
                    maxX = float.MinValue;
                    maxZ = float.MinValue;
                    foreach (var point in shape.Points)
                    {
                        if (point.X < minX) minX = point.X;
                        if (point.X > maxX) maxX = point.X;
                        if (point.Y < minZ) minZ = point.Y;
                        if (point.Y > maxZ) maxZ = point.Y;
                    }
                    return true;
            }

            return false;
        }

        private static bool TryGetRingBounds(
            ShapeDefinition shape,
            out float minX,
            out float minZ,
            out float maxX,
            out float maxZ)
        {
            minX = 0f;
            minZ = 0f;
            maxX = 0f;
            maxZ = 0f;

            var ringWidth = Math.Abs(shape.RingWidth);
            if (ringWidth <= 0f)
                return false;

            if (shape.Radius > 0f)
            {
                var inner = Math.Abs(shape.Radius);
                var outer = inner + ringWidth;
                minX = shape.X - outer;
                maxX = shape.X + outer;
                minZ = shape.Z - outer;
                maxZ = shape.Z + outer;
                return true;
            }

            var innerMinX = Math.Min(shape.X, shape.X + shape.Width);
            var innerMaxX = Math.Max(shape.X, shape.X + shape.Width);
            var innerMinZ = Math.Min(shape.Z, shape.Z + shape.Height);
            var innerMaxZ = Math.Max(shape.Z, shape.Z + shape.Height);
            if (innerMaxX <= innerMinX || innerMaxZ <= innerMinZ)
                return false;

            minX = innerMinX - ringWidth;
            maxX = innerMaxX + ringWidth;
            minZ = innerMinZ - ringWidth;
            maxZ = innerMaxZ + ringWidth;
            return true;
        }
    }
}
