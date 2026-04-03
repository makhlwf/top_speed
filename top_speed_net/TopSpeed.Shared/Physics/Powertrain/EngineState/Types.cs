using System;
using TopSpeed.Vehicles;

namespace TopSpeed.Physics.Powertrain
{
    public enum CouplingMode
    {
        Disengaged = 0,
        Blended = 1,
        Locked = 2
    }

    public readonly struct EngineStateRuntimeInput
    {
        public EngineStateRuntimeInput(
            Config powertrainConfig,
            TransmissionType transmissionType,
            bool isNeutralGear,
            bool engineStalled,
            bool drivelineLocked,
            bool drivelineDisengaged,
            float speedMps,
            float throttle,
            float couplingFactor,
            int switchingGear,
            float engineRpm,
            float coupledDriveRpm)
        {
            PowertrainConfig = powertrainConfig ?? throw new ArgumentNullException(nameof(powertrainConfig));
            TransmissionType = transmissionType;
            IsNeutralGear = isNeutralGear;
            EngineStalled = engineStalled;
            DrivelineLocked = drivelineLocked;
            DrivelineDisengaged = drivelineDisengaged;
            SpeedMps = speedMps;
            Throttle = throttle;
            CouplingFactor = couplingFactor;
            SwitchingGear = switchingGear;
            EngineRpm = engineRpm;
            CoupledDriveRpm = coupledDriveRpm;
        }

        public Config PowertrainConfig { get; }
        public TransmissionType TransmissionType { get; }
        public bool IsNeutralGear { get; }
        public bool EngineStalled { get; }
        public bool DrivelineLocked { get; }
        public bool DrivelineDisengaged { get; }
        public float SpeedMps { get; }
        public float Throttle { get; }
        public float CouplingFactor { get; }
        public int SwitchingGear { get; }
        public float EngineRpm { get; }
        public float CoupledDriveRpm { get; }
    }

    public readonly struct EngineStateRuntimeResult
    {
        public EngineStateRuntimeResult(CouplingMode couplingMode, float minimumCoupledRpm)
        {
            CouplingMode = couplingMode;
            MinimumCoupledRpm = minimumCoupledRpm;
        }

        public CouplingMode CouplingMode { get; }
        public float MinimumCoupledRpm { get; }
    }

    public readonly struct ManualStallRuntimeInput
    {
        public ManualStallRuntimeInput(
            TransmissionType transmissionType,
            int switchingGear,
            bool neutralGear,
            bool engineStalled,
            float elapsedSeconds,
            float engineRpm,
            float stallRpm,
            float speedKph,
            float throttle,
            float clutch,
            float drivelineCouplingFactor,
            float coupledDemandRpm,
            bool highLoadGear,
            bool reverseLoad,
            float stallTimerSeconds)
        {
            TransmissionType = transmissionType;
            SwitchingGear = switchingGear;
            NeutralGear = neutralGear;
            EngineStalled = engineStalled;
            ElapsedSeconds = elapsedSeconds;
            EngineRpm = engineRpm;
            StallRpm = stallRpm;
            SpeedKph = speedKph;
            Throttle = throttle;
            Clutch = clutch;
            DrivelineCouplingFactor = drivelineCouplingFactor;
            CoupledDemandRpm = coupledDemandRpm;
            HighLoadGear = highLoadGear;
            ReverseLoad = reverseLoad;
            StallTimerSeconds = stallTimerSeconds;
        }

        public TransmissionType TransmissionType { get; }
        public int SwitchingGear { get; }
        public bool NeutralGear { get; }
        public bool EngineStalled { get; }
        public float ElapsedSeconds { get; }
        public float EngineRpm { get; }
        public float StallRpm { get; }
        public float SpeedKph { get; }
        public float Throttle { get; }
        public float Clutch { get; }
        public float DrivelineCouplingFactor { get; }
        public float CoupledDemandRpm { get; }
        public bool HighLoadGear { get; }
        public bool ReverseLoad { get; }
        public float StallTimerSeconds { get; }
    }

    public readonly struct ManualStallRuntimeResult
    {
        public ManualStallRuntimeResult(bool shouldStall, float stallTimerSeconds)
        {
            ShouldStall = shouldStall;
            StallTimerSeconds = stallTimerSeconds;
        }

        public bool ShouldStall { get; }
        public float StallTimerSeconds { get; }
    }
}
