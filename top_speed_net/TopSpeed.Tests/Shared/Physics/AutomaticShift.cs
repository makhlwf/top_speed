using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests.Physics
{
    [Trait("Category", "SharedPhysics")]
    public sealed class AutomaticShiftTests
    {
        private static readonly TransmissionPolicy CamryLikePolicy = new TransmissionPolicy(
            upshiftRpmFraction: 0.84f,
            downshiftRpmFraction: 0.35f,
            upshiftHysteresis: 0.05f);

        [Fact]
        public void Decide_DoesNotDownshiftInHighRpmPostUpshiftBand()
        {
            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    currentGear: 4,
                    gears: 8,
                    speedMps: 28.6f,
                    referenceTopSpeedMps: 55f,
                    idleRpm: 700f,
                    revLimiter: 5000f,
                    currentRpm: 4700f,
                    currentAccel: 3.0f,
                    upAccel: 2.7f,
                    downAccel: 3.8f),
                CamryLikePolicy);

            Assert.False(decision.Changed);
            Assert.Equal(4, decision.NewGear);
        }

        [Fact]
        public void Decide_DownshiftsWhenRpmFallsBelowDownshiftThreshold()
        {
            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    currentGear: 5,
                    gears: 8,
                    speedMps: 18f,
                    referenceTopSpeedMps: 55f,
                    idleRpm: 700f,
                    revLimiter: 5000f,
                    currentRpm: 2000f,
                    currentAccel: 0.2f,
                    upAccel: 0.1f,
                    downAccel: 0.6f),
                CamryLikePolicy);

            Assert.True(decision.Changed);
            Assert.Equal(4, decision.NewGear);
        }

        [Fact]
        public void Decide_AllowsPerformanceDownshiftWhenRpmIsInReentryBand()
        {
            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    currentGear: 5,
                    gears: 8,
                    speedMps: 24f,
                    referenceTopSpeedMps: 55f,
                    idleRpm: 700f,
                    revLimiter: 5000f,
                    currentRpm: 3400f,
                    currentAccel: 1.8f,
                    upAccel: 1.2f,
                    downAccel: 2.2f),
                CamryLikePolicy);

            Assert.True(decision.Changed);
            Assert.Equal(4, decision.NewGear);
        }

        [Fact]
        public void Decide_UpshiftsWhenAboveUpshiftThresholdAndNextGearIsBetter()
        {
            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    currentGear: 2,
                    gears: 6,
                    speedMps: 18f,
                    referenceTopSpeedMps: 60f,
                    idleRpm: 700f,
                    revLimiter: 6000f,
                    currentRpm: 5600f,
                    currentAccel: 2.4f,
                    upAccel: 2.7f,
                    downAccel: 1.8f),
                TransmissionPolicy.Default);

            Assert.True(decision.Changed);
            Assert.Equal(3, decision.NewGear);
            Assert.True(decision.CooldownSeconds > 0f);
        }
    }
}



