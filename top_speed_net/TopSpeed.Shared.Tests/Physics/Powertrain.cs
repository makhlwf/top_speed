using TopSpeed.Physics.Powertrain;
using TopSpeed.Physics.Torque;
using Xunit;

namespace TopSpeed.Shared.Tests.Physics
{
    public sealed class Powertrain
    {
        [Fact]
        public void TorqueCurveProfile_Interpolates_AndClamps()
        {
            var curve = new CurveProfile(new[]
            {
                new CurvePoint(1000f, 100f),
                new CurvePoint(3000f, 300f),
                new CurvePoint(6000f, 180f)
            });

            Assert.Equal(100f, curve.EvaluateTorque(500f));
            Assert.Equal(300f, curve.EvaluateTorque(3000f));
            Assert.Equal(240f, curve.EvaluateTorque(4500f));
            Assert.Equal(180f, curve.EvaluateTorque(7000f));
        }

        [Fact]
        public void PowertrainCalculator_Computes_PositiveDriveAcceleration()
        {
            var configuration = BuildConfiguration();
            var acceleration = Calculator.DriveAccel(
                configuration,
                gear: 2,
                speedMps: 22f,
                throttle: 0.8f,
                surfaceTractionModifier: 1f,
                longitudinalGripFactor: 1f);

            Assert.True(acceleration > 0f);
        }

        [Fact]
        public void PowertrainCalculator_Computes_EngineBrakingDeceleration()
        {
            var configuration = BuildConfiguration();
            var decelKph = Calculator.EngineBrakeDecelKph(
                configuration,
                gear: 3,
                inReverse: false,
                speedMps: 35f,
                surfaceDecelerationModifier: 1f,
                currentEngineRpm: 4500f);

            Assert.True(decelKph > 0f);
        }

        [Fact]
        public void PowertrainCalculator_ComputesHorsepower()
        {
            var horsepower = Calculator.Horsepower(400f, 6000f);
            Assert.InRange(horsepower, 336f, 338f);
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



