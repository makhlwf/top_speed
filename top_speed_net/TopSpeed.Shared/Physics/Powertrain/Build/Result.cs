using System;

namespace TopSpeed.Physics.Powertrain
{
    public readonly struct BuildResult
    {
        public BuildResult(
            Config powertrain,
            float reverseMaxSpeedKph,
            float[] gearRatios,
            float coastDragBaseMps2,
            float coastDragLinearPerMps,
            float frictionLinearNmPerKrpm,
            float frictionQuadraticNmPerKrpm2,
            float idleControlWindowRpm,
            float idleControlGainNmPerRpm,
            float minCoupledRiseIdleRpmPerSecond,
            float minCoupledRiseFullRpmPerSecond,
            float engineOverrunIdleLossFraction,
            float overrunCurveExponent,
            float engineBrakeTransferEfficiency)
        {
            Powertrain = powertrain ?? throw new ArgumentNullException(nameof(powertrain));
            ReverseMaxSpeedKph = reverseMaxSpeedKph;
            GearRatios = gearRatios ?? throw new ArgumentNullException(nameof(gearRatios));
            CoastDragBaseMps2 = coastDragBaseMps2;
            CoastDragLinearPerMps = coastDragLinearPerMps;
            FrictionLinearNmPerKrpm = frictionLinearNmPerKrpm;
            FrictionQuadraticNmPerKrpm2 = frictionQuadraticNmPerKrpm2;
            IdleControlWindowRpm = idleControlWindowRpm;
            IdleControlGainNmPerRpm = idleControlGainNmPerRpm;
            MinCoupledRiseIdleRpmPerSecond = minCoupledRiseIdleRpmPerSecond;
            MinCoupledRiseFullRpmPerSecond = minCoupledRiseFullRpmPerSecond;
            EngineOverrunIdleLossFraction = engineOverrunIdleLossFraction;
            OverrunCurveExponent = overrunCurveExponent;
            EngineBrakeTransferEfficiency = engineBrakeTransferEfficiency;
        }

        public Config Powertrain { get; }
        public float ReverseMaxSpeedKph { get; }
        public float[] GearRatios { get; }
        public float CoastDragBaseMps2 { get; }
        public float CoastDragLinearPerMps { get; }
        public float FrictionLinearNmPerKrpm { get; }
        public float FrictionQuadraticNmPerKrpm2 { get; }
        public float IdleControlWindowRpm { get; }
        public float IdleControlGainNmPerRpm { get; }
        public float MinCoupledRiseIdleRpmPerSecond { get; }
        public float MinCoupledRiseFullRpmPerSecond { get; }
        public float EngineOverrunIdleLossFraction { get; }
        public float OverrunCurveExponent { get; }
        public float EngineBrakeTransferEfficiency { get; }
    }
}
