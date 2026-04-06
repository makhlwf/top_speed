using System.Collections.Generic;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;

namespace TopSpeed.Tests
{
    internal static class EngineHarness
    {
        public static EngineModel BuildEngine() =>
            new(
                idleRpm: 900f,
                maxRpm: 7800f,
                revLimiter: 7600f,
                autoShiftRpm: 7000f,
                engineBraking: 0.3f,
                topSpeedKmh: 320f,
                finalDriveRatio: 3.70f,
                tireCircumferenceM: 2.14f,
                gearCount: 6,
                gearRatios: new[] { 3.5f, 2.2f, 1.5f, 1.2f, 1.0f, 0.85f },
                peakTorqueNm: 650f,
                peakTorqueRpm: 3600f,
                idleTorqueNm: 180f,
                redlineTorqueNm: 360f,
                engineBrakingTorqueNm: 300f,
                powerFactor: 0.7f,
                engineInertiaKgm2: 0.24f,
                engineFrictionTorqueNm: 20f,
                drivelineCouplingRate: 12f);

        public static EngineTrace SimulateDisengagedRevBlip()
        {
            var engine = BuildEngine();
            engine.StartEngine();
            var samples = new List<EngineSample>();

            for (var i = 0; i < 16; i++)
                StepSync(engine, 100, EngineCouplingMode.Disengaged, 0f, 0f, samples, i);

            for (var i = 16; i < 56; i++)
                StepSync(engine, 0, EngineCouplingMode.Disengaged, 0f, 0f, samples, i);

            return new EngineTrace("DisengagedRevBlip", samples);
        }

        public static EngineTrace SimulateFreeRevShutdown()
        {
            var engine = BuildEngine();
            engine.StartEngine();
            var samples = new List<EngineSample>();

            for (var i = 0; i < 16; i++)
                StepSync(engine, 100, EngineCouplingMode.Disengaged, 0f, 0f, samples, i);

            for (var i = 16; i < 120 && engine.Rpm > 0f; i++)
            {
                engine.StepShutdown(speedGameUnits: 0f, elapsed: 0.05f);
                if (i % 8 == 0 || engine.Rpm <= 0f)
                {
                    samples.Add(new EngineSample(
                        Step: i,
                        Rpm: Rounding.F(engine.Rpm, 1),
                        Horsepower: Rounding.F(engine.NetHorsepower, 2),
                        DistanceMeters: Rounding.F(engine.DistanceMeters, 2)));
                }
            }

            return new EngineTrace("FreeRevShutdown", samples);
        }

        public static EngineTrace SimulateBackDrivenCombustionOff()
        {
            var engine = BuildEngine();
            engine.StartEngine();
            var samples = new List<EngineSample>();

            engine.SyncFromSpeed(
                speedGameUnits: 45f,
                gear: 3,
                elapsed: 0.05f,
                throttleInput: 0,
                inReverse: false,
                couplingMode: EngineCouplingMode.Locked,
                couplingFactor: 1f,
                minimumCoupledRpm: 0f,
                combustionEnabled: false);

            samples.Add(new EngineSample(
                Step: 0,
                Rpm: Rounding.F(engine.Rpm, 1),
                Horsepower: Rounding.F(engine.NetHorsepower, 2),
                DistanceMeters: Rounding.F(engine.DistanceMeters, 2)));

            return new EngineTrace("BackDrivenCombustionOff", samples);
        }

        public static IReadOnlyList<CarType> AutomaticVehicles => new[]
        {
            CarType.Vehicle1,
            CarType.Vehicle2,
            CarType.Vehicle6,
            CarType.Vehicle8,
            CarType.Vehicle9
        };

        public static LaunchTrace SimulateAutomaticLaunch(OfficialVehicleSpec spec)
        {
            var powertrain = PowertrainHarness.BuildConfig(spec);
            var torqueCurve = TopSpeed.Physics.Torque.CurveFactory.FromLegacy(
                spec.IdleRpm,
                spec.RevLimiter,
                spec.PeakTorqueRpm,
                spec.IdleTorqueNm,
                spec.PeakTorqueNm,
                spec.RedlineTorqueNm);

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

            const float elapsed = 0.016f;
            const int launchGear = 1;
            const float throttle = 1f;
            engine.StartEngine();

            var speedMps = 0f;
            var coupling = 1f;
            var samples = new List<LaunchSample>();
            var maxRpmBeforeBand = 0f;
            var minRpmInBand = float.MaxValue;
            float? firstKphRpm = null;

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
                var minimumCoupledRpm = Calculator.AutomaticMinimumCoupledRpm(powertrain, speedMps, throttle, coupling);
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

                if (!firstKphRpm.HasValue && speedKph >= 1f)
                    firstKphRpm = engine.Rpm;
                if (speedKph >= 1f && speedKph <= 10f && engine.Rpm > maxRpmBeforeBand)
                    maxRpmBeforeBand = engine.Rpm;
                if (speedKph >= 10f && speedKph <= 14f && engine.Rpm < minRpmInBand)
                    minRpmInBand = engine.Rpm;
                if (i % 20 == 0 || i == 519)
                {
                    samples.Add(new LaunchSample(
                        Step: i,
                        SpeedKph: Rounding.F(speedKph, 2),
                        Rpm: Rounding.F(engine.Rpm, 1),
                        Coupling: Rounding.F(coupling),
                        Gear: launchGear,
                        Ratio: Rounding.F(autoOutput.EffectiveDriveRatio),
                        CreepMps2: Rounding.F(autoOutput.CreepAccelerationMps2)));
                }
            }

            return new LaunchTrace(
                spec.Name,
                FirstKphRpm: Rounding.F(firstKphRpm ?? 0f, 1),
                MaxRpmBeforeBand: Rounding.F(maxRpmBeforeBand, 1),
                MinRpmInBand: Rounding.F(minRpmInBand < float.MaxValue ? minRpmInBand : 0f, 1),
                Samples: samples);
        }

        private static void StepSync(
            EngineModel engine,
            int throttleInput,
            EngineCouplingMode couplingMode,
            float couplingFactor,
            float minimumCoupledRpm,
            ICollection<EngineSample> samples,
            int step)
        {
            engine.SyncFromSpeed(
                speedGameUnits: 0f,
                gear: 1,
                elapsed: 0.05f,
                throttleInput: throttleInput,
                inReverse: false,
                couplingMode: couplingMode,
                couplingFactor: couplingFactor,
                minimumCoupledRpm: minimumCoupledRpm);

            if (step % 4 == 0)
            {
                samples.Add(new EngineSample(
                    Step: step,
                    Rpm: Rounding.F(engine.Rpm, 1),
                    Horsepower: Rounding.F(engine.NetHorsepower, 2),
                    DistanceMeters: Rounding.F(engine.DistanceMeters, 2)));
            }
        }
    }

    internal sealed record EngineTrace(string Scenario, IReadOnlyList<EngineSample> Samples);

    internal sealed record EngineSample(int Step, float Rpm, float Horsepower, float DistanceMeters);

    internal sealed record LaunchTrace(
        string Vehicle,
        float FirstKphRpm,
        float MaxRpmBeforeBand,
        float MinRpmInBand,
        IReadOnlyList<LaunchSample> Samples);

    internal sealed record LaunchSample(
        int Step,
        float SpeedKph,
        float Rpm,
        float Coupling,
        int Gear,
        float Ratio,
        float CreepMps2);
}
