using System;

namespace TopSpeed.Physics.Tires
{
    internal readonly struct TireSteerData
    {
        public TireSteerData(float speedMps, float speedKph, float speedNorm, float forwardSpeed, float steerRad, float highSpeedSteerT)
        {
            SpeedMps = speedMps;
            SpeedKph = speedKph;
            SpeedNorm = speedNorm;
            ForwardSpeed = forwardSpeed;
            SteerRad = steerRad;
            HighSpeedSteerT = highSpeedSteerT;
        }

        public float SpeedMps { get; }
        public float SpeedKph { get; }
        public float SpeedNorm { get; }
        public float ForwardSpeed { get; }
        public float SteerRad { get; }
        public float HighSpeedSteerT { get; }
    }

    internal static class TireSteer
    {
        public static TireSteerData Resolve(in TireModelParameters parameters, in TireModelInput input)
        {
            var speedMps = Math.Max(0f, input.SpeedMps);
            var speedKph = speedMps * 3.6f;
            var forwardSpeed = Math.Max(1f, speedMps);

            var steering = TireModelMath.Clamp(input.SteeringInput / 100.0f, -1f, 1f);
            var steeringMag = Math.Abs(steering);
            var steeringCurve = Math.Max(0.2f, parameters.SteeringCurve);
            var steeringShaped = TireModelMath.Sign(steering) * TireModelMath.Pow(steeringMag, steeringCurve);

            var steerWindow = Math.Max(1f, parameters.HighSpeedSteerFullKph - parameters.HighSpeedSteerStartKph);
            var highSpeedSteerT = TireModelMath.Clamp01((speedKph - parameters.HighSpeedSteerStartKph) / steerWindow);
            var configuredHighSpeedGain = TireModelMath.Clamp(parameters.HighSpeedSteerGain, 0.7f, 1.6f);
            var highSpeedStability = TireModelMath.Clamp(parameters.HighSpeedStability, 0f, 1f);
            var baselineTarget = TireModelMath.Lerp(0.68f, 0.50f, highSpeedStability);
            var baselineHighSpeedScale = TireModelMath.Lerp(1f, baselineTarget, highSpeedSteerT);
            var gainBoost = 1f + ((configuredHighSpeedGain - 1f) * 0.25f * highSpeedSteerT);
            var highSpeedSteerScale = TireModelMath.Clamp(baselineHighSpeedScale * gainBoost, 0.40f, 1.05f);
            var steeringCommand = TireModelMath.Clamp(steeringShaped * parameters.SteeringResponse * highSpeedSteerScale, -1f, 1f);
            var steerRad = TireModelMath.DegToRad(parameters.MaxSteerDeg * steeringCommand);
            var speedNorm = TireModelMath.Clamp01(speedKph / 240f);

            return new TireSteerData(speedMps, speedKph, speedNorm, forwardSpeed, steerRad, highSpeedSteerT);
        }
    }
}
