using System;

namespace TopSpeed.Physics.Powertrain
{
    public static partial class Calculator
    {
        public static float DriveAccel(
            Config config,
            int gear,
            float speedMps,
            float throttle,
            float surfaceTractionModifier,
            float longitudinalGripFactor,
            float? driveRatioOverride = null)
        {
            return DriveAccelCore(
                config,
                gear,
                inReverse: false,
                speedMps,
                throttle,
                surfaceTractionModifier,
                longitudinalGripFactor,
                driveRatioOverride);
        }

        public static float ReverseAccel(
            Config config,
            float speedMps,
            float throttle,
            float surfaceTractionModifier,
            float longitudinalGripFactor)
        {
            return DriveAccelCore(
                config,
                1,
                inReverse: true,
                speedMps,
                throttle,
                surfaceTractionModifier,
                longitudinalGripFactor);
        }

        private static float DriveAccelCore(
            Config config,
            int gear,
            bool inReverse,
            float speedMps,
            float throttle,
            float surfaceTractionModifier,
            float longitudinalGripFactor,
            float? driveRatioOverride = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var clampedThrottle = Clamp(throttle, 0f, 1f);
            if (clampedThrottle <= 0f)
                return 0f;

            var driveRpm = DriveRpm(config, gear, speedMps, clampedThrottle, inReverse, driveRatioOverride);
            var engineTorque = EngineTorque(config, driveRpm) * clampedThrottle * config.PowerFactor;
            var ratio = inReverse
                ? config.ReverseGearRatio
                : (driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                    ? driveRatioOverride.Value
                    : config.GetGearRatio(gear));
            var wheelTorque = engineTorque * ratio * config.FinalDriveRatio * config.DrivetrainEfficiency;
            var wheelForce = wheelTorque / config.WheelRadiusM;
            var tractionLimit = config.TireGripCoefficient * surfaceTractionModifier * config.MassKg * Gravity;
            if (wheelForce > tractionLimit)
                wheelForce = tractionLimit;
            wheelForce *= Clamp(longitudinalGripFactor, 0f, 1f);
            if (inReverse)
                wheelForce *= config.ReversePowerFactor;

            var netForce = wheelForce - ResistiveForce(config, speedMps);
            return netForce / config.MassKg;
        }
    }
}
