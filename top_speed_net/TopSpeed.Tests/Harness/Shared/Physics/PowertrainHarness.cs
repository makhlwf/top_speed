using System;
using System.Collections.Generic;
using System.Linq;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Torque;
using TopSpeed.Vehicles;

namespace TopSpeed.Tests
{
    internal static class PowertrainHarness
    {
        public static Config BuildConfig(OfficialVehicleSpec spec)
        {
            var torqueCurve = CurveFactory.FromLegacy(
                spec.IdleRpm,
                spec.RevLimiter,
                spec.PeakTorqueRpm,
                spec.IdleTorqueNm,
                spec.PeakTorqueNm,
                spec.RedlineTorqueNm);

            return new Config(
                spec.MassKg,
                spec.DrivetrainEfficiency,
                spec.EngineBrakingTorqueNm,
                spec.TireGripCoefficient,
                spec.BrakeStrength,
                spec.TireCircumferenceM / (2.0f * (float)Math.PI),
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
        }

        public static CoastTrace SimulateNeutralCoast(OfficialVehicleSpec spec, float startSpeedKph = 100f, float seconds = 8f)
        {
            var config = BuildConfig(spec);
            const float elapsed = 0.05f;
            var speedKph = startSpeedKph;
            var steps = (int)(seconds / elapsed);
            var samples = new List<CoastSample>();

            for (var i = 0; i < steps; i++)
            {
                var speedMps = speedKph / 3.6f;
                var passiveDecel = Calculator.PassiveResistanceDecelKph(config, speedMps, 1f);
                var chassisDecel = Calculator.ChassisCoastDecelKph(config, speedMps, 1f);
                speedKph = Math.Max(0f, speedKph - ((passiveDecel + chassisDecel) * elapsed));

                if (i % 20 == 0 || i == steps - 1)
                {
                    samples.Add(new CoastSample(
                        TimeSeconds: Rounding.F((i + 1) * elapsed, 2),
                        SpeedKph: Rounding.F(speedKph, 2),
                        PassiveDecelKph: Rounding.F(passiveDecel),
                        ChassisDecelKph: Rounding.F(chassisDecel)));
                }
            }

            return new CoastTrace(
                spec.Name,
                StartSpeedKph: startSpeedKph,
                FinalSpeedKph: Rounding.F(speedKph, 2),
                Samples: samples);
        }

        public static IReadOnlyList<VehicleCatalogSnapshot> BuildCatalogSnapshot()
        {
            return OfficialVehicleCatalog.Vehicles
                .Select(spec => new VehicleCatalogSnapshot(
                    spec.Name,
                    spec.PrimaryTransmissionType.ToString(),
                    spec.GearRatios.Length,
                    spec.TopSpeed,
                    TopGearKph: Rounding.F(GearTopSpeedKph(spec, spec.GearRatios.Length), 1),
                    PreviousGearKph: spec.GearRatios.Length > 1 ? Rounding.F(GearTopSpeedKph(spec, spec.GearRatios.Length - 1), 1) : 0f,
                    CoastDragBaseMps2: Rounding.F(spec.CoastDragBaseMps2),
                    CoastDragLinearPerMps: Rounding.F(spec.CoastDragLinearPerMps)))
                .ToArray();
        }

        public static float GearTopSpeedKph(OfficialVehicleSpec spec, int gear)
        {
            var ratio = spec.GearRatios[gear - 1] * spec.FinalDriveRatio;
            var speedMps = (spec.RevLimiter / 60f) * spec.TireCircumferenceM / ratio;
            return speedMps * 3.6f;
        }
    }

    internal sealed record CoastTrace(
        string Vehicle,
        float StartSpeedKph,
        float FinalSpeedKph,
        IReadOnlyList<CoastSample> Samples);

    internal sealed record CoastSample(
        float TimeSeconds,
        float SpeedKph,
        float PassiveDecelKph,
        float ChassisDecelKph);

    internal sealed record VehicleCatalogSnapshot(
        string Vehicle,
        string Transmission,
        int Gears,
        float ConfiguredTopSpeedKph,
        float TopGearKph,
        float PreviousGearKph,
        float CoastDragBaseMps2,
        float CoastDragLinearPerMps);
}
