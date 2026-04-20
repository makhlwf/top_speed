using System;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private static float Lerp(float from, float to, float blend)
        {
            return from + ((to - from) * blend);
        }

        private static float BlendFactor(float perSecond, float deltaSeconds)
        {
            if (perSecond <= 0f || deltaSeconds <= 0f)
                return 0f;

            var blend = 1f - (float)Math.Exp(-perSecond * deltaSeconds);
            if (blend < 0f)
                return 0f;
            return blend > 1f ? 1f : blend;
        }

        private static float MoveToward(float value, float target, float maxDelta)
        {
            if (value < target)
                return Math.Min(value + maxDelta, target);
            return Math.Max(value - maxDelta, target);
        }

        private static int ClampPercent(int value)
        {
            if (value < -100)
                return -100;
            if (value > 100)
                return 100;
            return value;
        }

        private static int ScalePercent(float magnitude, float fullScaleTravel)
        {
            if (magnitude <= 0f || fullScaleTravel <= 0f)
                return 0;

            var value = (int)Math.Round((magnitude / fullScaleTravel) * 100f);
            if (value <= 0)
                return 0;
            return value >= 100 ? 100 : value;
        }
    }
}
