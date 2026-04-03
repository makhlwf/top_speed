using System;
using TopSpeed.Physics.Torque;

namespace TopSpeed.Physics.Powertrain
{
    public readonly struct BuildInput
    {
        public BuildInput(
            float deceleration,
            float massKg,
            float drivetrainEfficiency,
            float engineBrakingTorqueNm,
            float tireGripCoefficient,
            float brakeStrength,
            float wheelRadiusM,
            float engineBraking,
            float idleRpm,
            float revLimiter,
            float finalDriveRatio,
            float powerFactor,
            float peakTorqueNm,
            float peakTorqueRpm,
            float idleTorqueNm,
            float redlineTorqueNm,
            float dragCoefficient,
            float frontalAreaM2,
            float rollingResistanceCoefficient,
            float launchRpm,
            float reversePowerFactor,
            float reverseGearRatio,
            float reverseMaxSpeedKph,
            float engineInertiaKgm2,
            float engineFrictionTorqueNm,
            float drivelineCouplingRate,
            int gears,
            CurveProfile torqueCurve,
            float[]? gearRatios = null,
            float coastDragBaseMps2 = -1f,
            float coastDragLinearPerMps = -1f,
            float frictionLinearNmPerKrpm = -1f,
            float frictionQuadraticNmPerKrpm2 = -1f,
            float idleControlWindowRpm = -1f,
            float idleControlGainNmPerRpm = -1f,
            float minCoupledRiseIdleRpmPerSecond = -1f,
            float minCoupledRiseFullRpmPerSecond = -1f,
            float engineOverrunIdleLossFraction = -1f,
            float overrunCurveExponent = -1f,
            float engineBrakeTransferEfficiency = -1f)
        {
            Deceleration = deceleration;
            MassKg = massKg;
            DrivetrainEfficiency = drivetrainEfficiency;
            EngineBrakingTorqueNm = engineBrakingTorqueNm;
            TireGripCoefficient = tireGripCoefficient;
            BrakeStrength = brakeStrength;
            WheelRadiusM = wheelRadiusM;
            EngineBraking = engineBraking;
            IdleRpm = idleRpm;
            RevLimiter = revLimiter;
            FinalDriveRatio = finalDriveRatio;
            PowerFactor = powerFactor;
            PeakTorqueNm = peakTorqueNm;
            PeakTorqueRpm = peakTorqueRpm;
            IdleTorqueNm = idleTorqueNm;
            RedlineTorqueNm = redlineTorqueNm;
            DragCoefficient = dragCoefficient;
            FrontalAreaM2 = frontalAreaM2;
            RollingResistanceCoefficient = rollingResistanceCoefficient;
            LaunchRpm = launchRpm;
            ReversePowerFactor = reversePowerFactor;
            ReverseGearRatio = reverseGearRatio;
            ReverseMaxSpeedKph = reverseMaxSpeedKph;
            EngineInertiaKgm2 = engineInertiaKgm2;
            EngineFrictionTorqueNm = engineFrictionTorqueNm;
            DrivelineCouplingRate = drivelineCouplingRate;
            Gears = gears;
            TorqueCurve = torqueCurve ?? throw new ArgumentNullException(nameof(torqueCurve));
            GearRatios = gearRatios;
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

        public float Deceleration { get; }
        public float MassKg { get; }
        public float DrivetrainEfficiency { get; }
        public float EngineBrakingTorqueNm { get; }
        public float TireGripCoefficient { get; }
        public float BrakeStrength { get; }
        public float WheelRadiusM { get; }
        public float EngineBraking { get; }
        public float IdleRpm { get; }
        public float RevLimiter { get; }
        public float FinalDriveRatio { get; }
        public float PowerFactor { get; }
        public float PeakTorqueNm { get; }
        public float PeakTorqueRpm { get; }
        public float IdleTorqueNm { get; }
        public float RedlineTorqueNm { get; }
        public float DragCoefficient { get; }
        public float FrontalAreaM2 { get; }
        public float RollingResistanceCoefficient { get; }
        public float LaunchRpm { get; }
        public float ReversePowerFactor { get; }
        public float ReverseGearRatio { get; }
        public float ReverseMaxSpeedKph { get; }
        public float EngineInertiaKgm2 { get; }
        public float EngineFrictionTorqueNm { get; }
        public float DrivelineCouplingRate { get; }
        public int Gears { get; }
        public CurveProfile TorqueCurve { get; }
        public float[]? GearRatios { get; }
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
