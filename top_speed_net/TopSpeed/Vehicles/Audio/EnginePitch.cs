using System;

namespace TopSpeed.Vehicles
{
    internal static class EnginePitch
    {
        public static int FromRpm(
            float rpm,
            float stallRpm,
            float idleRpm,
            float revLimiter,
            int idleFreq,
            int topFreq,
            float pitchCurveExponent)
        {
            var safeIdleRpm = Math.Max(1f, idleRpm);
            var safeStallRpm = Math.Max(1f, Math.Min(safeIdleRpm, stallRpm));
            var safeRevLimiter = Math.Max(safeIdleRpm + 1f, revLimiter);
            var minFrequency = Math.Min(idleFreq, topFreq);
            var maxFrequency = Math.Max(idleFreq, topFreq);
            if (maxFrequency <= minFrequency)
                return minFrequency;

            if (rpm <= safeIdleRpm)
            {
                var subIdleFloor = (int)Math.Round(minFrequency * (safeStallRpm / safeIdleRpm));
                if (subIdleFloor < 1)
                    subIdleFloor = 1;
                var subIdleSpan = Math.Max(1f, safeIdleRpm - safeStallRpm);
                var subIdleNormalized = (rpm - safeStallRpm) / subIdleSpan;
                if (subIdleNormalized < 0f)
                    subIdleNormalized = 0f;
                else if (subIdleNormalized > 1f)
                    subIdleNormalized = 1f;

                var subIdleFrequency = subIdleFloor
                    + (int)Math.Round(subIdleNormalized * (minFrequency - subIdleFloor));
                if (subIdleFrequency < subIdleFloor)
                    return subIdleFloor;
                return subIdleFrequency > minFrequency ? minFrequency : subIdleFrequency;
            }

            var rpmNormalized = (rpm - safeIdleRpm) / (safeRevLimiter - safeIdleRpm);
            if (rpmNormalized < 0f)
                rpmNormalized = 0f;
            else if (rpmNormalized > 1f)
                rpmNormalized = 1f;

            var exponent = VehicleDefinition.ClampPitchCurveExponent(pitchCurveExponent);
            var curved = (float)Math.Pow(rpmNormalized, exponent);

            var frequency = minFrequency + (int)Math.Round(curved * (maxFrequency - minFrequency));
            if (frequency < minFrequency)
                return minFrequency;
            return frequency > maxFrequency ? maxFrequency : frequency;
        }
    }
}

