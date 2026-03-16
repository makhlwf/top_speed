using System;
using TopSpeed.Vehicles;

namespace TopSpeed.Bots
{
    public readonly struct AutomaticShiftDecision
    {
        public AutomaticShiftDecision(bool changed, int newGear, float cooldownSeconds)
        {
            Changed = changed;
            NewGear = newGear;
            CooldownSeconds = cooldownSeconds;
        }

        public bool Changed { get; }
        public int NewGear { get; }
        public float CooldownSeconds { get; }
    }

    public readonly struct AutomaticShiftInput
    {
        public AutomaticShiftInput(
            int currentGear,
            int gears,
            float speedMps,
            float referenceTopSpeedMps,
            float idleRpm,
            float revLimiter,
            float currentRpm,
            float currentAccel,
            float upAccel,
            float downAccel)
        {
            CurrentGear = currentGear;
            Gears = gears;
            SpeedMps = speedMps;
            ReferenceTopSpeedMps = referenceTopSpeedMps;
            IdleRpm = idleRpm;
            RevLimiter = revLimiter;
            CurrentRpm = currentRpm;
            CurrentAccel = currentAccel;
            UpAccel = upAccel;
            DownAccel = downAccel;
        }

        public int CurrentGear { get; }
        public int Gears { get; }
        public float SpeedMps { get; }
        public float ReferenceTopSpeedMps { get; }
        public float IdleRpm { get; }
        public float RevLimiter { get; }
        public float CurrentRpm { get; }
        public float CurrentAccel { get; }
        public float UpAccel { get; }
        public float DownAccel { get; }
    }

    public static class AutomaticTransmissionLogic
    {
        public static AutomaticShiftDecision Decide(in AutomaticShiftInput input, TransmissionPolicy? policy)
        {
            var p = policy ?? TransmissionPolicy.Default;
            if (input.Gears <= 1 || input.CurrentGear < 1 || input.CurrentGear > input.Gears)
                return new AutomaticShiftDecision(false, input.CurrentGear, 0f);

            var intendedTopSpeedGear = p.ResolveIntendedTopSpeedGear(input.Gears);
            var topSpeedPursuitThreshold = input.ReferenceTopSpeedMps > 0f
                ? input.ReferenceTopSpeedMps * p.TopSpeedPursuitSpeedFraction
                : float.MaxValue;
            var nearTopSpeed = input.SpeedMps >= topSpeedPursuitThreshold;
            var upshiftRpm = p.ResolveUpshiftRpm(input.IdleRpm, input.RevLimiter);

            var bestGear = input.CurrentGear;
            var bestAccel = input.CurrentAccel;

            if (input.CurrentGear < input.Gears)
            {
                if (input.CurrentRpm >= upshiftRpm &&
                    CanConsiderUpshift(in input, p, intendedTopSpeedGear, nearTopSpeed) &&
                    input.UpAccel > bestAccel)
                {
                    bestAccel = input.UpAccel;
                    bestGear = input.CurrentGear + 1;
                }
            }

            if (input.CurrentGear > 1 && input.DownAccel > bestAccel)
            {
                bestAccel = input.DownAccel;
                bestGear = input.CurrentGear - 1;
            }

            if (input.CurrentGear < input.Gears && input.CurrentRpm >= input.RevLimiter * 0.995f)
            {
                if (CanForceUpshiftAtLimiter(in input, p, intendedTopSpeedGear, nearTopSpeed))
                    return UpshiftDecision(input.CurrentGear, input.Gears, p);
            }

            var downshiftRpm = p.ResolveDownshiftRpm(input.IdleRpm, input.RevLimiter);
            if (input.CurrentGear > 1 && input.CurrentRpm < downshiftRpm)
            {
                return new AutomaticShiftDecision(true, input.CurrentGear - 1, p.BaseAutoShiftCooldownSeconds);
            }

            if (bestGear != input.CurrentGear && bestAccel > input.CurrentAccel * (1f + p.UpshiftHysteresis))
            {
                if (bestGear > input.CurrentGear)
                    return UpshiftDecision(input.CurrentGear, input.Gears, p);

                return new AutomaticShiftDecision(true, bestGear, p.BaseAutoShiftCooldownSeconds);
            }

            return new AutomaticShiftDecision(false, input.CurrentGear, 0f);
        }

        private static bool CanConsiderUpshift(
            in AutomaticShiftInput input,
            TransmissionPolicy policy,
            int intendedTopSpeedGear,
            bool nearTopSpeed)
        {
            var nextGear = input.CurrentGear + 1;
            if (nextGear > input.Gears)
                return false;

            if (!policy.AllowOverdriveAboveGameTopSpeed &&
                nextGear > intendedTopSpeedGear &&
                input.SpeedMps < input.ReferenceTopSpeedMps * 0.999f)
            {
                return false;
            }

            if (policy.AllowOverdriveAboveGameTopSpeed &&
                nextGear > intendedTopSpeedGear &&
                policy.PreferIntendedTopSpeedGearNearLimit &&
                !nearTopSpeed)
            {
                return false;
            }

            // Avoid automatic upshifts that immediately put the vehicle into a decelerating gear.
            if (input.UpAccel < policy.MinUpshiftNetAccelerationMps2 && !nearTopSpeed)
                return false;

            return true;
        }

        private static bool CanForceUpshiftAtLimiter(
            in AutomaticShiftInput input,
            TransmissionPolicy policy,
            int intendedTopSpeedGear,
            bool nearTopSpeed)
        {
            var nextGear = input.CurrentGear + 1;
            if (nextGear > input.Gears)
                return false;

            if (!policy.AllowOverdriveAboveGameTopSpeed && nextGear > intendedTopSpeedGear)
                return false;

            if (policy.AllowOverdriveAboveGameTopSpeed &&
                nextGear > intendedTopSpeedGear &&
                policy.PreferIntendedTopSpeedGearNearLimit &&
                !nearTopSpeed)
            {
                return false;
            }

            // If next gear would immediately decelerate strongly, don't force the upshift unless we're already near top speed.
            if (input.UpAccel < policy.MinUpshiftNetAccelerationMps2 && !nearTopSpeed)
                return false;

            return true;
        }

        private static AutomaticShiftDecision UpshiftDecision(int currentGear, int gears, TransmissionPolicy policy)
        {
            return new AutomaticShiftDecision(
                true,
                currentGear + 1,
                policy.GetUpshiftCooldownSeconds(currentGear, gears));
        }
    }
}
