using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class GearSpeedLimiterTests
    {
        [Fact]
        public void ApplyForwardGearLimit_OverspeedDownshift_DoesNotSnapToGearMax()
        {
            var limited = GearSpeedLimiter.ApplyForwardGearLimit(
                speedBeforeKph: 120f,
                speedAfterKph: 118.5f,
                gearMaxKph: 52f);

            Assert.Equal(118.5f, limited, 3);
        }

        [Fact]
        public void ApplyForwardGearLimit_OverspeedDownshift_BlocksFurtherAcceleration()
        {
            var limited = GearSpeedLimiter.ApplyForwardGearLimit(
                speedBeforeKph: 120f,
                speedAfterKph: 121.2f,
                gearMaxKph: 52f);

            Assert.Equal(120f, limited, 3);
        }

        [Fact]
        public void ApplyForwardGearLimit_CrossesGearMaxFromBelow_ClampsAtGearMax()
        {
            var limited = GearSpeedLimiter.ApplyForwardGearLimit(
                speedBeforeKph: 49f,
                speedAfterKph: 52f,
                gearMaxKph: 50f);

            Assert.Equal(50f, limited, 3);
        }

        [Fact]
        public void ApplyForwardGearLimit_UnderGearMax_PassesThrough()
        {
            var limited = GearSpeedLimiter.ApplyForwardGearLimit(
                speedBeforeKph: 40f,
                speedAfterKph: 41.3f,
                gearMaxKph: 50f);

            Assert.Equal(41.3f, limited, 3);
        }

        [Fact]
        public void ShouldForceOverspeedCoast_ManualCoupledForwardOverspeed_ReturnsTrue()
        {
            var forceCoast = GearSpeedLimiter.ShouldForceOverspeedCoast(
                speedKph: 147f,
                gearMaxKph: 52f,
                drivelineCouplingFactor: 1f,
                forwardDriveGearActive: true,
                manualShiftControlActive: true);

            Assert.True(forceCoast);
        }

        [Fact]
        public void ShouldForceOverspeedCoast_ClutchDisengaged_ReturnsFalse()
        {
            var forceCoast = GearSpeedLimiter.ShouldForceOverspeedCoast(
                speedKph: 147f,
                gearMaxKph: 52f,
                drivelineCouplingFactor: 0f,
                forwardDriveGearActive: true,
                manualShiftControlActive: true);

            Assert.False(forceCoast);
        }

        [Fact]
        public void ShouldForceOverspeedCoast_AutomaticNoManualControl_ReturnsFalse()
        {
            var forceCoast = GearSpeedLimiter.ShouldForceOverspeedCoast(
                speedKph: 147f,
                gearMaxKph: 52f,
                drivelineCouplingFactor: 1f,
                forwardDriveGearActive: true,
                manualShiftControlActive: false);

            Assert.False(forceCoast);
        }
    }
}

