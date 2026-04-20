using System.Reflection;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class WheelPedalBehaviorTests
{
    [Fact]
    public void WheelPedals_AutoInvert_FromRestEndpoint_ForThrottleBrakeAndClutch()
    {
        var settings = new DriveSettings { DeviceMode = InputDeviceMode.Controller };
        var input = new DriveInput(settings);

        input.Run(new InputState(), new State { Z = 100, Rz = 100, Slider1 = 100 }, 0f, controllerIsRacingWheel: true);
        input.Run(new InputState(), new State { Z = -100, Rz = -100, Slider1 = -100 }, 0f, controllerIsRacingWheel: true);

        input.Intents.GetAxisPercent(DriveIntent.Throttle).Should().Be(100);
        input.Intents.GetAxisPercent(DriveIntent.Brake).Should().Be(-100);
        input.Intents.GetAxisPercent(DriveIntent.Clutch).Should().Be(100);
    }

    [Fact]
    public void WheelPedals_RefineRestEndpoint_ToUseFullTravel()
    {
        var settings = new DriveSettings { DeviceMode = InputDeviceMode.Controller };
        var input = new DriveInput(settings);

        input.Run(new InputState(), new State { Rz = 60 }, 0f, controllerIsRacingWheel: true);
        input.Run(new InputState(), new State { Rz = 100 }, 0f, controllerIsRacingWheel: true);
        input.Run(new InputState(), new State { Rz = -100 }, 0f, controllerIsRacingWheel: true);
        input.Run(new InputState(), new State { Rz = 0 }, 0f, controllerIsRacingWheel: true);

        input.Intents.GetAxisPercent(DriveIntent.Throttle).Should().BeInRange(45, 55);
    }

    [Fact]
    public void WheelPedals_AutoInvert_UsesObservedSpan_ForPartialRange()
    {
        var settings = new DriveSettings { DeviceMode = InputDeviceMode.Controller };
        var input = new DriveInput(settings);

        input.Run(new InputState(), new State { Rz = 31 }, 0f, controllerIsRacingWheel: true);
        input.Run(new InputState(), new State { Rz = -31 }, 0f, controllerIsRacingWheel: true);

        input.Intents.GetAxisPercent(DriveIntent.Throttle).Should().Be(100);
    }

    [Theory]
    [InlineData(69, false)]
    [InlineData(70, true)]
    [InlineData(100, true)]
    public void ManualShift_UsesRelaxedClutchThreshold(int clutch, bool expected)
    {
        var method = typeof(Car).GetMethod("CanShiftManual", BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        ((bool)method!.Invoke(null, new object[] { clutch })!).Should().Be(expected);
    }
}

