using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Torque;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests.Physics
{
    [Trait("Category", "SharedPhysics")]
    public sealed class EngineStateRuntimeTests
    {
        [Fact]
        public void Resolve_AtcLaunch_ReturnsBlendedWithLaunchMinimum()
        {
            var config = BuildConfiguration();
            var result = EngineStateRuntime.Resolve(
                new EngineStateRuntimeInput(
                    config,
                    TransmissionType.Atc,
                    isNeutralGear: false,
                    engineStalled: false,
                    drivelineLocked: false,
                    drivelineDisengaged: false,
                    speedMps: 1f / 3.6f,
                    throttle: 1f,
                    couplingFactor: 0.35f,
                    switchingGear: 0,
                    engineRpm: 900f,
                    coupledDriveRpm: 900f));

            Assert.Equal(CouplingMode.Blended, result.CouplingMode);
            Assert.True(result.MinimumCoupledRpm > config.IdleRpm);
        }

        [Fact]
        public void Resolve_DctNearLockSlipWindow_ReturnsLocked()
        {
            var config = BuildConfiguration();
            var result = EngineStateRuntime.Resolve(
                new EngineStateRuntimeInput(
                    config,
                    TransmissionType.Dct,
                    isNeutralGear: false,
                    engineStalled: false,
                    drivelineLocked: false,
                    drivelineDisengaged: false,
                    speedMps: 15f / 3.6f,
                    throttle: 0.35f,
                    couplingFactor: 0.998f,
                    switchingGear: 0,
                    engineRpm: 2010f,
                    coupledDriveRpm: 2005f));

            Assert.Equal(CouplingMode.Locked, result.CouplingMode);
        }

        [Fact]
        public void Resolve_DctWhileSwitching_RemainsBlended()
        {
            var config = BuildConfiguration();
            var result = EngineStateRuntime.Resolve(
                new EngineStateRuntimeInput(
                    config,
                    TransmissionType.Dct,
                    isNeutralGear: false,
                    engineStalled: false,
                    drivelineLocked: false,
                    drivelineDisengaged: false,
                    speedMps: 15f / 3.6f,
                    throttle: 0.35f,
                    couplingFactor: 0.998f,
                    switchingGear: 1,
                    engineRpm: 2010f,
                    coupledDriveRpm: 2005f));

            Assert.Equal(CouplingMode.Blended, result.CouplingMode);
        }

        [Fact]
        public void EvaluateManualStall_HighLoadLowSpeed_StallsAfterDelay()
        {
            var timer = 0f;
            var shouldStall = false;
            for (var i = 0; i < 3; i++)
            {
                var result = EngineStateRuntime.EvaluateManualStall(
                    new ManualStallRuntimeInput(
                        transmissionType: TransmissionType.Manual,
                        switchingGear: 0,
                        neutralGear: false,
                        engineStalled: false,
                        elapsedSeconds: 0.1f,
                        engineRpm: 710f,
                        stallRpm: 700f,
                        speedKph: 5f,
                        throttle: 0.06f,
                        clutch: 0f,
                        drivelineCouplingFactor: 1f,
                        coupledDemandRpm: 640f,
                        highLoadGear: true,
                        reverseLoad: false,
                        stallTimerSeconds: timer));
                timer = result.StallTimerSeconds;
                shouldStall = result.ShouldStall;
            }

            Assert.True(shouldStall);
            Assert.Equal(0f, timer, 3);
        }

        [Fact]
        public void EvaluateManualStall_NonManualTransmission_ResetsTimer()
        {
            var result = EngineStateRuntime.EvaluateManualStall(
                new ManualStallRuntimeInput(
                    transmissionType: TransmissionType.Atc,
                    switchingGear: 0,
                    neutralGear: false,
                    engineStalled: false,
                    elapsedSeconds: 0.1f,
                    engineRpm: 710f,
                    stallRpm: 700f,
                    speedKph: 5f,
                    throttle: 0.06f,
                    clutch: 0f,
                    drivelineCouplingFactor: 1f,
                    coupledDemandRpm: 640f,
                    highLoadGear: true,
                    reverseLoad: false,
                    stallTimerSeconds: 0.2f));

            Assert.False(result.ShouldStall);
            Assert.Equal(0f, result.StallTimerSeconds, 3);
        }

        [Fact]
        public void EvaluateManualStall_ClutchFullyDisengaged_DoesNotStall()
        {
            var timer = 0f;
            var shouldStall = false;
            for (var i = 0; i < 4; i++)
            {
                var result = EngineStateRuntime.EvaluateManualStall(
                    new ManualStallRuntimeInput(
                        transmissionType: TransmissionType.Manual,
                        switchingGear: 0,
                        neutralGear: false,
                        engineStalled: false,
                        elapsedSeconds: 0.1f,
                        engineRpm: 710f,
                        stallRpm: 700f,
                        speedKph: 0f,
                        throttle: 0f,
                        clutch: 1f,
                        drivelineCouplingFactor: 0f,
                        coupledDemandRpm: 0f,
                        highLoadGear: true,
                        reverseLoad: false,
                        stallTimerSeconds: timer));
                timer = result.StallTimerSeconds;
                shouldStall = result.ShouldStall;
            }

            Assert.False(shouldStall);
            Assert.Equal(0f, timer, 3);
        }

        [Fact]
        public void Resolve_AutomaticNeutral_KeepsMinimumCoupledRpmZero()
        {
            var config = BuildConfiguration();
            var result = EngineStateRuntime.Resolve(
                new EngineStateRuntimeInput(
                    config,
                    TransmissionType.Atc,
                    isNeutralGear: true,
                    engineStalled: false,
                    drivelineLocked: false,
                    drivelineDisengaged: true,
                    speedMps: 0f,
                    throttle: 1f,
                    couplingFactor: 0f,
                    switchingGear: 0,
                    engineRpm: 900f,
                    coupledDriveRpm: 0f));

            Assert.Equal(CouplingMode.Disengaged, result.CouplingMode);
            Assert.Equal(0f, result.MinimumCoupledRpm, 3);
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

