using System;
using TopSpeed.Vehicles;

namespace TopSpeed.Physics.Powertrain
{
    public static class AutomaticShiftRuntime
    {
        public static AutomaticShiftRuntimeResult Step(in AutomaticShiftRuntimeInput input)
        {
            if (input.Gears <= 1)
                return new AutomaticShiftRuntimeResult(false, input.CurrentGear, 0f);

            if (input.CurrentGear < 1 || input.CurrentGear > input.Gears)
                return new AutomaticShiftRuntimeResult(false, input.CurrentGear, 0f);

            if (input.ShiftOnDemandActive)
                return new AutomaticShiftRuntimeResult(false, input.CurrentGear, 0f);

            if (input.TransmissionType == TransmissionType.Cvt)
            {
                if (input.CurrentGear == 1)
                    return new AutomaticShiftRuntimeResult(false, 1, 0f);

                var direction = input.CurrentGear > 1 ? -1 : 1;
                return new AutomaticShiftRuntimeResult(true, 1, 0f, direction, 0f);
            }

            var cooldown = Math.Max(0f, input.CooldownSeconds);
            if (cooldown > 0f)
            {
                cooldown = Math.Max(0f, cooldown - Math.Max(0f, input.ElapsedSeconds));
                return new AutomaticShiftRuntimeResult(false, input.CurrentGear, cooldown);
            }

            var currentAccel = ComputeNetAccelForGear(
                input.PowertrainConfig,
                input.CurrentGear,
                input.Gears,
                input.SpeedMps,
                input.Throttle,
                input.SurfaceTractionModifier,
                input.LongitudinalGripFactor,
                input.DriveRatioOverride);
            var currentRpm = Calculator.RpmAtSpeed(
                input.PowertrainConfig,
                input.SpeedMps,
                input.CurrentGear,
                input.DriveRatioOverride);
            var upAccel = input.CurrentGear < input.Gears
                ? ComputeNetAccelForGear(
                    input.PowertrainConfig,
                    input.CurrentGear + 1,
                    input.Gears,
                    input.SpeedMps,
                    input.Throttle,
                    input.SurfaceTractionModifier,
                    input.LongitudinalGripFactor,
                    driveRatioOverride: null)
                : float.NegativeInfinity;
            var downAccel = input.CurrentGear > 1
                ? ComputeNetAccelForGear(
                    input.PowertrainConfig,
                    input.CurrentGear - 1,
                    input.Gears,
                    input.SpeedMps,
                    input.Throttle,
                    input.SurfaceTractionModifier,
                    input.LongitudinalGripFactor,
                    driveRatioOverride: null)
                : float.NegativeInfinity;

            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    input.CurrentGear,
                    input.Gears,
                    input.SpeedMps,
                    input.ReferenceTopSpeedMps,
                    input.PowertrainConfig.IdleRpm,
                    input.PowertrainConfig.RevLimiter,
                    currentRpm,
                    currentAccel,
                    upAccel,
                    downAccel),
                input.TransmissionPolicy);

            if (!decision.Changed)
                return new AutomaticShiftRuntimeResult(false, input.CurrentGear, 0f);

            var shiftDirection = decision.NewGear > input.CurrentGear ? 1 : -1;
            var inGearDelaySeconds = shiftDirection > 0
                ? Math.Max(0.2f, decision.CooldownSeconds)
                : 0.2f;
            return new AutomaticShiftRuntimeResult(
                true,
                decision.NewGear,
                Math.Max(0f, decision.CooldownSeconds),
                shiftDirection,
                inGearDelaySeconds);
        }

        private static float ComputeNetAccelForGear(
            Config config,
            int gear,
            int gears,
            float speedMps,
            float throttle,
            float surfaceTractionModifier,
            float longitudinalGripFactor,
            float? driveRatioOverride)
        {
            var rpm = Calculator.RpmAtSpeed(config, speedMps, gear, driveRatioOverride);
            if (rpm <= 0f)
                return float.NegativeInfinity;
            if (rpm > config.RevLimiter && gear < gears)
                return float.NegativeInfinity;

            return Calculator.DriveAccel(
                config,
                gear,
                speedMps,
                throttle,
                surfaceTractionModifier,
                longitudinalGripFactor,
                driveRatioOverride);
        }
    }
}
