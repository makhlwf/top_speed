using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Torque;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests.Physics
{
    [Trait("Category", "SharedPhysics")]
    public sealed class PowertrainBehaviorTests
    {
        [Fact]
        public void DriveRpm_AtStandstill_TracksLaunchTarget()
        {
            var config = BuildConfiguration();
            var rpm = Calculator.DriveRpm(
                config,
                gear: 1,
                speedMps: 0f,
                throttle: 1f,
                inReverse: false);

            Assert.Equal(config.LaunchRpm, rpm, 3);
        }

        [Fact]
        public void DriveRpm_AtExtremeSpeed_ClampsToRevLimiter()
        {
            var config = BuildConfiguration();
            var rpm = Calculator.DriveRpm(
                config,
                gear: 1,
                speedMps: 400f,
                throttle: 1f,
                inReverse: false);

            Assert.Equal(config.RevLimiter, rpm, 3);
        }

        [Fact]
        public void DriveAccel_WithZeroLongitudinalGrip_IsNonPositive()
        {
            var config = BuildConfiguration();
            var accel = Calculator.DriveAccel(
                config,
                gear: 2,
                speedMps: 25f,
                throttle: 0.9f,
                surfaceTractionModifier: 1f,
                longitudinalGripFactor: 0f);

            Assert.True(accel <= 0f);
        }

        [Fact]
        public void ReverseAccel_IsLowerThanForwardAccel_ForSameInput()
        {
            var config = BuildConfiguration();
            var speedMps = 12f;
            var throttle = 0.8f;

            var forward = Calculator.DriveAccel(
                config,
                gear: 1,
                speedMps: speedMps,
                throttle: throttle,
                surfaceTractionModifier: 1f,
                longitudinalGripFactor: 1f);
            var reverse = Calculator.ReverseAccel(
                config,
                speedMps: speedMps,
                throttle: throttle,
                surfaceTractionModifier: 1f,
                longitudinalGripFactor: 1f);

            Assert.True(forward > reverse);
        }

        [Fact]
        public void LongitudinalStep_DrivePath_MatchesDriveAccelIntegration()
        {
            var config = BuildConfiguration();
            const float elapsed = 0.05f;
            const float speedMps = 12f;
            const float throttle = 0.75f;
            const float grip = 0.92f;
            const float traction = 1f;
            const float coupling = 0.84f;

            var expectedAccelMps2 = Calculator.DriveAccel(
                config,
                gear: 2,
                speedMps: speedMps,
                throttle: throttle,
                surfaceTractionModifier: traction,
                longitudinalGripFactor: grip) * coupling;
            var expectedSpeedDeltaKph = expectedAccelMps2 * elapsed * 3.6f;

            var result = LongitudinalStep.Compute(
                new LongitudinalStepInput(
                    config,
                    elapsedSeconds: elapsed,
                    speedMps: speedMps,
                    throttle: throttle,
                    brake: 0f,
                    surfaceTractionModifier: traction,
                    surfaceDecelerationModifier: 1f,
                    longitudinalGripFactor: grip,
                    gear: 2,
                    inReverse: false,
                    drivelineCouplingFactor: coupling,
                    creepAccelerationMps2: 0f,
                    currentEngineRpm: 2000f,
                    requestDrive: true,
                    requestBrake: false,
                    applyEngineBraking: false));

            Assert.Equal(expectedSpeedDeltaKph, result.SpeedDeltaKph, 4);
            Assert.Equal(expectedAccelMps2, result.DriveAccelerationMps2, 4);
            Assert.True(result.CoupledDriveRpm >= config.IdleRpm);
        }

        [Fact]
        public void LongitudinalStep_CoastPath_MatchesCombinedDecelModel()
        {
            var config = BuildConfiguration();
            const float elapsed = 0.05f;
            const float speedMps = 50f / 3.6f;
            var engineRpm = Calculator.RpmAtSpeed(config, speedMps, gear: 3);
            var passiveDecel = Calculator.PassiveResistanceDecelKph(config, speedMps, 1f);
            var chassisDecel = Calculator.ChassisCoastDecelKph(config, speedMps, 1f);
            var engineBrakeDecel = Calculator.EngineBrakeDecelKph(
                config,
                gear: 3,
                inReverse: false,
                speedMps: speedMps,
                surfaceDecelerationModifier: 1f,
                currentEngineRpm: engineRpm);
            var brakeDecel = Calculator.BrakeDecelKph(config, brakeInput: 0.35f, surfaceDecelerationModifier: 1f);
            var expectedTotalDecel = passiveDecel + chassisDecel + engineBrakeDecel + brakeDecel;
            var expectedSpeedDeltaKph = -expectedTotalDecel * elapsed;

            var result = LongitudinalStep.Compute(
                new LongitudinalStepInput(
                    config,
                    elapsedSeconds: elapsed,
                    speedMps: speedMps,
                    throttle: 0f,
                    brake: 0.35f,
                    surfaceTractionModifier: 1f,
                    surfaceDecelerationModifier: 1f,
                    longitudinalGripFactor: 1f,
                    gear: 3,
                    inReverse: false,
                    drivelineCouplingFactor: 1f,
                    creepAccelerationMps2: 0f,
                    currentEngineRpm: engineRpm,
                    requestDrive: false,
                    requestBrake: true,
                    applyEngineBraking: true));

            Assert.Equal(expectedSpeedDeltaKph, result.SpeedDeltaKph, 4);
            Assert.Equal(expectedTotalDecel, result.TotalDecelKph, 4);
            Assert.Equal(brakeDecel, result.BrakeDecelKph, 4);
            Assert.Equal(engineBrakeDecel, result.EngineBrakeDecelKph, 4);
        }

        [Fact]
        public void EngineBrakeDecel_RisesWithHigherEngineRpm()
        {
            var config = BuildConfiguration();

            var lowRpmDecel = Calculator.EngineBrakeDecelKph(
                config,
                gear: 2,
                inReverse: false,
                speedMps: 8f,
                surfaceDecelerationModifier: 1f,
                currentEngineRpm: 1400f);
            var highRpmDecel = Calculator.EngineBrakeDecelKph(
                config,
                gear: 2,
                inReverse: false,
                speedMps: 8f,
                surfaceDecelerationModifier: 1f,
                currentEngineRpm: 5200f);

            Assert.True(highRpmDecel > lowRpmDecel);
        }

        [Fact]
        public void EngineBrakeDecel_HigherManualGears_StayAboveRoadLoadAtLowSpeed()
        {
            var config = BuildConfiguration();
            var speedMps = 45f / 3.6f;

            var firstGearDecel = Calculator.EngineBrakeDecelKph(
                config,
                gear: 1,
                inReverse: false,
                speedMps: speedMps,
                surfaceDecelerationModifier: 1f,
                currentEngineRpm: 1700f);
            var thirdGearDecel = Calculator.EngineBrakeDecelKph(
                config,
                gear: 3,
                inReverse: false,
                speedMps: speedMps,
                surfaceDecelerationModifier: 1f,
                currentEngineRpm: 1200f);
            var passiveDecel = (Calculator.ResistiveForce(config, speedMps) / config.MassKg) * 3.6f;

            Assert.True(firstGearDecel > 0f);
            Assert.True(firstGearDecel > thirdGearDecel);
            Assert.True(
                thirdGearDecel > passiveDecel * 1.25f,
                $"Expected higher-gear coast braking to stay above road-load only decel. passive={passiveDecel:0.###}, gear3={thirdGearDecel:0.###}.");
        }

        [Fact]
        public void ManualCoastSimulation_GearThree_DoesNotFreeRollAgainstRoadLoad()
        {
            var config = BuildConfiguration();
            const float elapsed = 0.05f;
            const float simulationSeconds = 2.5f;
            var steps = (int)(simulationSeconds / elapsed);
            var speedWithEngineBrakeKph = 50f;
            var speedPassiveOnlyKph = 50f;

            for (var i = 0; i < steps; i++)
            {
                var speedWithEngineBrakeMps = speedWithEngineBrakeKph / 3.6f;
                var speedPassiveOnlyMps = speedPassiveOnlyKph / 3.6f;
                var thirdGearRpm = Calculator.RpmAtSpeed(config, speedWithEngineBrakeMps, gear: 3);
                var engineBrakeDecel = Calculator.EngineBrakeDecelKph(
                    config,
                    gear: 3,
                    inReverse: false,
                    speedMps: speedWithEngineBrakeMps,
                    surfaceDecelerationModifier: 1f,
                    currentEngineRpm: thirdGearRpm);
                var passiveDecelWithEngine = (Calculator.ResistiveForce(config, speedWithEngineBrakeMps) / config.MassKg) * 3.6f;
                var passiveDecelOnly = (Calculator.ResistiveForce(config, speedPassiveOnlyMps) / config.MassKg) * 3.6f;

                speedWithEngineBrakeKph = System.Math.Max(0f, speedWithEngineBrakeKph - ((engineBrakeDecel + passiveDecelWithEngine) * elapsed));
                speedPassiveOnlyKph = System.Math.Max(0f, speedPassiveOnlyKph - (passiveDecelOnly * elapsed));
            }

            var dropWithEngineBrake = 50f - speedWithEngineBrakeKph;
            var dropPassiveOnly = 50f - speedPassiveOnlyKph;
            Assert.True(
                dropWithEngineBrake >= dropPassiveOnly + 3f,
                $"Expected manual 3rd-gear coast to slow clearly more than passive road load only. withEngine={dropWithEngineBrake:0.###}, passiveOnly={dropPassiveOnly:0.###}.");
        }

        [Fact]
        public void AutomaticMinimumCoupledRpm_NoThrottleAtStandstill_EqualsIdle()
        {
            var config = BuildConfiguration();

            var minimumRpm = Calculator.AutomaticMinimumCoupledRpm(
                config,
                speedMps: 0f,
                throttle: 0f,
                couplingFactor: 0.25f);

            Assert.Equal(config.IdleRpm, minimumRpm, 3);
        }

        [Fact]
        public void AutomaticMinimumCoupledRpm_LaunchAssistFadesAsSpeedIncreases()
        {
            var config = BuildConfiguration();

            var lowSpeedMinimum = Calculator.AutomaticMinimumCoupledRpm(
                config,
                speedMps: 2f / 3.6f,
                throttle: 1f,
                couplingFactor: 0.35f);
            var highSpeedMinimum = Calculator.AutomaticMinimumCoupledRpm(
                config,
                speedMps: 24f / 3.6f,
                throttle: 1f,
                couplingFactor: 0.35f);

            Assert.True(lowSpeedMinimum > config.IdleRpm);
            Assert.True(highSpeedMinimum <= lowSpeedMinimum);
            Assert.True(highSpeedMinimum >= config.IdleRpm);
        }

        [Fact]
        public void AutomaticMinimumCoupledRpm_FullThrottle_HoldsLaunchBandBeforeFade()
        {
            var config = BuildConfiguration();

            var earlyMinimum = Calculator.AutomaticMinimumCoupledRpm(
                config,
                speedMps: 2f / 3.6f,
                throttle: 1f,
                couplingFactor: 0.85f);
            var launchBandMinimum = Calculator.AutomaticMinimumCoupledRpm(
                config,
                speedMps: 9f / 3.6f,
                throttle: 1f,
                couplingFactor: 0.85f);
            var postFadeMinimum = Calculator.AutomaticMinimumCoupledRpm(
                config,
                speedMps: 22f / 3.6f,
                throttle: 1f,
                couplingFactor: 0.85f);

            Assert.True(launchBandMinimum >= earlyMinimum - 5f);
            Assert.True(postFadeMinimum < launchBandMinimum);
        }

        [Fact]
        public void AutomaticLaunchSimulation_FullThrottle_AvoidsMidLaunchRpmDip()
        {
            var config = BuildConfiguration();
            var tuning = AutomaticDrivelineTuning.Default;
            var state = new AutomaticDrivelineState(couplingFactor: 0f, cvtRatio: 0f);
            const float elapsed = 0.05f;
            const float throttle = 1f;
            var speedMps = 0f;
            var previousMinimum = config.IdleRpm;
            var minimumSeenInBand = float.MaxValue;
            var maximumSeenBeforeBand = 0f;
            var minimumAtLaunchEntry = float.MaxValue;

            for (var i = 0; i < 220; i++)
            {
                var minimumCoupledRpm = Calculator.AutomaticMinimumCoupledRpm(
                    config,
                    speedMps,
                    throttle,
                    state.CouplingFactor);

                var output = AutomaticDrivelineModel.Step(
                    TransmissionType.Atc,
                    tuning,
                    new AutomaticDrivelineInput(
                        elapsedSeconds: elapsed,
                        speedMps: speedMps,
                        throttle: throttle,
                        brake: 0f,
                        shifting: false,
                        wheelCircumferenceM: config.WheelRadiusM * 2f * (float)System.Math.PI,
                        finalDriveRatio: config.FinalDriveRatio,
                        idleRpm: config.IdleRpm,
                        revLimiter: config.RevLimiter,
                        launchRpm: config.LaunchRpm,
                        currentEngineRpm: previousMinimum),
                    state);

                state = new AutomaticDrivelineState(output.CouplingFactor, state.CvtRatio);
                previousMinimum = minimumCoupledRpm;

                var speedKph = speedMps * 3.6f;
                if (speedKph >= 2f && speedKph <= 3f && minimumCoupledRpm < minimumAtLaunchEntry)
                    minimumAtLaunchEntry = minimumCoupledRpm;
                if (speedKph >= 2f && speedKph <= 10f)
                {
                    if (minimumCoupledRpm > maximumSeenBeforeBand)
                        maximumSeenBeforeBand = minimumCoupledRpm;
                }

                if (speedKph >= 10f && speedKph <= 14f)
                {
                    if (minimumCoupledRpm < minimumSeenInBand)
                        minimumSeenInBand = minimumCoupledRpm;
                }

                var accelMps2 = Calculator.DriveAccel(
                    config,
                    gear: 1,
                    speedMps: speedMps,
                    throttle: throttle,
                    surfaceTractionModifier: 1f,
                    longitudinalGripFactor: 1f);
                accelMps2 *= output.CouplingFactor;
                if (accelMps2 < 0f)
                    accelMps2 = 0f;
                speedMps += accelMps2 * elapsed;
            }

            Assert.True(maximumSeenBeforeBand > 0f);
            Assert.True(minimumSeenInBand < float.MaxValue);
            Assert.True(minimumAtLaunchEntry > config.IdleRpm + 40f);
            Assert.True(
                maximumSeenBeforeBand - minimumSeenInBand <= 260f,
                $"Expected no deep RPM valley during launch band. maxBeforeBand={maximumSeenBeforeBand:0.##}, minInBand={minimumSeenInBand:0.##}.");
        }

        private static Config BuildConfiguration()
        {
            var torqueCurve = CurveFactory.FromLegacy(
                idleRpm: 900f,
                revLimiter: 7600f,
                peakTorqueRpm: 3600f,
                idleTorqueNm: 180f,
                peakTorqueNm: 650f,
                redlineTorqueNm: 360f);

            return new Config(
                massKg: 1650f,
                drivetrainEfficiency: 0.85f,
                engineBrakingTorqueNm: 300f,
                tireGripCoefficient: 1.0f,
                brakeStrength: 1.0f,
                wheelRadiusM: 0.34f,
                engineBraking: 0.3f,
                idleRpm: 900f,
                revLimiter: 7600f,
                finalDriveRatio: 3.70f,
                powerFactor: 0.7f,
                peakTorqueNm: 650f,
                peakTorqueRpm: 3600f,
                idleTorqueNm: 180f,
                redlineTorqueNm: 360f,
                dragCoefficient: 0.30f,
                frontalAreaM2: 2.2f,
                rollingResistanceCoefficient: 0.015f,
                launchRpm: 2400f,
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

