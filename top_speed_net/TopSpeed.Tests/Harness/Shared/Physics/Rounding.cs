using System;

namespace TopSpeed.Tests
{
    internal static class Rounding
    {
        public static float F(float value, int digits = 3) =>
            (!float.IsNaN(value) && !float.IsInfinity(value)) ? (float)Math.Round(value, digits) : value;
    }
}
