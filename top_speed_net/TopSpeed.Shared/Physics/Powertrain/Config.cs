using System;
using TopSpeed.Physics.Torque;

namespace TopSpeed.Physics.Powertrain
{
    public sealed class Config
    {
        private readonly float[] _gearRatios;

        public Config(
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
            float engineInertiaKgm2,
            float engineFrictionTorqueNm,
            float drivelineCouplingRate,
            int gears,
            float[] gearRatios,
            CurveProfile torqueCurve,
            float coastDragBaseMps2 = 0f,
            float coastDragLinearPerMps = 0f,
            float engineFrictionLinearNmPerKrpm = 0f,
            float engineFrictionQuadraticNmPerKrpm2 = 0f,
            float idleControlWindowRpm = 150f,
            float idleControlGainNmPerRpm = 0.08f,
            float minCoupledRiseIdleRpmPerSecond = 2200f,
            float minCoupledRiseFullRpmPerSecond = 6200f,
            float engineOverrunIdleLossFraction = 0.35f,
            float overrunCurveExponent = 1f,
            float engineBrakeTransferEfficiency = 0.68f)
        {
            MassKg = Math.Max(1f, massKg);
            DrivetrainEfficiency = Clamp(drivetrainEfficiency, 0.1f, 1.0f);
            EngineBrakingTorqueNm = Math.Max(0f, engineBrakingTorqueNm);
            TireGripCoefficient = Math.Max(0.1f, tireGripCoefficient);
            BrakeStrength = Math.Max(0.1f, brakeStrength);
            WheelRadiusM = Math.Max(0.01f, wheelRadiusM);
            EngineBraking = Clamp(engineBraking, 0.05f, 1.5f);
            IdleRpm = Math.Max(500f, idleRpm);
            RevLimiter = Math.Max(IdleRpm + 1f, revLimiter);
            FinalDriveRatio = Math.Max(0.1f, finalDriveRatio);
            PowerFactor = Math.Max(0.05f, powerFactor);
            PeakTorqueNm = Math.Max(0f, peakTorqueNm);
            PeakTorqueRpm = Math.Max(IdleRpm + 100f, peakTorqueRpm);
            IdleTorqueNm = Math.Max(0f, idleTorqueNm);
            RedlineTorqueNm = Math.Max(0f, redlineTorqueNm);
            DragCoefficient = Math.Max(0.01f, dragCoefficient);
            FrontalAreaM2 = Math.Max(0.1f, frontalAreaM2);
            RollingResistanceCoefficient = Math.Max(0.001f, rollingResistanceCoefficient);
            LaunchRpm = Clamp(launchRpm, IdleRpm, RevLimiter);
            ReversePowerFactor = Math.Max(0.05f, reversePowerFactor);
            ReverseGearRatio = Math.Max(0.1f, reverseGearRatio);
            EngineInertiaKgm2 = Math.Max(0.01f, engineInertiaKgm2);
            EngineFrictionTorqueNm = Math.Max(0f, engineFrictionTorqueNm);
            DrivelineCouplingRate = Math.Max(0.1f, drivelineCouplingRate);
            CoastDragBaseMps2 = Math.Max(0f, coastDragBaseMps2);
            CoastDragLinearPerMps = Math.Max(0f, coastDragLinearPerMps);
            EngineFrictionLinearNmPerKrpm = Math.Max(0f, engineFrictionLinearNmPerKrpm);
            EngineFrictionQuadraticNmPerKrpm2 = Math.Max(0f, engineFrictionQuadraticNmPerKrpm2);
            IdleControlWindowRpm = Math.Max(0f, idleControlWindowRpm);
            IdleControlGainNmPerRpm = Math.Max(0f, idleControlGainNmPerRpm);
            MinCoupledRiseIdleRpmPerSecond = Math.Max(0f, minCoupledRiseIdleRpmPerSecond);
            MinCoupledRiseFullRpmPerSecond = Math.Max(MinCoupledRiseIdleRpmPerSecond, minCoupledRiseFullRpmPerSecond);
            EngineOverrunIdleLossFraction = Clamp(engineOverrunIdleLossFraction, 0f, 1f);
            OverrunCurveExponent = Clamp(overrunCurveExponent, 0.2f, 5f);
            EngineBrakeTransferEfficiency = Clamp(engineBrakeTransferEfficiency, 0.1f, 1f);
            Gears = Math.Max(1, gears);
            _gearRatios = (gearRatios != null && gearRatios.Length == Gears)
                ? gearRatios
                : BuildDefaultRatios(Gears);
            TorqueCurve = torqueCurve ?? throw new ArgumentNullException(nameof(torqueCurve));
        }

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
        public float EngineInertiaKgm2 { get; }
        public float EngineFrictionTorqueNm { get; }
        public float EngineFrictionLinearNmPerKrpm { get; }
        public float EngineFrictionQuadraticNmPerKrpm2 { get; }
        public float DrivelineCouplingRate { get; }
        public float CoastDragBaseMps2 { get; }
        public float CoastDragLinearPerMps { get; }
        public float IdleControlWindowRpm { get; }
        public float IdleControlGainNmPerRpm { get; }
        public float MinCoupledRiseIdleRpmPerSecond { get; }
        public float MinCoupledRiseFullRpmPerSecond { get; }
        public float EngineOverrunIdleLossFraction { get; }
        public float OverrunCurveExponent { get; }
        public float EngineBrakeTransferEfficiency { get; }
        public int Gears { get; }
        public CurveProfile TorqueCurve { get; }

        public float GetGearRatio(int gear)
        {
            var clamped = Math.Max(1, Math.Min(Gears, gear));
            return _gearRatios[clamped - 1];
        }

        public float[] GetGearRatios()
        {
            var copy = new float[_gearRatios.Length];
            Array.Copy(_gearRatios, copy, _gearRatios.Length);
            return copy;
        }

        private static float[] BuildDefaultRatios(int gears)
        {
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
