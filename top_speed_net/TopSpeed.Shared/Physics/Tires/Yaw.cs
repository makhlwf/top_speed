using System;

namespace TopSpeed.Physics.Tires
{
    internal readonly struct TireYawData
    {
        public TireYawData(float rateTarget, float trackGain, float sourceGain, float speedSharpness)
        {
            RateTarget = rateTarget;
            TrackGain = trackGain;
            SourceGain = sourceGain;
            SpeedSharpness = speedSharpness;
        }

        public float RateTarget { get; }
        public float TrackGain { get; }
        public float SourceGain { get; }
        public float SpeedSharpness { get; }
    }

    internal static class TireYaw
    {
        public static TireYawData Resolve(in TireModelParameters parameters, in TireSteerData steer, in TireAxleData axle, float massKg)
        {
            var wheelbase = Math.Max(0.5f, axle.Wheelbase);
            var sharpSpeedT = TireModelMath.Clamp01((steer.SpeedKph - 90f) / 130f);
            var highSpeedStability = TireModelMath.Clamp(parameters.HighSpeedStability, 0f, 1f);

            var yawRateTarget = steer.ForwardSpeed / wheelbase * (float)Math.Tan(steer.SteerRad);
            var yawRateScaleAtHighSpeed = TireModelMath.Lerp(0.70f, 0.46f, highSpeedStability);
            var yawRateScale = TireModelMath.Lerp(1f, yawRateScaleAtHighSpeed, sharpSpeedT);
            yawRateTarget *= yawRateScale;

            var trackGainAtHighSpeed = TireModelMath.Lerp(1.30f, 0.90f, highSpeedStability);
            var trackGain = TireModelMath.Lerp(1.85f, trackGainAtHighSpeed, sharpSpeedT) * Math.Max(0.2f, parameters.TurnResponse);
            var steerNorm = TireModelMath.Clamp01(Math.Abs(steer.SteerRad) / TireModelMath.DegToRad(Math.Max(1f, parameters.MaxSteerDeg)));
            var sourceGainAtHighSpeed = TireModelMath.Lerp(0.12f, 0.04f, highSpeedStability);
            var sourceGainBase = TireModelMath.Lerp(0.24f, sourceGainAtHighSpeed, sharpSpeedT);
            var sourceGain = sourceGainBase * TireModelMath.Lerp(0.35f, 0.85f, steerNorm);

            return new TireYawData(yawRateTarget, trackGain, sourceGain, sharpSpeedT);
        }
    }
}
