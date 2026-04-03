namespace TopSpeed.Physics.Powertrain
{
    public static partial class Calculator
    {
        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static float SmoothStep(float edge0, float edge1, float value)
        {
            if (edge1 <= edge0)
                return value >= edge1 ? 1f : 0f;

            var t = Clamp((value - edge0) / (edge1 - edge0), 0f, 1f);
            return t * t * (3f - (2f * t));
        }
    }
}
