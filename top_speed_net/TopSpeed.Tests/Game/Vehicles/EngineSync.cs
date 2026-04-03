using TopSpeed.Physics.Powertrain;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class EngineSyncTests
    {
        [Fact]
        public void SyncFromSpeed_AutomaticStandstill_DoesNotDipBelowIdle()
        {
            var engine = BuildEngine();
            engine.StartEngine();
            var minimumRpm = engine.Rpm;

            for (var i = 0; i < 24; i++)
            {
                var blend = i / 23f;
                var couplingFactor = 1f - (0.78f * blend);
                engine.SyncFromSpeed(
                    speedGameUnits: 0f,
                    gear: 1,
                    elapsed: 0.05f,
                    throttleInput: 0,
                    inReverse: false,
                    couplingMode: EngineCouplingMode.Blended,
                    couplingFactor: couplingFactor,
                    minimumCoupledRpm: 900f);

                if (engine.Rpm < minimumRpm)
                    minimumRpm = engine.Rpm;
            }

            Assert.True(
                minimumRpm >= engine.IdleRpm - 0.5f,
                $"Expected standstill automatic RPM to stay near idle. Idle={engine.IdleRpm:0.##}, min={minimumRpm:0.##}.");
        }

        [Fact]
        public void SyncFromSpeed_AutomaticLaunchBuildsRpmWithoutIdleCliff()
        {
            var engine = BuildEngine();
            engine.StartEngine();
            var minimumRpm = engine.Rpm;

            for (var i = 0; i < 18; i++)
            {
                engine.SyncFromSpeed(
                    speedGameUnits: 0f,
                    gear: 1,
                    elapsed: 0.05f,
                    throttleInput: 65,
                    inReverse: false,
                    couplingMode: EngineCouplingMode.Blended,
                    couplingFactor: 0.55f,
                    minimumCoupledRpm: 2400f);

                if (engine.Rpm < minimumRpm)
                    minimumRpm = engine.Rpm;
            }

            Assert.True(
                minimumRpm >= engine.IdleRpm - 0.5f,
                $"Expected launch RPM to avoid dropping below idle. Idle={engine.IdleRpm:0.##}, min={minimumRpm:0.##}.");
            Assert.True(engine.Rpm > engine.IdleRpm + 100f);
        }

        [Fact]
        public void SyncFromSpeed_DisengagedStandstill_HoldsIdle()
        {
            var engine = BuildEngine();
            engine.StartEngine();
            var minimumRpm = engine.Rpm;

            for (var i = 0; i < 40; i++)
            {
                engine.SyncFromSpeed(
                    speedGameUnits: 0f,
                    gear: 1,
                    elapsed: 0.05f,
                    throttleInput: 0,
                    inReverse: false,
                    couplingMode: EngineCouplingMode.Disengaged,
                    couplingFactor: 0f,
                    minimumCoupledRpm: 0f);

                if (engine.Rpm < minimumRpm)
                    minimumRpm = engine.Rpm;
            }

            Assert.True(
                minimumRpm >= engine.IdleRpm - 0.5f,
                $"Expected disengaged standstill to hold idle. Idle={engine.IdleRpm:0.##}, min={minimumRpm:0.##}.");
        }

        [Fact]
        public void SyncFromSpeed_DisengagedAfterThrottleBlip_ReturnsToIdleWithoutStallDip()
        {
            var engine = BuildEngine();
            engine.StartEngine();
            var minimumRpm = engine.Rpm;

            for (var i = 0; i < 14; i++)
            {
                engine.SyncFromSpeed(
                    speedGameUnits: 0f,
                    gear: 1,
                    elapsed: 0.05f,
                    throttleInput: 100,
                    inReverse: false,
                    couplingMode: EngineCouplingMode.Disengaged,
                    couplingFactor: 0f,
                    minimumCoupledRpm: 0f);
            }
            var peakAfterBlip = engine.Rpm;

            for (var i = 0; i < 90; i++)
            {
                engine.SyncFromSpeed(
                    speedGameUnits: 0f,
                    gear: 1,
                    elapsed: 0.05f,
                    throttleInput: 0,
                    inReverse: false,
                    couplingMode: EngineCouplingMode.Disengaged,
                    couplingFactor: 0f,
                    minimumCoupledRpm: 0f);

                if (engine.Rpm < minimumRpm)
                    minimumRpm = engine.Rpm;
            }

            Assert.True(
                minimumRpm >= engine.IdleRpm - 0.5f,
                $"Expected clutch-down rev decay to avoid sub-idle collapse. Idle={engine.IdleRpm:0.##}, min={minimumRpm:0.##}.");
            Assert.True(engine.Rpm < peakAfterBlip);
        }

        [Fact]
        public void SyncFromSpeed_AutomaticNeutralThenDrive_NoIdleDropAtEngagement()
        {
            var engine = BuildEngine();
            engine.StartEngine();
            var minimumRpm = engine.Rpm;

            for (var i = 0; i < 20; i++)
            {
                engine.SyncFromSpeed(
                    speedGameUnits: 0f,
                    gear: 1,
                    elapsed: 0.05f,
                    throttleInput: 0,
                    inReverse: false,
                    couplingMode: EngineCouplingMode.Disengaged,
                    couplingFactor: 0f,
                    minimumCoupledRpm: 0f);
                if (engine.Rpm < minimumRpm)
                    minimumRpm = engine.Rpm;
            }

            for (var i = 0; i < 24; i++)
            {
                var blend = i / 23f;
                var couplingFactor = 0.22f + (0.56f * blend);
                engine.SyncFromSpeed(
                    speedGameUnits: 0f,
                    gear: 1,
                    elapsed: 0.05f,
                    throttleInput: 0,
                    inReverse: false,
                    couplingMode: EngineCouplingMode.Blended,
                    couplingFactor: couplingFactor,
                    minimumCoupledRpm: engine.IdleRpm);

                if (engine.Rpm < minimumRpm)
                    minimumRpm = engine.Rpm;
            }

            Assert.True(
                minimumRpm >= engine.IdleRpm - 0.5f,
                $"Expected neutral->drive transition at standstill to avoid idle cliff. Idle={engine.IdleRpm:0.##}, min={minimumRpm:0.##}.");
        }

        [Fact]
        public void SyncFromSpeed_DisengagedLiftOff_DecaysFromHighRpmWithoutIdleCollapse()
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
                    couplingFactor: 0f,
                    minimumCoupledRpm: 0f);
            }
            var peakRpm = engine.Rpm;

            for (var i = 0; i < 20; i++)
            {
                engine.SyncFromSpeed(
                    speedGameUnits: 0f,
                    gear: 1,
                    elapsed: 0.05f,
                    throttleInput: 0,
                    inReverse: false,
                    couplingMode: EngineCouplingMode.Disengaged,
                    couplingFactor: 0f,
                    minimumCoupledRpm: 0f);
            }

            Assert.True(peakRpm > engine.IdleRpm + 500f);
            Assert.True(
                engine.Rpm <= peakRpm - 400f,
                $"Expected free-rev lift-off decay to be noticeable. peak={peakRpm:0.##}, after={engine.Rpm:0.##}.");
            Assert.True(
                engine.Rpm >= engine.IdleRpm - 0.5f,
                $"Expected lift-off decay to avoid sub-idle dip. idle={engine.IdleRpm:0.##}, after={engine.Rpm:0.##}.");
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

