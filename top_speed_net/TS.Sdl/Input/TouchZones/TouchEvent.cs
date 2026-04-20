using TS.Sdl.Events;

namespace TS.Sdl.Input
{
    public readonly struct TouchZoneTouchEvent
    {
        public TouchZoneTouchEvent(EventType type, in TouchFingerEvent value, TouchZoneHit zone)
        {
            Type = type;
            Timestamp = value.Timestamp;
            TouchId = value.TouchId;
            FingerId = value.FingerId;
            WindowId = value.WindowId;
            X = value.X;
            Y = value.Y;
            DX = value.DX;
            DY = value.DY;
            Pressure = value.Pressure;
            Zone = zone;
        }

        public EventType Type { get; }
        public ulong Timestamp { get; }
        public ulong TouchId { get; }
        public ulong FingerId { get; }
        public uint WindowId { get; }
        public float X { get; }
        public float Y { get; }
        public float DX { get; }
        public float DY { get; }
        public float Pressure { get; }
        public TouchZoneHit Zone { get; }
    }
}

