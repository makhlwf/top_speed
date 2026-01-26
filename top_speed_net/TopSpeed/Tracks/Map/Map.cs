using System;
using System.Collections.Generic;
using System.Numerics;
using TopSpeed.Data;
using TopSpeed.Tracks.Areas;
using TopSpeed.Tracks.Beacons;
using TopSpeed.Tracks.Guidance;
using TopSpeed.Tracks.Markers;
using TopSpeed.Tracks.Sectors;
using TopSpeed.Tracks.Topology;

namespace TopSpeed.Tracks.Map
{
    internal sealed class TrackMap
    {
        private readonly Dictionary<CellKey, TrackMapCell> _cells;
        private readonly List<TrackSectorDefinition> _sectors;
        private readonly List<ShapeDefinition> _shapes;
        private readonly List<TrackAreaDefinition> _areas;
        private readonly List<PortalDefinition> _portals;
        private readonly List<LinkDefinition> _links;
        private readonly List<PathDefinition> _paths;
        private readonly List<TrackBeaconDefinition> _beacons;
        private readonly List<TrackMarkerDefinition> _markers;
        private readonly List<TrackApproachDefinition> _approaches;

        public TrackMap(string name, float cellSizeMeters)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Track" : name.Trim();
            CellSizeMeters = Math.Max(0.1f, cellSizeMeters);
            _cells = new Dictionary<CellKey, TrackMapCell>();
            _sectors = new List<TrackSectorDefinition>();
            _shapes = new List<ShapeDefinition>();
            _areas = new List<TrackAreaDefinition>();
            _portals = new List<PortalDefinition>();
            _links = new List<LinkDefinition>();
            _paths = new List<PathDefinition>();
            _beacons = new List<TrackBeaconDefinition>();
            _markers = new List<TrackMarkerDefinition>();
            _approaches = new List<TrackApproachDefinition>();
        }

        public string Name { get; }
        public float CellSizeMeters { get; }
        public int CellCount => _cells.Count;
        public IReadOnlyList<TrackSectorDefinition> Sectors => _sectors;
        public IReadOnlyList<TrackAreaDefinition> Areas => _areas;
        public IReadOnlyList<ShapeDefinition> Shapes => _shapes;
        public IReadOnlyList<PortalDefinition> Portals => _portals;
        public IReadOnlyList<LinkDefinition> Links => _links;
        public IReadOnlyList<PathDefinition> Paths => _paths;
        public IReadOnlyList<TrackBeaconDefinition> Beacons => _beacons;
        public IReadOnlyList<TrackMarkerDefinition> Markers => _markers;
        public IReadOnlyList<TrackApproachDefinition> Approaches => _approaches;
        public TrackWeather Weather { get; set; } = TrackWeather.Sunny;
        public TrackAmbience Ambience { get; set; } = TrackAmbience.NoAmbience;
        public TrackSurface DefaultSurface { get; set; } = TrackSurface.Asphalt;
        public TrackNoise DefaultNoise { get; set; } = TrackNoise.NoNoise;
        public float DefaultWidthMeters { get; set; } = 12f;
        public int StartX { get; set; }
        public int StartZ { get; set; }
        public MapDirection StartHeading { get; set; } = MapDirection.North;
        public string? StartAreaId { get; set; }
        public string? FinishAreaId { get; set; }

        public bool TryGetCell(int x, int z, out TrackMapCell cell)
        {
            return _cells.TryGetValue(new CellKey(x, z), out cell!);
        }

        public TrackMapCell GetOrCreateCell(int x, int z)
        {
            var key = new CellKey(x, z);
            if (_cells.TryGetValue(key, out var cell))
                return cell;

            cell = new TrackMapCell
            {
                Exits = MapExits.None,
                Surface = DefaultSurface,
                Noise = DefaultNoise,
                WidthMeters = DefaultWidthMeters,
                IsSafeZone = false
            };
            _cells[key] = cell;
            return cell;
        }

        public void AddSector(TrackSectorDefinition sector)
        {
            if (sector == null)
                throw new ArgumentNullException(nameof(sector));
            _sectors.Add(sector);
        }

        public void AddShape(ShapeDefinition shape)
        {
            if (shape == null)
                throw new ArgumentNullException(nameof(shape));
            _shapes.Add(shape);
        }

        public void AddArea(TrackAreaDefinition area)
        {
            if (area == null)
                throw new ArgumentNullException(nameof(area));
            _areas.Add(area);
        }

        public void AddPortal(PortalDefinition portal)
        {
            if (portal == null)
                throw new ArgumentNullException(nameof(portal));
            _portals.Add(portal);
        }

        public void AddLink(LinkDefinition link)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));
            _links.Add(link);
        }

        public void AddPath(PathDefinition path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            _paths.Add(path);
        }

        public void AddBeacon(TrackBeaconDefinition beacon)
        {
            if (beacon == null)
                throw new ArgumentNullException(nameof(beacon));
            _beacons.Add(beacon);
        }

        public void AddMarker(TrackMarkerDefinition marker)
        {
            if (marker == null)
                throw new ArgumentNullException(nameof(marker));
            _markers.Add(marker);
        }

        public void AddApproach(TrackApproachDefinition approach)
        {
            if (approach == null)
                throw new ArgumentNullException(nameof(approach));
            _approaches.Add(approach);
        }

        public TrackAreaManager BuildAreaManager()
        {
            return new TrackAreaManager(_shapes, _areas);
        }

        public bool TryGetBounds(out int minX, out int minZ, out int maxX, out int maxZ)
        {
            minX = 0;
            minZ = 0;
            maxX = 0;
            maxZ = 0;
            if (_cells.Count == 0)
                return false;

            minX = int.MaxValue;
            minZ = int.MaxValue;
            maxX = int.MinValue;
            maxZ = int.MinValue;

            foreach (var key in _cells.Keys)
            {
                if (key.X < minX) minX = key.X;
                if (key.Z < minZ) minZ = key.Z;
                if (key.X > maxX) maxX = key.X;
                if (key.Z > maxZ) maxZ = key.Z;
            }

            return true;
        }

        public TrackPortalManager BuildPortalManager()
        {
            return new TrackPortalManager(_portals, _links);
        }

        public bool TryGetStartAreaBounds(out float minX, out float minZ, out float maxX, out float maxZ)
        {
            minX = 0f;
            minZ = 0f;
            maxX = 0f;
            maxZ = 0f;
            if (string.IsNullOrWhiteSpace(StartAreaId))
                return false;
            return TryGetAreaBounds(StartAreaId!, out minX, out minZ, out maxX, out maxZ);
        }

        public bool TryGetFinishAreaBounds(out float minX, out float minZ, out float maxX, out float maxZ)
        {
            minX = 0f;
            minZ = 0f;
            maxX = 0f;
            maxZ = 0f;
            if (string.IsNullOrWhiteSpace(FinishAreaId))
                return false;
            return TryGetAreaBounds(FinishAreaId!, out minX, out minZ, out maxX, out maxZ);
        }

        public bool TryGetAreaBounds(string areaId, out float minX, out float minZ, out float maxX, out float maxZ)
        {
            minX = 0f;
            minZ = 0f;
            maxX = 0f;
            maxZ = 0f;

            if (string.IsNullOrWhiteSpace(areaId))
                return false;

            TrackAreaDefinition? area = null;
            foreach (var candidate in _areas)
            {
                if (candidate != null && string.Equals(candidate.Id, areaId, StringComparison.OrdinalIgnoreCase))
                {
                    area = candidate;
                    break;
                }
            }
            if (area == null || string.IsNullOrWhiteSpace(area.ShapeId))
                return false;

            ShapeDefinition? shape = null;
            foreach (var candidate in _shapes)
            {
                if (candidate != null && string.Equals(candidate.Id, area.ShapeId, StringComparison.OrdinalIgnoreCase))
                {
                    shape = candidate;
                    break;
                }
            }
            if (shape == null)
                return false;

            return TryGetShapeBounds(shape, out minX, out minZ, out maxX, out maxZ);
        }

        public bool TryGetStartAreaDefinition(out TrackAreaDefinition area)
        {
            return TryGetAreaDefinition(StartAreaId, out area);
        }

        public bool TryGetFinishAreaDefinition(out TrackAreaDefinition area)
        {
            return TryGetAreaDefinition(FinishAreaId, out area);
        }

        private bool TryGetAreaDefinition(string? areaId, out TrackAreaDefinition area)
        {
            area = null!;
            if (string.IsNullOrWhiteSpace(areaId))
                return false;

            foreach (var candidate in _areas)
            {
                if (candidate != null && string.Equals(candidate.Id, areaId, StringComparison.OrdinalIgnoreCase))
                {
                    area = candidate;
                    return true;
                }
            }
            return false;
        }

        public TrackPathManager BuildPathManager()
        {
            return new TrackPathManager(_paths, _shapes, BuildPortalManager(), DefaultWidthMeters);
        }

        public TrackSectorManager BuildSectorManager()
        {
            return new TrackSectorManager(_sectors, BuildAreaManager(), BuildPortalManager());
        }

        public TrackApproachManager BuildApproachManager()
        {
            return new TrackApproachManager(_sectors, _approaches, BuildPortalManager());
        }

        public TrackSectorRuleManager BuildSectorRuleManager()
        {
            return new TrackSectorRuleManager(_sectors, BuildPortalManager());
        }

        public TrackBranchManager BuildBranchManager()
        {
            return new TrackBranchManager(_sectors, _approaches, BuildPortalManager());
        }

        public void MergeCell(int x, int z, MapExits exits, TrackSurface? surface, TrackNoise? noise, float? widthMeters, bool? safeZone, string? zone)
        {
            var cell = GetOrCreateCell(x, z);
            cell.Exits |= exits;
            if (surface.HasValue)
                cell.Surface = surface.Value;
            if (noise.HasValue)
                cell.Noise = noise.Value;
            if (widthMeters.HasValue)
                cell.WidthMeters = Math.Max(0.5f, widthMeters.Value);
            if (safeZone.HasValue)
                cell.IsSafeZone = safeZone.Value;
            if (!string.IsNullOrWhiteSpace(zone))
                cell.Zone = zone!.Trim();
        }

        public bool TryStep(int x, int z, MapDirection direction, out int nextX, out int nextZ, out TrackMapCell nextCell)
        {
            nextCell = null!;
            nextX = x;
            nextZ = z;
            if (!TryGetCell(x, z, out var cell))
                return false;

            if (!AllowsExit(cell, direction))
                return false;

            (nextX, nextZ) = Offset(x, z, direction);
            if (!TryGetCell(nextX, nextZ, out nextCell))
                return false;

            if (!AllowsEntry(nextCell, direction))
                return false;

            return true;
        }

        public Vector3 CellToWorld(int x, int z)
        {
            return new Vector3(x * CellSizeMeters, 0f, z * CellSizeMeters);
        }

        public (int X, int Z) WorldToCell(Vector3 worldPosition)
        {
            var x = (int)Math.Round(worldPosition.X / CellSizeMeters, MidpointRounding.AwayFromZero);
            var z = (int)Math.Round(worldPosition.Z / CellSizeMeters, MidpointRounding.AwayFromZero);
            return (x, z);
        }

        public static MapDirection? DirectionFromDelta(Vector3 delta)
        {
            if (Math.Abs(delta.X) > Math.Abs(delta.Z))
                return delta.X >= 0f ? MapDirection.East : MapDirection.West;
            if (Math.Abs(delta.Z) > 0f)
                return delta.Z >= 0f ? MapDirection.North : MapDirection.South;
            return null;
        }

        public static MapExits ExitsFromDirection(MapDirection direction)
        {
            return direction switch
            {
                MapDirection.North => MapExits.North,
                MapDirection.East => MapExits.East,
                MapDirection.South => MapExits.South,
                MapDirection.West => MapExits.West,
                _ => MapExits.None
            };
        }

        public static MapDirection Opposite(MapDirection direction)
        {
            return direction switch
            {
                MapDirection.North => MapDirection.South,
                MapDirection.East => MapDirection.West,
                MapDirection.South => MapDirection.North,
                MapDirection.West => MapDirection.East,
                _ => MapDirection.North
            };
        }

        private static (int X, int Z) Offset(int x, int z, MapDirection direction)
        {
            return direction switch
            {
                MapDirection.North => (x, z + 1),
                MapDirection.East => (x + 1, z),
                MapDirection.South => (x, z - 1),
                MapDirection.West => (x - 1, z),
                _ => (x, z)
            };
        }

        private static bool AllowsExit(TrackMapCell cell, MapDirection direction)
        {
            return (cell.Exits & ExitsFromDirection(direction)) != 0;
        }

        private static bool AllowsEntry(TrackMapCell cell, MapDirection direction)
        {
            var opposite = Opposite(direction);
            return (cell.Exits & ExitsFromDirection(opposite)) != 0;
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

        private readonly struct CellKey : IEquatable<CellKey>
        {
            public CellKey(int x, int z)
            {
                X = x;
                Z = z;
            }

            public int X { get; }
            public int Z { get; }

            public bool Equals(CellKey other)
            {
                return X == other.X && Z == other.Z;
            }

            public override bool Equals(object? obj)
            {
                return obj is CellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (X * 397) ^ Z;
                }
            }
        }
    }
}
