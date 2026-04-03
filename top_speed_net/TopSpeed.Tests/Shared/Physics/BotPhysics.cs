using System;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Torque;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests.Physics
{
    [Trait("Category", "SharedPhysics")]
    public sealed class BotPhysicsTests
    {
        private static readonly CarType[] AutomaticShiftMatrix =
        {
            CarType.Vehicle6, // ATC
            CarType.Vehicle8, // ATC
            CarType.Vehicle9, // ATC
            CarType.Vehicle1, // DCT
            CarType.Vehicle2  // DCT
        };

        private static readonly CarType[] ManualCoastMatrix =
        {
            CarType.Vehicle3,  // Fiat 500
            CarType.Vehicle10  // Kawasaki ZX-10R
        };

        [Fact]
        public void BotPhysics_AllOfficialVehicles_CanLaunchFromStandstill()
        {
            foreach (CarType carType in Enum.GetValues(typeof(CarType)))
            {
                if (carType == CarType.CustomVehicle)
                    continue;

                var config = BotPhysicsCatalog.Get(carType);
                var state = new BotPhysicsState
                {
                    PositionX = 0f,
                    PositionY = 0f,
                    SpeedKph = 0f,
                    LateralVelocityMps = 0f,
                    YawRateRad = 0f,
                    Gear = 1,
                    AutoShiftCooldownSeconds = 0f
                };

                var input = new BotPhysicsInput(
                    elapsedSeconds: 0.1f,
                    surface: TrackSurface.Asphalt,
                    throttle: 100,
                    brake: 0,
                    steering: 0);

                TopSpeed.Bots.BotPhysics.Step(config, ref state, input);

                Assert.True(state.SpeedKph > 0f, $"{carType} failed to launch from standstill.");
            }
        }

        [Fact]
        public void BotPhysics_AutomaticShift_Matrix_ShiftsUpFromLaunch()
        {
            for (var i = 0; i < AutomaticShiftMatrix.Length; i++)
            {
                var carType = AutomaticShiftMatrix[i];
                var config = BuildConfigFromOfficial(carType);
                Assert.True(
                    TransmissionTypes.IsAutomaticFamily(config.ActiveTransmissionType),
                    $"Matrix vehicle {carType} is not automatic-family.");

                var state = new BotPhysicsState
                {
                    PositionX = 0f,
                    PositionY = 0f,
                    SpeedKph = 0f,
                    LateralVelocityMps = 0f,
                    YawRateRad = 0f,
                    Gear = 1,
                    AutoShiftCooldownSeconds = 0f,
                    AutomaticCouplingFactor = 1f,
                    CvtRatio = config.AutomaticTuning.Cvt.RatioMax,
                    EffectiveDriveRatio = 0f
                };
                var input = new BotPhysicsInput(
                    elapsedSeconds: 0.05f,
                    surface: TrackSurface.Asphalt,
                    throttle: 100,
                    brake: 0,
                    steering: 0);

                for (var step = 0; step < 260; step++)
                    BotPhysics.Step(config, ref state, input);

                Assert.True(state.SpeedKph > 25f, $"Vehicle {carType} did not build launch speed. speed={state.SpeedKph:0.###}.");
                Assert.True(state.Gear > 1, $"Vehicle {carType} did not upshift during launch. finalGear={state.Gear}.");
                Assert.True(state.AutoShiftCooldownSeconds >= 0f, $"Vehicle {carType} cooldown dropped below zero.");
            }
        }

        [Fact]
        public void BotPhysics_ManualHighGearCoast_DeceleratesWithPassiveResistance()
        {
            var config = BuildManualConfig();
            var state = new BotPhysicsState
            {
                PositionX = 0f,
                PositionY = 0f,
                SpeedKph = 40f,
                LateralVelocityMps = 0f,
                YawRateRad = 0f,
                Gear = 3,
                AutoShiftCooldownSeconds = 0f,
                AutomaticCouplingFactor = 1f,
                CvtRatio = config.AutomaticTuning.Cvt.RatioMax,
                EffectiveDriveRatio = 0f
            };

            var input = new BotPhysicsInput(
                elapsedSeconds: 1.0f,
                surface: TrackSurface.Asphalt,
                throttle: 0,
                brake: 0,
                steering: 0);

            BotPhysics.Step(config, ref state, input);

            Assert.True(
                state.SpeedKph <= 39.8f,
                $"Expected manual high-gear coast to decelerate clearly from 40 kph. Actual={state.SpeedKph:0.###} kph.");
        }

        [Fact]
        public void BotPhysics_ManualCoast_GearThree_RemainsCloseToGearOneDecel()
        {
            var config = BuildManualConfig();
            var gearOne = new BotPhysicsState
            {
                PositionX = 0f,
                PositionY = 0f,
                SpeedKph = 45f,
                LateralVelocityMps = 0f,
                YawRateRad = 0f,
                Gear = 1,
                AutoShiftCooldownSeconds = 0f,
                AutomaticCouplingFactor = 1f,
                CvtRatio = config.AutomaticTuning.Cvt.RatioMax,
                EffectiveDriveRatio = 0f
            };
            var gearThree = new BotPhysicsState
            {
                PositionX = 0f,
                PositionY = 0f,
                SpeedKph = 45f,
                LateralVelocityMps = 0f,
                YawRateRad = 0f,
                Gear = 3,
                AutoShiftCooldownSeconds = 0f,
                AutomaticCouplingFactor = 1f,
                CvtRatio = config.AutomaticTuning.Cvt.RatioMax,
                EffectiveDriveRatio = 0f
            };
            var input = new BotPhysicsInput(
                elapsedSeconds: 0.05f,
                surface: TrackSurface.Asphalt,
                throttle: 0,
                brake: 0,
                steering: 0);

            for (var i = 0; i < 40; i++)
            {
                BotPhysics.Step(config, ref gearOne, input);
                BotPhysics.Step(config, ref gearThree, input);
            }

            var dropGearOne = 45f - gearOne.SpeedKph;
            var dropGearThree = 45f - gearThree.SpeedKph;
            Assert.True(dropGearOne > 0f);
            Assert.True(
                dropGearThree >= 4f,
                $"Expected substantial high-gear lift-off deceleration. drop3={dropGearThree:0.###}.");
            Assert.True(
                dropGearThree >= dropGearOne * 0.55f,
                $"Expected gear 3 coast decel to remain in the same order as gear 1. drop1={dropGearOne:0.###}, drop3={dropGearThree:0.###}.");
        }

        [Fact]
        public void BotPhysics_OfficialManualMatrix_CoastInHigherGear_BeatsRoadLoadBaseline()
        {
            for (var i = 0; i < ManualCoastMatrix.Length; i++)
            {
                var carType = ManualCoastMatrix[i];
                var config = BuildConfigFromOfficial(carType);
                Assert.Equal(TransmissionType.Manual, config.ActiveTransmissionType);

                var initialSpeedKph = 45f;
                var activeState = new BotPhysicsState
                {
                    PositionX = 0f,
                    PositionY = 0f,
                    SpeedKph = initialSpeedKph,
                    LateralVelocityMps = 0f,
                    YawRateRad = 0f,
                    Gear = Math.Min(3, config.Gears),
                    AutoShiftCooldownSeconds = 0f,
                    AutomaticCouplingFactor = 1f,
                    CvtRatio = config.AutomaticTuning.Cvt.RatioMax,
                    EffectiveDriveRatio = 0f
                };

                var passiveBaselineKph = initialSpeedKph;
                var input = new BotPhysicsInput(
                    elapsedSeconds: 0.05f,
                    surface: TrackSurface.Asphalt,
                    throttle: 0,
                    brake: 0,
                    steering: 0);

                for (var step = 0; step < 40; step++)
                {
                    BotPhysics.Step(config, ref activeState, input);
                    var speedMps = passiveBaselineKph / 3.6f;
                    var passiveDecel = Calculator.PassiveResistanceDecelKph(config.Powertrain, speedMps, 1f);
                    var coastDecel = Calculator.ChassisCoastDecelKph(config.Powertrain, speedMps, 1f);
                    passiveBaselineKph = Math.Max(0f, passiveBaselineKph - ((passiveDecel + coastDecel) * input.ElapsedSeconds));
                }

                var activeDrop = initialSpeedKph - activeState.SpeedKph;
                var passiveDrop = initialSpeedKph - passiveBaselineKph;
                Assert.True(activeDrop > 0f, $"Vehicle {carType} did not decelerate on lift-off.");
                Assert.True(
                    activeDrop >= passiveDrop + 1.5f,
                    $"Vehicle {carType} higher-gear coast too close to passive road-load baseline. activeDrop={activeDrop:0.###}, passiveDrop={passiveDrop:0.###}.");
            }
        }

        private static BotPhysicsConfig BuildConfigFromOfficial(CarType carType)
        {
            var spec = OfficialVehicleCatalog.Get((int)carType);
            var wheelRadiusM = Math.Max(0.01f, spec.TireCircumferenceM / (2.0f * (float)Math.PI));
            var torqueCurve = CurveFactory.FromLegacy(
                spec.IdleRpm,
                spec.RevLimiter,
                spec.PeakTorqueRpm,
                spec.IdleTorqueNm,
                spec.PeakTorqueNm,
                spec.RedlineTorqueNm);

            return new BotPhysicsConfig(
                spec.SurfaceTractionFactor,
                spec.Deceleration,
                spec.TopSpeed,
                spec.MassKg,
                spec.DrivetrainEfficiency,
                spec.EngineBrakingTorqueNm,
                spec.TireGripCoefficient,
                spec.BrakeStrength,
                wheelRadiusM,
                spec.EngineBraking,
                spec.IdleRpm,
                spec.RevLimiter,
                spec.FinalDriveRatio,
                spec.PowerFactor,
                spec.PeakTorqueNm,
                spec.PeakTorqueRpm,
                spec.IdleTorqueNm,
                spec.RedlineTorqueNm,
                spec.DragCoefficient,
                spec.FrontalAreaM2,
                spec.RollingResistanceCoefficient,
                spec.LaunchRpm,
                spec.ReversePowerFactor,
                spec.ReverseGearRatio,
                spec.EngineInertiaKgm2,
                spec.EngineFrictionTorqueNm,
                spec.DrivelineCouplingRate,
                spec.LateralGripCoefficient,
                spec.HighSpeedStability,
                spec.WheelbaseM,
                spec.WidthM,
                spec.LengthM,
                spec.MaxSteerDeg,
                spec.Steering,
                spec.HighSpeedSteerGain,
                spec.HighSpeedSteerStartKph,
                spec.HighSpeedSteerFullKph,
                spec.CombinedGripPenalty,
                spec.SlipAnglePeakDeg,
                spec.SlipAngleFalloff,
                spec.TurnResponse,
                spec.MassSensitivity,
                spec.DownforceGripGain,
                spec.CornerStiffnessFront,
                spec.CornerStiffnessRear,
                spec.YawInertiaScale,
                spec.SteeringCurve,
                spec.TransientDamping,
                spec.Gears,
                torqueCurve,
                spec.GearRatios,
                spec.TransmissionPolicy,
                activeTransmissionType: spec.PrimaryTransmissionType,
                automaticTuning: spec.AutomaticTuning,
                coastDragBaseMps2: spec.CoastDragBaseMps2,
                coastDragLinearPerMps: spec.CoastDragLinearPerMps,
                frictionLinearNmPerKrpm: spec.FrictionLinearNmPerKrpm,
                frictionQuadraticNmPerKrpm2: spec.FrictionQuadraticNmPerKrpm2,
                idleControlWindowRpm: spec.IdleControlWindowRpm,
                idleControlGainNmPerRpm: spec.IdleControlGainNmPerRpm,
                minCoupledRiseIdleRpmPerSecond: spec.MinCoupledRiseIdleRpmPerSecond,
                minCoupledRiseFullRpmPerSecond: spec.MinCoupledRiseFullRpmPerSecond,
                engineOverrunIdleLossFraction: spec.EngineOverrunIdleLossFraction,
                overrunCurveExponent: spec.OverrunCurveExponent,
                engineBrakeTransferEfficiency: spec.EngineBrakeTransferEfficiency);
        }

        private static BotPhysicsConfig BuildManualConfig()
        {
            var torqueCurve = CurveFactory.FromLegacy(
                idleRpm: 900f,
                revLimiter: 7600f,
                peakTorqueRpm: 3600f,
                idleTorqueNm: 180f,
                peakTorqueNm: 650f,
                redlineTorqueNm: 360f);

            return new BotPhysicsConfig(
                surfaceTractionFactor: 1f,
                deceleration: 0.30f,
                topSpeedKph: 320f,
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
                lateralGripCoefficient: 1.0f,
                highSpeedStability: 0.4f,
                wheelbaseM: 2.75f,
                widthM: 1.85f,
                lengthM: 4.65f,
                maxSteerDeg: 35f,
                steering: 1f,
                highSpeedSteerGain: 1.0f,
                highSpeedSteerStartKph: 140f,
                highSpeedSteerFullKph: 240f,
                combinedGripPenalty: 0.72f,
                slipAnglePeakDeg: 8f,
                slipAngleFalloff: 1.25f,
                turnResponse: 1f,
                massSensitivity: 0.75f,
                downforceGripGain: 0.05f,
                cornerStiffnessFront: 1f,
                cornerStiffnessRear: 1f,
                yawInertiaScale: 1f,
                steeringCurve: 1f,
                transientDamping: 1f,
                gears: 6,
                torqueCurve: torqueCurve,
                gearRatios: new[] { 3.5f, 2.2f, 1.5f, 1.2f, 1.0f, 0.85f },
                transmissionPolicy: TransmissionPolicy.Default,
                activeTransmissionType: TransmissionType.Manual,
                automaticTuning: AutomaticDrivelineTuning.Default);
        }

    }
}



