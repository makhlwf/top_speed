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
            CurveProfile torqueCurve)
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
        public float DrivelineCouplingRate { get; }
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
