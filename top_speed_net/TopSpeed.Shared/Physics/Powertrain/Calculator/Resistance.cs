using System;

namespace TopSpeed.Physics.Powertrain
{
    public static partial class Calculator
    {
        public static float BrakeDecelKph(
            Config config,
            float brakeInput,
            float surfaceDecelerationModifier)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (brakeInput <= 0f)
                return 0f;

            var grip = Math.Max(0.1f, config.TireGripCoefficient * surfaceDecelerationModifier);
            var decelMps2 = Clamp(brakeInput, 0f, 1f) * config.BrakeStrength * grip * Gravity;
            return decelMps2 * 3.6f;
        }

        public static float PassiveResistanceDecelKph(
            Config config,
            float speedMps,
            float surfaceDecelerationModifier)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (speedMps <= 0f)
                return 0f;

            var resistiveForce = ResistiveForce(config, speedMps);
            var decelMps2 = resistiveForce / Math.Max(1f, config.MassKg);
            decelMps2 *= Math.Max(0f, surfaceDecelerationModifier);
            return Math.Max(0f, decelMps2 * 3.6f);
        }

        public static float ChassisCoastDecelKph(
            Config config,
            float speedMps,
            float surfaceDecelerationModifier)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (speedMps <= 0f)
                return 0f;

            var baseLossMps2 = config.CoastDragBaseMps2;
            var viscousLossMps2 = config.CoastDragLinearPerMps * speedMps;
            var coastLossMps2 = (baseLossMps2 + viscousLossMps2) * Math.Max(0f, surfaceDecelerationModifier);
            return Math.Max(0f, coastLossMps2 * 3.6f);
        }

        public static float EngineBrakeDecelKph(
            Config config,
            int gear,
            bool inReverse,
            float speedMps,
            float surfaceDecelerationModifier,
            float currentEngineRpm,
            float? driveRatioOverride = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (config.EngineBrakingTorqueNm <= 0f || config.MassKg <= 0f || config.WheelRadiusM <= 0f)
                return 0f;

            var rpmRange = config.RevLimiter - config.IdleRpm;
            if (rpmRange <= 0f)
                return 0f;

            var ratio = inReverse
                ? config.ReverseGearRatio
                : (driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                    ? driveRatioOverride.Value
                    : config.GetGearRatio(gear));
            var speedBasedRpm = RpmForRatio(config, speedMps, ratio);
            var effectiveRpm = Math.Max(config.IdleRpm, Math.Max(currentEngineRpm, speedBasedRpm));

            var engineLossTorque = EngineLossTorqueNm(config, effectiveRpm, closedThrottle: true);
            if (engineLossTorque <= 0f)
                return 0f;

            var wheelTorque = engineLossTorque * ratio * config.FinalDriveRatio * config.DrivetrainEfficiency * config.EngineBrakeTransferEfficiency;
            var wheelForce = wheelTorque / config.WheelRadiusM;
            var totalDriveRatio = ratio * config.FinalDriveRatio;
            var reflectedEngineInertia = config.EngineInertiaKgm2 * totalDriveRatio * totalDriveRatio;
            var equivalentMassFromEngineInertia = reflectedEngineInertia / Math.Max(0.0001f, config.WheelRadiusM * config.WheelRadiusM);
            var effectiveMass = config.MassKg + Math.Max(0f, equivalentMassFromEngineInertia);
            var decelMps2 = (wheelForce / effectiveMass) * surfaceDecelerationModifier;
            return Math.Max(0f, decelMps2 * 3.6f);
        }

        public static float ResistiveForce(Config config, float speedMps)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var dragForce = 0.5f * AirDensityKgPerM3 * config.DragCoefficient * config.FrontalAreaM2 * speedMps * speedMps;
            var rollingForce = config.RollingResistanceCoefficient * config.MassKg * Gravity;
            return dragForce + rollingForce;
        }

        private static float RpmForRatio(Config config, float speedMps, float ratio)
        {
            var wheelCircumference = config.WheelRadiusM * TwoPi;
            if (wheelCircumference <= 0f || ratio <= 0f)
                return 0f;
            return (speedMps / wheelCircumference) * 60f * ratio * config.FinalDriveRatio;
        }
    }
}
