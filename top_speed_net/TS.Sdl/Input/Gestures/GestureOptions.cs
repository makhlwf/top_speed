using System;

namespace TS.Sdl.Input
{
    public sealed class GestureOptions
    {
        public TimeSpan TapMaxTime { get; set; } = TimeSpan.FromMilliseconds(250);
        public TimeSpan DoubleTapGap { get; set; } = TimeSpan.FromMilliseconds(320);
        public TimeSpan LongPressTime { get; set; } = TimeSpan.FromMilliseconds(550);
        public TimeSpan TwoTapMaxTime { get; set; } = TimeSpan.FromMilliseconds(300);

        public float TapMove { get; set; } = 0.02f;
        public float DoubleTapMove { get; set; } = 0.04f;
        public float LongPressMove { get; set; } = 0.015f;
        public float TwoTapMove { get; set; } = 0.02f;

        public float SwipeMinDistance { get; set; } = 0.08f;
        public float SwipeMinVelocity { get; set; } = 0.4f;

        public float PinchStartDistance { get; set; } = 0.02f;
        public float RotateStartRadians { get; set; } = 0.12f;
    }
}
