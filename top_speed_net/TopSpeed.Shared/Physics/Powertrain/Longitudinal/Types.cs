using System;

namespace TopSpeed.Physics.Powertrain
{
    public readonly struct LongitudinalStepInput
    {
        public LongitudinalStepInput(
            Config config,
            float elapsedSeconds,
            float speedMps,
            float throttle,
            float brake,
            float surfaceTractionModifier,
            float surfaceDecelerationModifier,
            float longitudinalGripFactor,
            int gear,
            bool inReverse,
            float drivelineCouplingFactor,
            float creepAccelerationMps2,
            float currentEngineRpm,
            bool requestDrive,
            bool requestBrake,
            bool applyEngineBraking,
            float? driveRatioOverride = null,
            float driveAccelerationScale = 1f)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            ElapsedSeconds = elapsedSeconds;
            SpeedMps = speedMps;
            Throttle = throttle;
            Brake = brake;
            SurfaceTractionModifier = surfaceTractionModifier;
            SurfaceDecelerationModifier = surfaceDecelerationModifier;
            LongitudinalGripFactor = longitudinalGripFactor;
            Gear = gear;
            InReverse = inReverse;
            DrivelineCouplingFactor = drivelineCouplingFactor;
            CreepAccelerationMps2 = creepAccelerationMps2;
            CurrentEngineRpm = currentEngineRpm;
            RequestDrive = requestDrive;
            RequestBrake = requestBrake;
            ApplyEngineBraking = applyEngineBraking;
            DriveRatioOverride = driveRatioOverride;
            DriveAccelerationScale = driveAccelerationScale;
        }

        public Config Config { get; }
        public float ElapsedSeconds { get; }
        public float SpeedMps { get; }
        public float Throttle { get; }
        public float Brake { get; }
        public float SurfaceTractionModifier { get; }
        public float SurfaceDecelerationModifier { get; }
        public float LongitudinalGripFactor { get; }
        public int Gear { get; }
        public bool InReverse { get; }
        public float DrivelineCouplingFactor { get; }
        public float CreepAccelerationMps2 { get; }
        public float CurrentEngineRpm { get; }
        public bool RequestDrive { get; }
        public bool RequestBrake { get; }
        public bool ApplyEngineBraking { get; }
        public float? DriveRatioOverride { get; }
        public float DriveAccelerationScale { get; }
    }

    public readonly struct LongitudinalStepResult
    {
        public LongitudinalStepResult(
            float speedDeltaKph,
            float coupledDriveRpm,
            float driveAccelerationMps2,
            float totalDecelKph,
            float brakeDecelKph,
            float engineBrakeDecelKph,
            float passiveResistanceDecelKph,
            float chassisCoastDecelKph)
        {
            SpeedDeltaKph = speedDeltaKph;
            CoupledDriveRpm = coupledDriveRpm;
            DriveAccelerationMps2 = driveAccelerationMps2;
            TotalDecelKph = totalDecelKph;
            BrakeDecelKph = brakeDecelKph;
            EngineBrakeDecelKph = engineBrakeDecelKph;
            PassiveResistanceDecelKph = passiveResistanceDecelKph;
            ChassisCoastDecelKph = chassisCoastDecelKph;
        }

        public float SpeedDeltaKph { get; }
        public float CoupledDriveRpm { get; }
        public float DriveAccelerationMps2 { get; }
        public float TotalDecelKph { get; }
        public float BrakeDecelKph { get; }
        public float EngineBrakeDecelKph { get; }
        public float PassiveResistanceDecelKph { get; }
        public float ChassisCoastDecelKph { get; }
    }
}
