namespace TS.Sdl.Input
{
    public struct GestureEvent
    {
        public GestureKind Kind;
        public byte FingerCount;
        public byte TapCount;
        public ulong Timestamp;
        public ulong TouchId;
        public ulong FingerId;
        public uint WindowId;
        public float X;
        public float Y;
        public float DeltaX;
        public float DeltaY;
        public float Distance;
        public float Velocity;
        public SwipeDirection Direction;
        public float Scale;
        public float ScaleDelta;
        public float ScaleVelocity;
        public float Rotation;
        public float RotationDelta;
        public float RotationVelocity;
    }
}
