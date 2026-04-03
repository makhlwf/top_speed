using System;

namespace TopSpeed.Physics.Powertrain
{
    public static partial class Calculator
    {
        public static float EngineTorque(Config config, float rpm)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            return Math.Max(0f, config.TorqueCurve.EvaluateTorque(Clamp(rpm, config.IdleRpm, config.RevLimiter)));
        }

        public static float Horsepower(float torqueNm, float rpm)
        {
            if (rpm <= 0f || torqueNm <= 0f)
                return 0f;
            return (torqueNm * rpm) / 7127f;
        }

        public static float EngineLossTorqueNm(Config config, float rpm, bool closedThrottle)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return EngineLossTorqueNm(
                rpm,
                config.IdleRpm,
                config.RevLimiter,
                config.EngineFrictionTorqueNm,
                config.EngineFrictionLinearNmPerKrpm,
                config.EngineFrictionQuadraticNmPerKrpm2,
                config.EngineBrakingTorqueNm,
                config.EngineBraking,
                config.EngineOverrunIdleLossFraction,
                config.OverrunCurveExponent,
                closedThrottle);
        }

        public static float EngineLossTorqueNm(
            float rpm,
            float idleRpm,
            float revLimiter,
            float frictionBaseNm,
            float frictionLinearNmPerKrpm,
            float frictionQuadraticNmPerKrpm2,
            float engineBrakingTorqueNm,
            float engineBraking,
            float overrunIdleFraction,
            float overrunCurveExponent,
            bool closedThrottle)
        {
            var safeIdle = Math.Max(1f, idleRpm);
            var safeLimiter = Math.Max(safeIdle + 1f, revLimiter);
            var clampedRpm = Clamp(rpm, safeIdle, safeLimiter);
            var krpm = clampedRpm / 1000f;
            var frictionTorque = Math.Max(
                0f,
                frictionBaseNm
                + (Math.Max(0f, frictionLinearNmPerKrpm) * krpm)
                + (Math.Max(0f, frictionQuadraticNmPerKrpm2) * krpm * krpm));
            if (!closedThrottle)
                return frictionTorque;

            var rpmRange = safeLimiter - safeIdle;
            var rpmFactor = Clamp((clampedRpm - safeIdle) / Math.Max(1f, rpmRange), 0f, 1f);
            var shapedRpmFactor = (float)Math.Pow(rpmFactor, Clamp(overrunCurveExponent, 0.2f, 5f));
            var idleLossFraction = Clamp(overrunIdleFraction, 0f, 1f);
            var overrunFactor = idleLossFraction + ((1f - idleLossFraction) * shapedRpmFactor);
            var overrunTorque = Math.Max(0f, engineBrakingTorqueNm) * Math.Max(0f, engineBraking) * overrunFactor;
            return frictionTorque + overrunTorque;
        }
    }
}
