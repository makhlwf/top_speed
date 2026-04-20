namespace TopSpeed.Input
{
    internal readonly struct TouchZoneState
    {
        public TouchZoneState(
            bool isActive,
            int fingerCount,
            float startX,
            float startY,
            float x,
            float y,
            float pressure,
            ulong startTimestamp,
            ulong timestamp)
        {
            IsActive = isActive;
            FingerCount = fingerCount;
            StartX = startX;
            StartY = startY;
            X = x;
            Y = y;
            Pressure = pressure;
            StartTimestamp = startTimestamp;
            Timestamp = timestamp;
        }

        public bool IsActive { get; }
        public int FingerCount { get; }
        public float StartX { get; }
        public float StartY { get; }
        public float X { get; }
        public float Y { get; }
        public float Pressure { get; }
        public ulong StartTimestamp { get; }
        public ulong Timestamp { get; }

        public static TouchZoneState Inactive => default;
    }
}
