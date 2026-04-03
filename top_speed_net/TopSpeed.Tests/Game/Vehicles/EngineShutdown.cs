using TopSpeed.Physics.Powertrain;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class EngineShutdownTests
    {
        [Fact]
        public void StepShutdown_FreeRevRpmDecaysMonotonicallyToZero()
        {
            var engine = BuildEngine();
            engine.StartEngine();

            for (var i = 0; i < 16; i++)
            {
                engine.SyncFromSpeed(
                    speedGameUnits: 0f,
                    gear: 1,
                    elapsed: 0.05f,
                    throttleInput: 100,
                    inReverse: false,
                    couplingMode: EngineCouplingMode.Disengaged,
                    couplingFactor: 0f);
            }

            Assert.True(engine.Rpm > engine.IdleRpm + 400f);

            var previousRpm = engine.Rpm;
            var reachedZero = false;
            for (var i = 0; i < 220; i++)
            {
                engine.StepShutdown(speedGameUnits: 0f, elapsed: 0.05f);
                Assert.True(
                    engine.Rpm <= previousRpm + 0.001f,
                    $"Expected shutdown RPM to be non-increasing. previous={previousRpm:0.###}, current={engine.Rpm:0.###}.");
                previousRpm = engine.Rpm;
                if (engine.Rpm <= 0f)
                {
                    reachedZero = true;
                    break;
                }
            }

            Assert.True(reachedZero, $"Expected engine shutdown to reach zero RPM. final={engine.Rpm:0.###}.");
        }

        [Fact]
        public void StepShutdown_StillAccumulatesDistanceFromVehicleSpeed()
        {
            var engine = BuildEngine();
            engine.StartEngine();

            var before = engine.DistanceMeters;
            engine.StepShutdown(speedGameUnits: 36f, elapsed: 1f); // 10 m/s for 1s
            var after = engine.DistanceMeters;

            Assert.True(after >= before + 9.9f, $"Expected distance to advance during shutdown. before={before:0.###}, after={after:0.###}.");
        }

        private static EngineModel BuildEngine()
        {
            return new EngineModel(
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
        }
    }
}

