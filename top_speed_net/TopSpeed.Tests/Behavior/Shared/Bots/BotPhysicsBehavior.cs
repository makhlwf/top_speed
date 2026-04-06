using System.Collections.Generic;
using System.Linq;
using TopSpeed.Bots;
using TopSpeed.Protocol;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class BotPhysicsBehaviorTests
{
    public static IEnumerable<object[]> OfficialCars()
    {
        return BotPhysicsHarness.OfficialCars().Select(carType => new object[] { carType });
    }

    [Fact]
    public void CustomVehicle_ShouldReuseVehicle1BotConfig()
    {
        var expected = BotPhysicsCatalog.Get(CarType.Vehicle1);
        var actual = BotPhysicsCatalog.Get(CarType.CustomVehicle);

        actual.Gears.Should().Be(expected.Gears);
        actual.TopSpeedKph.Should().Be(expected.TopSpeedKph);
        actual.GearRatios.Should().Equal(expected.GearRatios);
    }

    [Theory]
    [MemberData(nameof(OfficialCars))]
    public void OfficialConfigs_ShouldExposeUsableWheelAndGearData(CarType carType)
    {
        var config = BotPhysicsCatalog.Get(carType);

        config.Gears.Should().BeGreaterThan(0);
        config.GearRatios.Should().HaveCount(config.Gears);
        config.GearRatios.Should().BeInDescendingOrder();
        config.WheelRadiusM.Should().BeGreaterThan(0.01f);
        config.TopSpeedKph.Should().BeGreaterThan(1f);
    }

    [Theory]
    [MemberData(nameof(OfficialCars))]
    public void LaunchSimulation_ShouldMoveForward_AndStayWithinValidGearRange(CarType carType)
    {
        var config = BotPhysicsCatalog.Get(carType);
        var trace = BotPhysicsHarness.SimulateLaunch(carType);

        trace.FinalSpeedKph.Should().BeGreaterThan(0f);
        trace.FinalPositionY.Should().BeGreaterThan(0f);
        trace.FinalGear.Should().BeInRange(1, config.Gears);
        trace.Samples.Should().OnlyContain(sample => sample.Gear >= 1 && sample.Gear <= config.Gears);
    }
}
