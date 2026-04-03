using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Torque;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests.Physics
{
    [Trait("Category", "SharedPhysics")]
    public sealed class AutomaticShiftRuntimeTests
    {
        [Fact]
        public void Step_CooldownActive_CountsDownWithoutGearChange()
        {
            var config = BuildConfiguration();
            var result = AutomaticShiftRuntime.Step(
                new AutomaticShiftRuntimeInput(
                    config,
                    TransmissionPolicy.Default,
                    TransmissionType.Atc,
                    currentGear: 3,
                    gears: 6,
                    speedMps: 22f,
                    throttle: 0.7f,
                    surfaceTractionModifier: 1f,
                    longitudinalGripFactor: 1f,
                    referenceTopSpeedMps: 60f,
                    elapsedSeconds: 0.1f,
                    cooldownSeconds: 0.35f));

            Assert.False(result.Changed);
            Assert.Equal(3, result.Gear);
            Assert.Equal(0.25f, result.CooldownSeconds, 3);
        }

        [Fact]
        public void Step_AboveRevLimiter_Upshifts()
        {
            var config = BuildConfiguration();
            var result = AutomaticShiftRuntime.Step(
                new AutomaticShiftRuntimeInput(
                    config,
                    TransmissionPolicy.Default,
                    TransmissionType.Atc,
                    currentGear: 1,
                    gears: 6,
                    speedMps: 20f,
                    throttle: 1f,
                    surfaceTractionModifier: 1f,
                    longitudinalGripFactor: 1f,
                    referenceTopSpeedMps: 90f,
                    elapsedSeconds: 0.05f,
                    cooldownSeconds: 0f));

            Assert.True(result.Changed);
            Assert.Equal(2, result.Gear);
            Assert.True(result.CooldownSeconds > 0f);
        }

        [Fact]
        public void Step_LowRpmInHighGear_Downshifts()
        {
            var config = BuildConfiguration();
            var result = AutomaticShiftRuntime.Step(
                new AutomaticShiftRuntimeInput(
                    config,
                    TransmissionPolicy.Default,
                    TransmissionType.Atc,
                    currentGear: 5,
                    gears: 6,
                    speedMps: 8.0f,
                    throttle: 0.1f,
                    surfaceTractionModifier: 1f,
                    longitudinalGripFactor: 1f,
                    referenceTopSpeedMps: 90f,
                    elapsedSeconds: 0.05f,
                    cooldownSeconds: 0f));

            Assert.True(result.Changed);
            Assert.Equal(4, result.Gear);
            Assert.True(result.CooldownSeconds > 0f);
        }

        [Fact]
        public void Step_ShiftOnDemandActive_HoldsCurrentGear()
        {
            var config = BuildConfiguration();
            var result = AutomaticShiftRuntime.Step(
                new AutomaticShiftRuntimeInput(
                    config,
                    TransmissionPolicy.Default,
                    TransmissionType.Atc,
                    currentGear: 3,
                    gears: 6,
                    speedMps: 40f,
                    throttle: 1f,
                    surfaceTractionModifier: 1f,
                    longitudinalGripFactor: 1f,
                    referenceTopSpeedMps: 90f,
                    elapsedSeconds: 0.1f,
                    cooldownSeconds: 0f,
                    shiftOnDemandActive: true));

            Assert.False(result.Changed);
            Assert.Equal(3, result.Gear);
        }

        [Fact]
        public void Step_CvtForcesFirstGear()
        {
            var config = BuildConfiguration();
            var result = AutomaticShiftRuntime.Step(
                new AutomaticShiftRuntimeInput(
                    config,
                    TransmissionPolicy.Default,
                    TransmissionType.Cvt,
                    currentGear: 4,
                    gears: 6,
                    speedMps: 15f,
                    throttle: 0.5f,
                    surfaceTractionModifier: 1f,
                    longitudinalGripFactor: 1f,
                    referenceTopSpeedMps: 70f,
                    elapsedSeconds: 0.1f,
                    cooldownSeconds: 0f));

            Assert.True(result.Changed);
            Assert.Equal(1, result.Gear);
            Assert.Equal(-1, result.ShiftDirection);
        }

        [Fact]
        public void Step_Upshift_ReportsShiftMetadata()
        {
            var config = BuildConfiguration();
            var result = AutomaticShiftRuntime.Step(
                new AutomaticShiftRuntimeInput(
                    config,
                    TransmissionPolicy.Default,
                    TransmissionType.Atc,
                    currentGear: 1,
                    gears: 6,
                    speedMps: 20f,
                    throttle: 1f,
                    surfaceTractionModifier: 1f,
                    longitudinalGripFactor: 1f,
                    referenceTopSpeedMps: 90f,
                    elapsedSeconds: 0.05f,
                    cooldownSeconds: 0f));

            Assert.True(result.Changed);
            Assert.Equal(2, result.Gear);
            Assert.Equal(1, result.ShiftDirection);
            Assert.True(result.InGearDelaySeconds >= 0.2f);
        }

        private static Config BuildConfiguration()
        {
            var torqueCurve = CurveFactory.FromLegacy(
                idleRpm: 700f,
                revLimiter: 6000f,
                peakTorqueRpm: 3200f,
                idleTorqueNm: 180f,
                peakTorqueNm: 380f,
                redlineTorqueNm: 240f);

            return new Config(
                massKg: 1500f,
                drivetrainEfficiency: 0.85f,
                engineBrakingTorqueNm: 260f,
                tireGripCoefficient: 1.0f,
                brakeStrength: 1.0f,
                wheelRadiusM: 0.34f,
                engineBraking: 0.3f,
                idleRpm: 700f,
                revLimiter: 6000f,
                finalDriveRatio: 3.35f,
                powerFactor: 0.75f,
                peakTorqueNm: 380f,
                peakTorqueRpm: 3200f,
                idleTorqueNm: 180f,
                redlineTorqueNm: 240f,
                dragCoefficient: 0.30f,
                frontalAreaM2: 2.2f,
                rollingResistanceCoefficient: 0.015f,
                launchRpm: 2000f,
                reversePowerFactor: 0.55f,
                reverseGearRatio: 3.2f,
                engineInertiaKgm2: 0.24f,
                engineFrictionTorqueNm: 20f,
                drivelineCouplingRate: 12f,
                gears: 6,
                gearRatios: new[] { 3.5f, 2.2f, 1.5f, 1.2f, 1.0f, 0.85f },
                torqueCurve: torqueCurve);
        }
    }
}

