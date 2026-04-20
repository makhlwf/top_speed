using System;
using System.Collections.Generic;

namespace TS.Sdl.Input
{
    public sealed class TouchZoneRegistry
    {
        private readonly List<Entry> _zones = new List<Entry>();
        private long _nextOrder = 1;

        public int Count => _zones.Count;

        public void Set(in TouchZone zone)
        {
            var existingIndex = FindIndex(zone.Id);
            if (existingIndex >= 0)
            {
                var entry = _zones[existingIndex];
                _zones[existingIndex] = new Entry(zone, entry.Order);
                return;
            }

            _zones.Add(new Entry(zone, _nextOrder++));
        }

        public bool Remove(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            var index = FindIndex(id.Trim());
            if (index < 0)
                return false;

            _zones.RemoveAt(index);
            return true;
        }

        public void Clear()
        {
            _zones.Clear();
            _nextOrder = 1;
        }

        public TouchZone[] Snapshot()
        {
            var values = new TouchZone[_zones.Count];
            for (var i = 0; i < _zones.Count; i++)
                values[i] = _zones[i].Zone;
            return values;
        }

        public bool TryGet(string id, out TouchZone zone)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                zone = default;
                return false;
            }

            var index = FindIndex(id.Trim());
            if (index < 0)
            {
                zone = default;
                return false;
            }

            zone = _zones[index].Zone;
            return true;
        }

        public bool TryResolve(float x, float y, out TouchZone zone)
        {
            var matched = false;
            var bestPriority = int.MinValue;
            var bestOrder = long.MaxValue;
            var bestZone = default(TouchZone);

            for (var i = 0; i < _zones.Count; i++)
            {
                var candidate = _zones[i];
                if (!candidate.Zone.Rect.Contains(x, y))
                    continue;

                if (!matched
                    || candidate.Zone.Priority > bestPriority
                    || (candidate.Zone.Priority == bestPriority && candidate.Order < bestOrder))
                {
                    matched = true;
                    bestPriority = candidate.Zone.Priority;
                    bestOrder = candidate.Order;
                    bestZone = candidate.Zone;
                }
            }

            zone = bestZone;
            return matched;
        }

        private int FindIndex(string id)
        {
            for (var i = 0; i < _zones.Count; i++)
            {
                if (string.Equals(_zones[i].Zone.Id, id, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private readonly struct Entry
        {
            public Entry(in TouchZone zone, long order)
            {
                Zone = zone;
                Order = order;
            }

            public TouchZone Zone { get; }
            public long Order { get; }
        }
    }
}

