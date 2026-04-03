using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests.Physics
{
    [Trait("Category", "SharedPhysics")]
    public sealed class AutomaticShiftPolicyTests
    {
        [Fact]
        public void Decide_InvalidCurrentGear_DoesNotChange()
        {
            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    currentGear: 0,
                    gears: 6,
                    speedMps: 20f,
                    referenceTopSpeedMps: 55f,
                    idleRpm: 700f,
                    revLimiter: 6000f,
                    currentRpm: 4500f,
                    currentAccel: 1.0f,
                    upAccel: 1.2f,
                    downAccel: 0.8f),
                TransmissionPolicy.Default);

            Assert.False(decision.Changed);
            Assert.Equal(0, decision.NewGear);
            Assert.Equal(0f, decision.CooldownSeconds);
        }

        [Fact]
        public void Decide_AtLimiter_UpshiftsWhenEligible()
        {
            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    currentGear: 2,
                    gears: 6,
                    speedMps: 20f,
                    referenceTopSpeedMps: 60f,
                    idleRpm: 700f,
                    revLimiter: 6000f,
                    currentRpm: 6000f,
                    currentAccel: 0.8f,
                    upAccel: 0.9f,
                    downAccel: 0.5f),
                TransmissionPolicy.Default);

            Assert.True(decision.Changed);
            Assert.Equal(3, decision.NewGear);
            Assert.True(decision.CooldownSeconds > 0f);
        }

        [Fact]
        public void Decide_AtLimiter_DoesNotUpshiftIntoBlockedOverdrive()
        {
            var policy = new TransmissionPolicy(
                intendedTopSpeedGear: 3,
                allowOverdriveAboveGameTopSpeed: false);

            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    currentGear: 3,
                    gears: 6,
                    speedMps: 50f,
                    referenceTopSpeedMps: 60f,
                    idleRpm: 700f,
                    revLimiter: 6000f,
                    currentRpm: 6000f,
                    currentAccel: 1.1f,
                    upAccel: 1.3f,
                    downAccel: 0.7f),
                policy);

            Assert.False(decision.Changed);
            Assert.Equal(3, decision.NewGear);
        }

        [Fact]
        public void Decide_NearTopSpeed_AllowsConfiguredOverdrive()
        {
            var policy = new TransmissionPolicy(
                intendedTopSpeedGear: 3,
                allowOverdriveAboveGameTopSpeed: true,
                topSpeedPursuitSpeedFraction: 0.97f,
                preferIntendedTopSpeedGearNearLimit: true);

            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    currentGear: 3,
                    gears: 6,
                    speedMps: 59f,
                    referenceTopSpeedMps: 60f,
                    idleRpm: 700f,
                    revLimiter: 6000f,
                    currentRpm: 5800f,
                    currentAccel: 1.0f,
                    upAccel: 1.4f,
                    downAccel: 0.8f),
                policy);

            Assert.True(decision.Changed);
            Assert.Equal(4, decision.NewGear);
        }

        [Fact]
        public void Decide_BelowTopSpeed_HoldsIntendedTopSpeedGear()
        {
            var policy = new TransmissionPolicy(
                intendedTopSpeedGear: 3,
                allowOverdriveAboveGameTopSpeed: true,
                topSpeedPursuitSpeedFraction: 0.97f,
                preferIntendedTopSpeedGearNearLimit: true);

            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    currentGear: 3,
                    gears: 6,
                    speedMps: 50f,
                    referenceTopSpeedMps: 60f,
                    idleRpm: 700f,
                    revLimiter: 6000f,
                    currentRpm: 5800f,
                    currentAccel: 1.0f,
                    upAccel: 1.4f,
                    downAccel: 0.8f),
                policy);

            Assert.False(decision.Changed);
            Assert.Equal(3, decision.NewGear);
        }

        [Fact]
        public void Decide_UpshiftCooldown_UsesPerGearOverride()
        {
            var policy = new TransmissionPolicy(
                baseAutoShiftCooldownSeconds: 0.10f,
                upshiftCooldownBySourceGear: new[] { 0f, 0.35f, 0f, 0f, 0f, 0f });

            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    currentGear: 2,
                    gears: 6,
                    speedMps: 20f,
                    referenceTopSpeedMps: 60f,
                    idleRpm: 700f,
                    revLimiter: 6000f,
                    currentRpm: 5700f,
                    currentAccel: 1.1f,
                    upAccel: 1.5f,
                    downAccel: 0.7f),
                policy);

            Assert.True(decision.Changed);
            Assert.Equal(3, decision.NewGear);
            Assert.Equal(0.35f, decision.CooldownSeconds, 3);
        }
    }
}

