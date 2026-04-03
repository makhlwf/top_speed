using System;
using TopSpeed.Vehicles;

namespace TopSpeed.Physics.Powertrain
{
    public static class EngineStateRuntime
    {
        private const float DctLockSlipFloorRpm = 120f;
        private const float DctLockSlipRangeFraction = 0.025f;
        private const float DctLockCouplingThreshold = 0.995f;

        private const float StallSpeedThresholdKph = 8f;
        private const float StallCouplingThreshold = 0.75f;
        private const float StallDelaySeconds = 0.25f;
        private const float StallDisengagedThrottleMax = 0.12f;
        private const float StallRpmCaptureBand = 35f;
        private const float StallDriveThrottleMax = 0.20f;
        private const float StallReverseThrottleMax = 0.15f;
        private const float StallClutchDownThreshold = 0.90f;

        public static EngineStateRuntimeResult Resolve(in EngineStateRuntimeInput input)
        {
            var couplingMode = ResolveCouplingMode(in input);
            var minimumCoupledRpm = 0f;
            if (!input.EngineStalled
                && TransmissionTypes.IsAutomaticFamily(input.TransmissionType)
                && !input.IsNeutralGear)
            {
                minimumCoupledRpm = Calculator.AutomaticMinimumCoupledRpm(
                    input.PowertrainConfig,
                    Math.Max(0f, input.SpeedMps),
                    Clamp01(input.Throttle),
                    Clamp01(input.CouplingFactor));
            }

            return new EngineStateRuntimeResult(couplingMode, minimumCoupledRpm);
        }

        public static ManualStallRuntimeResult EvaluateManualStall(in ManualStallRuntimeInput input)
        {
            var stallTimer = Math.Max(0f, input.StallTimerSeconds);
            if (input.EngineStalled)
                return new ManualStallRuntimeResult(false, stallTimer);

            if (input.TransmissionType != TransmissionType.Manual || input.SwitchingGear != 0 || input.NeutralGear)
                return new ManualStallRuntimeResult(false, 0f);

            var throttle = Clamp01(input.Throttle);
            var clutch = Clamp01(input.Clutch);
            var engineNearStall = input.EngineRpm <= input.StallRpm + StallRpmCaptureBand;
            var lowSpeed = input.SpeedKph <= StallSpeedThresholdKph;
            var engagedEnough = input.DrivelineCouplingFactor >= StallCouplingThreshold;
            if (clutch >= StallClutchDownThreshold)
                return new ManualStallRuntimeResult(false, 0f);

            var insufficientThrottle = throttle < StallDriveThrottleMax;
            var reverseLowThrottle = input.ReverseLoad && throttle < StallReverseThrottleMax;
            if (!lowSpeed || !engagedEnough || (!insufficientThrottle && !input.HighLoadGear && !reverseLowThrottle))
                return new ManualStallRuntimeResult(false, 0f);

            var demandNearStall = input.CoupledDemandRpm < input.StallRpm;
            if (!demandNearStall && !engineNearStall)
                return new ManualStallRuntimeResult(false, 0f);

            return AccumulateStallTimer(stallTimer, input.ElapsedSeconds);
        }

        private static CouplingMode ResolveCouplingMode(in EngineStateRuntimeInput input)
        {
            if (input.EngineStalled)
                return CouplingMode.Disengaged;

            var type = input.TransmissionType;
            if (TransmissionTypes.IsAutomaticFamily(type))
            {
                if (input.DrivelineDisengaged)
                    return CouplingMode.Disengaged;

                if (type == TransmissionType.Cvt || type == TransmissionType.Atc)
                    return CouplingMode.Blended;

                if (type == TransmissionType.Dct)
                {
                    if (input.SwitchingGear != 0)
                        return CouplingMode.Blended;

                    var rpmRange = Math.Max(1f, input.PowertrainConfig.RevLimiter - input.PowertrainConfig.IdleRpm);
                    var lockSlipWindowRpm = Math.Max(DctLockSlipFloorRpm, rpmRange * DctLockSlipRangeFraction);
                    var slipRpm = Math.Abs(input.CoupledDriveRpm - input.EngineRpm);
                    if (input.CouplingFactor >= DctLockCouplingThreshold && slipRpm <= lockSlipWindowRpm)
                        return CouplingMode.Locked;

                    return CouplingMode.Blended;
                }
            }

            if (input.DrivelineLocked)
                return CouplingMode.Locked;
            if (input.DrivelineDisengaged)
                return CouplingMode.Disengaged;
            return CouplingMode.Blended;
        }

        private static ManualStallRuntimeResult AccumulateStallTimer(float currentTimer, float elapsedSeconds)
        {
            var nextTimer = currentTimer + Math.Max(0f, elapsedSeconds);
            if (nextTimer >= StallDelaySeconds)
                return new ManualStallRuntimeResult(true, 0f);

            return new ManualStallRuntimeResult(false, nextTimer);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }
    }
}
