using System;

namespace TopSpeed.Physics.Powertrain
{
    public static partial class Calculator
    {
        public static float DriveRpm(
            Config config,
            int gear,
            float speedMps,
            float throttle,
            bool inReverse,
            float? driveRatioOverride = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var wheelCircumference = config.WheelRadiusM * TwoPi;
            var ratio = inReverse
                ? config.ReverseGearRatio
                : (driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                    ? driveRatioOverride.Value
                    : config.GetGearRatio(gear));
            var speedBasedRpm = wheelCircumference > 0f
                ? (speedMps / wheelCircumference) * 60f * ratio * config.FinalDriveRatio
                : 0f;
            var launchTarget = config.IdleRpm + (Clamp(throttle, 0f, 1f) * (config.LaunchRpm - config.IdleRpm));
            var rpm = Math.Max(speedBasedRpm, launchTarget);
            return Clamp(rpm, config.IdleRpm, config.RevLimiter);
        }

        public static float AutomaticMinimumCoupledRpm(
            Config config,
            float speedMps,
            float throttle,
            float couplingFactor)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var clampedThrottle = Clamp(throttle, 0f, 1f);
            if (clampedThrottle <= 0.01f)
                return config.IdleRpm;

            var clampedCoupling = Clamp(couplingFactor, 0f, 1f);
            var speedKph = Math.Max(0f, speedMps * 3.6f);
            var launchTargetRpm = config.IdleRpm + (clampedThrottle * (config.LaunchRpm - config.IdleRpm));

            var launchHoldSpeedKph = 6f + (8f * clampedThrottle);
            var slipFactor = 1f - clampedCoupling;
            var launchAssistBuildEndSpeedKph = Math.Max(1.5f, launchHoldSpeedKph * 0.75f);
            var launchAssistFadeStartSpeedKph = launchHoldSpeedKph;
            var launchAssistFadeEndSpeedKph = launchHoldSpeedKph + 10f;
            var launchAssistBuild = SmoothStep(0.4f, launchAssistBuildEndSpeedKph, speedKph);
            var launchAssistFade = 1f - SmoothStep(launchAssistFadeStartSpeedKph, launchAssistFadeEndSpeedKph, speedKph);
            var assistedLaunchProgress = launchAssistBuild * launchAssistFade;
            var throttleLaunchAssistMax = 0.12f + (0.68f * (float)Math.Pow(clampedThrottle, 1.30f));
            var throttleLaunchAssist = throttleLaunchAssistMax * assistedLaunchProgress;

            var slipInfluence = 0.12f + (0.88f * assistedLaunchProgress);
            var slipAssist = slipFactor * slipInfluence;
            var effectiveSlip = Math.Max(slipAssist, throttleLaunchAssist);
            var assist = Clamp(effectiveSlip, 0f, 1f);
            var minimumRpm = config.IdleRpm + ((launchTargetRpm - config.IdleRpm) * assist);
            return Clamp(minimumRpm, config.IdleRpm, config.RevLimiter);
        }

        public static float RpmAtSpeed(Config config, float speedMps, int gear, float? driveRatioOverride = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var wheelCircumference = config.WheelRadiusM * TwoPi;
            if (wheelCircumference <= 0f)
                return 0f;
            var gearRatio = driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                ? driveRatioOverride.Value
                : config.GetGearRatio(gear);
            return (speedMps / wheelCircumference) * 60f * gearRatio * config.FinalDriveRatio;
        }
    }
}
