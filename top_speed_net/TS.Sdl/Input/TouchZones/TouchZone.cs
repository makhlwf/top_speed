using System;

namespace TS.Sdl.Input
{
    public readonly struct TouchZone
    {
        public TouchZone(string id, TouchZoneRect rect, int priority = 0, TouchZoneBehavior behavior = TouchZoneBehavior.Lock)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Touch zone id is required.", nameof(id));

            Id = id.Trim();
            Rect = rect;
            Priority = priority;
            Behavior = behavior;
        }

        public string Id { get; }
        public TouchZoneRect Rect { get; }
        public int Priority { get; }
        public TouchZoneBehavior Behavior { get; }
    }
}

