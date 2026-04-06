using System;

namespace TopSpeed.Physics.Powertrain
{
    public static class PowertrainBuild
    {
        public static BuildResult Create(in BuildInput input)
        {
            var deceleration = Math.Max(0.01f, input.Deceleration);
            var coastDragBaseMps2 = input.CoastDragBaseMps2 >= 0f
                ? input.CoastDragBaseMps2
                : Math.Min(0.18f, Math.Max(0.08f, deceleration * 0.50f));
            var coastDragLinearPerMps = input.CoastDragLinearPerMps >= 0f
                ? input.CoastDragLinearPerMps
                : Math.Min(0.010f, Math.Max(0.003f, deceleration * 0.015f));
            var frictionLinearNmPerKrpm = input.FrictionLinearNmPerKrpm >= 0f ? input.FrictionLinearNmPerKrpm : 0f;
            var frictionQuadraticNmPerKrpm2 = input.FrictionQuadraticNmPerKrpm2 >= 0f ? input.FrictionQuadraticNmPerKrpm2 : 0f;
            var idleControlWindowRpm = input.IdleControlWindowRpm >= 0f ? input.IdleControlWindowRpm : 150f;
            var idleControlGainNmPerRpm = input.IdleControlGainNmPerRpm >= 0f ? input.IdleControlGainNmPerRpm : 0.08f;
            var minCoupledRiseIdleRpmPerSecond = input.MinCoupledRiseIdleRpmPerSecond >= 0f ? input.MinCoupledRiseIdleRpmPerSecond : 2200f;
            var minCoupledRiseFullRpmPerSecond = input.MinCoupledRiseFullRpmPerSecond >= 0f ? input.MinCoupledRiseFullRpmPerSecond : 6200f;
            if (minCoupledRiseFullRpmPerSecond < minCoupledRiseIdleRpmPerSecond)
                minCoupledRiseFullRpmPerSecond = minCoupledRiseIdleRpmPerSecond;
            var engineOverrunIdleLossFraction = input.EngineOverrunIdleLossFraction >= 0f ? input.EngineOverrunIdleLossFraction : 0.35f;
            var overrunCurveExponent = input.OverrunCurveExponent >= 0f ? input.OverrunCurveExponent : 1f;
            var engineBrakeTransferEfficiency = input.EngineBrakeTransferEfficiency >= 0f ? input.EngineBrakeTransferEfficiency : 0.68f;
            var reverseMaxSpeedKph = Math.Max(5f, input.ReverseMaxSpeedKph);
            var gears = Math.Max(1, input.Gears);
            var gearRatios = BuildRatios(gears, input.GearRatios);

            var powertrain = new Config(
                input.MassKg,
                input.DrivetrainEfficiency,
                input.EngineBrakingTorqueNm,
                input.TireGripCoefficient,
                input.BrakeStrength,
                input.WheelRadiusM,
                input.EngineBraking,
                input.IdleRpm,
                input.RevLimiter,
                input.FinalDriveRatio,
                input.PowerFactor,
                input.PeakTorqueNm,
                input.PeakTorqueRpm,
                input.IdleTorqueNm,
                input.RedlineTorqueNm,
                input.DragCoefficient,
                input.FrontalAreaM2,
                input.RollingResistanceCoefficient,
                input.LaunchRpm,
                input.ReversePowerFactor,
                input.ReverseGearRatio,
                input.EngineInertiaKgm2,
                input.EngineFrictionTorqueNm,
                input.DrivelineCouplingRate,
                gears,
                gearRatios,
                input.TorqueCurve,
                coastDragBaseMps2: coastDragBaseMps2,
                coastDragLinearPerMps: coastDragLinearPerMps,
                engineFrictionLinearNmPerKrpm: frictionLinearNmPerKrpm,
                engineFrictionQuadraticNmPerKrpm2: frictionQuadraticNmPerKrpm2,
                idleControlWindowRpm: idleControlWindowRpm,
                idleControlGainNmPerRpm: idleControlGainNmPerRpm,
                minCoupledRiseIdleRpmPerSecond: minCoupledRiseIdleRpmPerSecond,
                minCoupledRiseFullRpmPerSecond: minCoupledRiseFullRpmPerSecond,
                engineOverrunIdleLossFraction: engineOverrunIdleLossFraction,
                overrunCurveExponent: overrunCurveExponent,
                engineBrakeTransferEfficiency: engineBrakeTransferEfficiency);

            return new BuildResult(
                powertrain,
                reverseMaxSpeedKph,
                powertrain.GetGearRatios(),
                powertrain.CoastDragBaseMps2,
                powertrain.CoastDragLinearPerMps,
                powertrain.EngineFrictionLinearNmPerKrpm,
                powertrain.EngineFrictionQuadraticNmPerKrpm2,
                powertrain.IdleControlWindowRpm,
                powertrain.IdleControlGainNmPerRpm,
                powertrain.MinCoupledRiseIdleRpmPerSecond,
                powertrain.MinCoupledRiseFullRpmPerSecond,
                powertrain.EngineOverrunIdleLossFraction,
                powertrain.OverrunCurveExponent,
                powertrain.EngineBrakeTransferEfficiency);
        }

        private static float[] BuildRatios(int gears, float[]? provided)
        {
            if (provided != null && provided.Length == gears)
                return provided;

            var ratios = new float[gears];
            const float first = 3.5f;
            const float last = 0.85f;
            var logFirst = Math.Log(first);
            var logLast = Math.Log(last);
            for (var i = 0; i < gears; i++)
            {
                var t = gears > 1 ? i / (float)(gears - 1) : 0f;
                ratios[i] = (float)Math.Exp(logFirst + ((logLast - logFirst) * t));
            }

            return ratios;
        }
    }
}
