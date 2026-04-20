using System;

namespace TS.Sdl.Input
{
    public readonly struct TouchZoneRect
    {
        public TouchZoneRect(float x, float y, float width, float height)
        {
            if (!IsFinite(x) || !IsFinite(y) || !IsFinite(width) || !IsFinite(height))
                throw new ArgumentOutOfRangeException(nameof(width), "Touch zone bounds must be finite values.");

            if (width <= 0f || height <= 0f)
                throw new ArgumentOutOfRangeException(nameof(width), "Touch zone width and height must be greater than zero.");

            if (x < 0f || y < 0f || x > 1f || y > 1f)
                throw new ArgumentOutOfRangeException(nameof(x), "Touch zone origin must be within normalized bounds.");

            var maxX = x + width;
            var maxY = y + height;
            if (maxX > 1f || maxY > 1f)
                throw new ArgumentOutOfRangeException(nameof(width), "Touch zone must fit inside normalized bounds.");

            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }

        public bool Contains(float x, float y)
        {
            return x >= X
                && x <= X + Width
                && y >= Y
                && y <= Y + Height;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

