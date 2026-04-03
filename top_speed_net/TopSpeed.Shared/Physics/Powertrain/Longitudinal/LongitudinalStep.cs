using System;

namespace TopSpeed.Physics.Powertrain
{
    public static class LongitudinalStep
    {
        private const float TwoPi = (float)(Math.PI * 2.0);

        public static int ResolveThrust(int throttleInput, int brakeInput)
        {
            if (throttleInput == 0)
                return brakeInput;
            if (brakeInput == 0)
                return throttleInput;
            return -brakeInput > throttleInput ? brakeInput : throttleInput;
        }

        public static LongitudinalStepResult Compute(in LongitudinalStepInput input)
        {
            if (input.ElapsedSeconds <= 0f)
                return new LongitudinalStepResult(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

            if (input.RequestDrive)
                return ComputeDrive(in input);

            return ComputeCoast(in input);
        }

        private static LongitudinalStepResult ComputeDrive(in LongitudinalStepInput input)
        {
            var throttle = Clamp(input.Throttle, 0f, 1f);
            var coupling = Clamp(input.DrivelineCouplingFactor, 0f, 1f);
            var driveScale = Math.Max(0f, input.DriveAccelerationScale);
            var accelMps2 = input.InReverse
                ? Calculator.ReverseAccel(
                    input.Config,
                    input.SpeedMps,
                    throttle,
                    input.SurfaceTractionModifier,
                    input.LongitudinalGripFactor)
                : Calculator.DriveAccel(
                    input.Config,
                    input.Gear,
                    input.SpeedMps,
                    throttle,
                    input.SurfaceTractionModifier,
                    input.LongitudinalGripFactor,
                    input.DriveRatioOverride);

            accelMps2 *= coupling;
            accelMps2 *= driveScale;

            var newSpeedMps = Math.Max(0f, input.SpeedMps + (accelMps2 * input.ElapsedSeconds));
            var speedDeltaKph = (newSpeedMps - input.SpeedMps) * 3.6f;
            var coupledDriveRpm = CoupledRpm(input.Config, input.Gear, newSpeedMps, input.InReverse, input.DriveRatioOverride);
            return new LongitudinalStepResult(
                speedDeltaKph,
                coupledDriveRpm,
                accelMps2,
                totalDecelKph: 0f,
                brakeDecelKph: 0f,
                engineBrakeDecelKph: 0f,
                passiveResistanceDecelKph: 0f,
                chassisCoastDecelKph: 0f);
        }

        private static LongitudinalStepResult ComputeCoast(in LongitudinalStepInput input)
        {
            var brakeInput = Clamp(input.Brake, 0f, 1f);
            var brakeDecel = input.RequestBrake
                ? Calculator.BrakeDecelKph(input.Config, brakeInput, input.SurfaceDecelerationModifier)
                : 0f;
            var engineBrakeDecel = input.ApplyEngineBraking
                ? Calculator.EngineBrakeDecelKph(
                    input.Config,
                    input.Gear,
                    input.InReverse,
                    input.SpeedMps,
                    input.SurfaceDecelerationModifier,
                    input.CurrentEngineRpm,
                    input.DriveRatioOverride)
                : 0f;
            var passiveResistanceDecel = Calculator.PassiveResistanceDecelKph(input.Config, input.SpeedMps, input.SurfaceDecelerationModifier);
            var chassisCoastDecel = Calculator.ChassisCoastDecelKph(input.Config, input.SpeedMps, input.SurfaceDecelerationModifier);
            var totalDecelKph = passiveResistanceDecel + chassisCoastDecel + engineBrakeDecel + brakeDecel;
            var creepDeltaKph = Math.Max(0f, input.CreepAccelerationMps2) * input.ElapsedSeconds * 3.6f;
            var speedDeltaKph = (-totalDecelKph * input.ElapsedSeconds) + creepDeltaKph;
            return new LongitudinalStepResult(
                speedDeltaKph,
                coupledDriveRpm: 0f,
                driveAccelerationMps2: 0f,
                totalDecelKph,
                brakeDecelKph: brakeDecel,
                engineBrakeDecelKph: engineBrakeDecel,
                passiveResistanceDecelKph: passiveResistanceDecel,
                chassisCoastDecelKph: chassisCoastDecel);
        }

        private static float CoupledRpm(
            Config config,
            int gear,
            float speedMps,
            bool inReverse,
            float? driveRatioOverride)
        {
            var wheelCircumference = config.WheelRadiusM * TwoPi;
            if (wheelCircumference <= 0f)
                return config.IdleRpm;

            var ratio = inReverse
                ? config.ReverseGearRatio
                : (driveRatioOverride.HasValue && driveRatioOverride.Value > 0f
                    ? driveRatioOverride.Value
                    : config.GetGearRatio(gear));
            var coupledRpm = (speedMps / wheelCircumference) * 60f * ratio * config.FinalDriveRatio;
            return Clamp(coupledRpm, config.IdleRpm, config.RevLimiter);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
