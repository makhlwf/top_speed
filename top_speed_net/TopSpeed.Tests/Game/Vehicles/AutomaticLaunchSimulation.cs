using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Torque;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class AutomaticLaunchSimulationTests
    {
        private static readonly CarType[] AutomaticLaunchMatrix =
        {
            CarType.Vehicle6, // Toyota Camry (ATC)
            CarType.Vehicle8, // BMW 3 Series (ATC)
            CarType.Vehicle9, // Mercedes Sprinter (ATC)
            CarType.Vehicle1, // Nissan GT-R Nismo (DCT)
            CarType.Vehicle2  // Porsche 911 GT3 RS (DCT)
        };

        [Fact]
        public void AutomaticLaunch_FullThrottle_Matrix_DoesNotExhibitLaunchRpmCliffOrValley()
        {
            for (var i = 0; i < AutomaticLaunchMatrix.Length; i++)
            {
                var carType = AutomaticLaunchMatrix[i];
                var spec = OfficialVehicleCatalog.Get((int)carType);
                Assert.True(
                    TransmissionTypes.IsAutomaticFamily(spec.PrimaryTransmissionType),
                    $"Vehicle {carType} in automatic launch matrix is not automatic-family.");

                var result = SimulateLaunch(spec);
                var jumpLimit = spec.PrimaryTransmissionType == TransmissionType.Dct ? 1200f : 900f;
                var valleyLimit = spec.PrimaryTransmissionType == TransmissionType.Dct ? 320f : 260f;

                Assert.True(result.ReachedOneKph, $"Vehicle {carType} did not reach 1 km/h.");
                Assert.True(result.EnteredBand, $"Vehicle {carType} did not enter 10-14 km/h launch band.");
                Assert.True(
                    result.FirstKphRpm - result.StartRpm <= jumpLimit,
                    $"Vehicle {carType} detected abrupt 0-1 km/h RPM jump. start={result.StartRpm:0.##}, at1kph={result.FirstKphRpm:0.##}, limit={jumpLimit:0.##}.");
                Assert.True(
                    result.MaxRpmBeforeBand - result.MinRpmInBand <= valleyLimit,
                    $"Vehicle {carType} detected launch-band RPM valley. peakBeforeBand={result.MaxRpmBeforeBand:0.##}, minInBand={result.MinRpmInBand:0.##}, limit={valleyLimit:0.##}.");
            }
        }

        private static LaunchSimulationResult SimulateLaunch(OfficialVehicleSpec spec)
        {
            var torqueCurve = CurveFactory.FromLegacy(
                spec.IdleRpm,
                spec.RevLimiter,
                spec.PeakTorqueRpm,
                spec.IdleTorqueNm,
                spec.PeakTorqueNm,
                spec.RedlineTorqueNm);
            var powertrain = new Config(
                spec.MassKg,
                spec.DrivetrainEfficiency,
                spec.EngineBrakingTorqueNm,
                spec.TireGripCoefficient,
                spec.BrakeStrength,
                spec.TireCircumferenceM / (2.0f * (float)System.Math.PI),
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
                spec.Gears,
                spec.GearRatios,
                torqueCurve,
                coastDragBaseMps2: spec.CoastDragBaseMps2 >= 0f ? spec.CoastDragBaseMps2 : 0f,
                coastDragLinearPerMps: spec.CoastDragLinearPerMps >= 0f ? spec.CoastDragLinearPerMps : 0f,
                engineFrictionLinearNmPerKrpm: spec.FrictionLinearNmPerKrpm >= 0f ? spec.FrictionLinearNmPerKrpm : 0f,
                engineFrictionQuadraticNmPerKrpm2: spec.FrictionQuadraticNmPerKrpm2 >= 0f ? spec.FrictionQuadraticNmPerKrpm2 : 0f,
                idleControlWindowRpm: spec.IdleControlWindowRpm >= 0f ? spec.IdleControlWindowRpm : 150f,
                idleControlGainNmPerRpm: spec.IdleControlGainNmPerRpm >= 0f ? spec.IdleControlGainNmPerRpm : 0.08f,
                minCoupledRiseIdleRpmPerSecond: spec.MinCoupledRiseIdleRpmPerSecond >= 0f ? spec.MinCoupledRiseIdleRpmPerSecond : 2200f,
                minCoupledRiseFullRpmPerSecond: spec.MinCoupledRiseFullRpmPerSecond >= 0f ? spec.MinCoupledRiseFullRpmPerSecond : 6200f,
                engineOverrunIdleLossFraction: spec.EngineOverrunIdleLossFraction >= 0f ? spec.EngineOverrunIdleLossFraction : 0.35f,
                overrunCurveExponent: spec.OverrunCurveExponent >= 0f ? spec.OverrunCurveExponent : 1f,
                engineBrakeTransferEfficiency: spec.EngineBrakeTransferEfficiency >= 0f ? spec.EngineBrakeTransferEfficiency : 0.68f);
            var engine = new EngineModel(
                spec.IdleRpm,
                spec.MaxRpm,
                spec.RevLimiter,
                spec.AutoShiftRpm,
                spec.EngineBraking,
                spec.TopSpeed,
                spec.FinalDriveRatio,
                spec.TireCircumferenceM,
                spec.Gears,
                spec.GearRatios,
                spec.PeakTorqueNm,
                spec.PeakTorqueRpm,
                spec.IdleTorqueNm,
                spec.RedlineTorqueNm,
                spec.EngineBrakingTorqueNm,
                spec.PowerFactor,
                spec.EngineInertiaKgm2,
                spec.EngineFrictionTorqueNm,
                spec.DrivelineCouplingRate,
                torqueCurve,
                engineOverrunIdleLossFraction: spec.EngineOverrunIdleLossFraction >= 0f ? spec.EngineOverrunIdleLossFraction : 0.35f,
                engineFrictionLinearNmPerKrpm: spec.FrictionLinearNmPerKrpm >= 0f ? spec.FrictionLinearNmPerKrpm : 0f,
                engineFrictionQuadraticNmPerKrpm2: spec.FrictionQuadraticNmPerKrpm2 >= 0f ? spec.FrictionQuadraticNmPerKrpm2 : 0f,
                idleControlWindowRpm: spec.IdleControlWindowRpm >= 0f ? spec.IdleControlWindowRpm : 150f,
                idleControlGainNmPerRpm: spec.IdleControlGainNmPerRpm >= 0f ? spec.IdleControlGainNmPerRpm : 0.08f,
                minCoupledRiseIdleRpmPerSecond: spec.MinCoupledRiseIdleRpmPerSecond >= 0f ? spec.MinCoupledRiseIdleRpmPerSecond : 2200f,
                minCoupledRiseFullRpmPerSecond: spec.MinCoupledRiseFullRpmPerSecond >= 0f ? spec.MinCoupledRiseFullRpmPerSecond : 6200f,
                overrunCurveExponent: spec.OverrunCurveExponent >= 0f ? spec.OverrunCurveExponent : 1f);
            engine.StartEngine();

            const float elapsed = 0.016f;
            const int launchGear = 1;
            const float throttle = 1f;
            var speedMps = 0f;
            var coupling = 1f;
            var startRpm = engine.Rpm;
            var firstKphRpm = float.NaN;
            var minRpmInBand = float.MaxValue;
            var maxRpmBeforeBand = 0f;

            for (var i = 0; i < 520; i++)
            {
                var autoOutput = AutomaticDrivelineModel.Step(
                    spec.PrimaryTransmissionType,
                    spec.AutomaticTuning,
                    new AutomaticDrivelineInput(
                        elapsed,
                        speedMps,
                        throttle,
                        brake: 0f,
                        shifting: false,
                        wheelCircumferenceM: spec.TireCircumferenceM,
                        finalDriveRatio: spec.FinalDriveRatio,
                        idleRpm: spec.IdleRpm,
                        revLimiter: spec.RevLimiter,
                        launchRpm: spec.LaunchRpm,
                        currentEngineRpm: engine.Rpm),
                    new AutomaticDrivelineState(coupling, cvtRatio: 0f));
                coupling = autoOutput.CouplingFactor;

                var longitudinal = LongitudinalStep.Compute(
                    new LongitudinalStepInput(
                        powertrain,
                        elapsed,
                        speedMps,
                        throttle,
                        brake: 0f,
                        surfaceTractionModifier: 1f,
                        surfaceDecelerationModifier: 1f,
                        longitudinalGripFactor: 1f,
                        launchGear,
                        inReverse: false,
                        drivelineCouplingFactor: coupling,
                        creepAccelerationMps2: autoOutput.CreepAccelerationMps2,
                        currentEngineRpm: engine.Rpm,
                        requestDrive: true,
                        requestBrake: false,
                        applyEngineBraking: false,
                        driveRatioOverride: autoOutput.EffectiveDriveRatio > 0f ? autoOutput.EffectiveDriveRatio : (float?)null));
                speedMps = System.Math.Max(0f, speedMps + (longitudinal.SpeedDeltaKph / 3.6f));
                var speedKph = speedMps * 3.6f;

                var minimumCoupledRpm = Calculator.AutomaticMinimumCoupledRpm(
                    powertrain,
                    speedMps,
                    throttle,
                    coupling);
                engine.SyncFromSpeed(
                    speedKph,
                    launchGear,
                    elapsed,
                    throttleInput: 100,
                    inReverse: false,
                    couplingMode: EngineCouplingMode.Blended,
                    couplingFactor: coupling,
                    driveRatioOverride: autoOutput.EffectiveDriveRatio > 0f ? autoOutput.EffectiveDriveRatio : (float?)null,
                    minimumCoupledRpm: minimumCoupledRpm);

                if (float.IsNaN(firstKphRpm) && speedKph >= 1f)
                    firstKphRpm = engine.Rpm;
                if (speedKph >= 1f && speedKph <= 10f && engine.Rpm > maxRpmBeforeBand)
                    maxRpmBeforeBand = engine.Rpm;
                if (speedKph >= 10f && speedKph <= 14f && engine.Rpm < minRpmInBand)
                    minRpmInBand = engine.Rpm;
            }

            return new LaunchSimulationResult(
                startRpm,
                firstKphRpm,
                maxRpmBeforeBand,
                minRpmInBand);
        }

        private readonly struct LaunchSimulationResult
        {
            public LaunchSimulationResult(float startRpm, float firstKphRpm, float maxRpmBeforeBand, float minRpmInBand)
            {
                StartRpm = startRpm;
                FirstKphRpm = firstKphRpm;
                MaxRpmBeforeBand = maxRpmBeforeBand;
                MinRpmInBand = minRpmInBand;
            }

            public float StartRpm { get; }
            public float FirstKphRpm { get; }
            public float MaxRpmBeforeBand { get; }
            public float MinRpmInBand { get; }
            public bool ReachedOneKph => !float.IsNaN(FirstKphRpm);
            public bool EnteredBand => MaxRpmBeforeBand > 0f && MinRpmInBand < float.MaxValue;
        }
    }
}

