using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class GearSpeedLimiterBehaviorTests
{
    [Fact]
    public void ApplyForwardGearLimit_OverspeedDownshift_DoesNotSnapToGearMax()
    {
        var limited = GearSpeedLimiter.ApplyForwardGearLimit(120f, 118.5f, 52f);

        limited.Should().BeApproximately(118.5f, 0.001f);
    }

    [Fact]
    public void ApplyForwardGearLimit_OverspeedDownshift_BlocksFurtherAcceleration()
    {
        var limited = GearSpeedLimiter.ApplyForwardGearLimit(120f, 121.2f, 52f);

        limited.Should().BeApproximately(120f, 0.001f);
    }

    [Fact]
    public void ApplyForwardGearLimit_CrossesGearMaxFromBelow_ClampsAtGearMax()
    {
        var limited = GearSpeedLimiter.ApplyForwardGearLimit(49f, 52f, 50f);

        limited.Should().BeApproximately(50f, 0.001f);
    }

    [Fact]
    public void ApplyForwardGearLimit_UnderGearMax_PassesThrough()
    {
        var limited = GearSpeedLimiter.ApplyForwardGearLimit(40f, 41.3f, 50f);

        limited.Should().BeApproximately(41.3f, 0.001f);
    }

    [Fact]
    public void ShouldForceOverspeedCoast_ManualCoupledForwardOverspeed_ReturnsTrue()
    {
        GearSpeedLimiter.ShouldForceOverspeedCoast(147f, 52f, 1f, forwardDriveGearActive: true, manualShiftControlActive: true)
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldForceOverspeedCoast_ClutchDisengaged_ReturnsFalse()
    {
        GearSpeedLimiter.ShouldForceOverspeedCoast(147f, 52f, 0f, forwardDriveGearActive: true, manualShiftControlActive: true)
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldForceOverspeedCoast_AutomaticNoManualControl_ReturnsFalse()
    {
        GearSpeedLimiter.ShouldForceOverspeedCoast(147f, 52f, 1f, forwardDriveGearActive: true, manualShiftControlActive: false)
            .Should().BeFalse();
    }
}
