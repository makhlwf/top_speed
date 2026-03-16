using System;
using TopSpeed.Physics.Surface;
using TopSpeed.Physics.Tires;

namespace TopSpeed.Bots
{
    public static partial class BotPhysics
    {
        public static void Step(BotPhysicsConfig config, ref BotPhysicsState state, in BotPhysicsInput input)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (input.ElapsedSeconds <= 0f)
                return;

            if (state.Gear < 1 || state.Gear > config.Gears)
                state.Gear = 1;

            var surface = SurfaceModel.Resolve(input.Surface, config.SurfaceTractionFactor, config.Deceleration);
            var surfaceTraction = surface.Traction;
            var surfaceDecel = surface.Deceleration;

            var thrust = 0f;
            if (input.Throttle == 0)
                thrust = input.Brake;
            else if (input.Brake == 0 || -input.Brake <= input.Throttle)
                thrust = input.Throttle;
            else
                thrust = input.Brake;

            var speedKph = Math.Max(0f, state.SpeedKph);
            var speedMpsCurrent = speedKph / 3.6f;
            var throttle = Math.Max(0f, Math.Min(100f, input.Throttle)) / 100f;
            var steeringInput = input.Steering;
            var surfaceTractionMod = surfaceTraction / config.SurfaceTractionFactor;
            var longitudinalGripFactor = 1.0f;
            var speedDiffKph = 0f;
            var tireState = new TireModelState(state.LateralVelocityMps, state.YawRateRad);

            if (thrust > 10f)
            {
                var tireOutput = SolveTireModel(config, input.ElapsedSeconds, speedMpsCurrent, steeringInput, surfaceTractionMod, 1f, tireState);
                longitudinalGripFactor = tireOutput.LongitudinalGripFactor;
                var accelMps2 = ComputeNetAccelForGear(config, state.Gear, speedMpsCurrent, throttle, surfaceTractionMod, longitudinalGripFactor);
                var newSpeedMps = speedMpsCurrent + (accelMps2 * input.ElapsedSeconds);
                if (newSpeedMps < 0f)
                    newSpeedMps = 0f;
                speedDiffKph = (newSpeedMps - speedMpsCurrent) * 3.6f;
            }
            else
            {
                var surfaceDecelMod = surfaceDecel / config.Deceleration;
                var brakeInput = Math.Max(0f, Math.Min(100f, -input.Brake)) / 100f;
                var brakeDecel = CalculateBrakeDecel(config, brakeInput, surfaceDecelMod);
                var engineBrakeDecel = CalculateEngineBrakingDecel(config, state.Gear, speedMpsCurrent, surfaceDecelMod);
                var totalDecel = thrust < -10f ? (brakeDecel + engineBrakeDecel) : engineBrakeDecel;
                speedDiffKph = -totalDecel * input.ElapsedSeconds;
            }

            speedKph += speedDiffKph;
            var safetySpeed = ResolveForwardSafetySpeedKph(config.TopSpeedKph);
            if (speedKph > safetySpeed)
                speedKph = safetySpeed;
            if (speedKph < 0f)
                speedKph = 0f;

            UpdateAutomaticGear(config, ref state, input.ElapsedSeconds, speedKph / 3.6f, throttle, surfaceTractionMod, longitudinalGripFactor);
            if (thrust < -50f && speedKph > 0f)
                steeringInput = steeringInput * 2 / 3;

            var speedMps = speedKph / 3.6f;
            state.PositionY += speedMps * input.ElapsedSeconds;
            state.SpeedKph = speedKph;

            var surfaceTractionModLat = surfaceTraction / config.SurfaceTractionFactor;
            var lateralOutput = SolveTireModel(config, input.ElapsedSeconds, speedMps, steeringInput, surfaceTractionModLat, surface.LateralSpeedMultiplier, tireState);
            state.PositionX += lateralOutput.LateralSpeedMps * input.ElapsedSeconds;
            state.LateralVelocityMps = lateralOutput.State.LateralVelocityMps;
            state.YawRateRad = lateralOutput.State.YawRateRad;
        }
    }
}
