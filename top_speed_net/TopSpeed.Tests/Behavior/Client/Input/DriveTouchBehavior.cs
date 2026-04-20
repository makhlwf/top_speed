using TopSpeed.Input;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class DriveTouchBehaviorTests
{
    [Fact]
    public void TouchAxes_ResetImmediatelyWhenTouchStateIsCleared()
    {
        var input = new DriveInput(new DriveSettings { DeviceMode = InputDeviceMode.Keyboard });

        input.SetTouchInputState(
            steering: 55,
            throttle: 70,
            brake: 0,
            clutch: 0,
            horn: false,
            gearUp: false,
            gearDown: false,
            startEngine: false);
        input.Run(new InputState(), 0f);

        input.Intents.GetAxisPercent(DriveIntent.Steering).Should().Be(55);
        input.Intents.GetAxisPercent(DriveIntent.Throttle).Should().Be(70);
        input.Intents.GetAxisPercent(DriveIntent.Brake).Should().Be(0);

        input.ClearTouchInputState();
        input.Run(new InputState(), 0f);

        input.Intents.GetAxisPercent(DriveIntent.Steering).Should().Be(0);
        input.Intents.GetAxisPercent(DriveIntent.Throttle).Should().Be(0);
        input.Intents.GetAxisPercent(DriveIntent.Brake).Should().Be(0);
    }

    [Fact]
    public void TouchCommands_FollowPerFrameStateWithoutCooldown()
    {
        var input = new DriveInput(new DriveSettings { DeviceMode = InputDeviceMode.Keyboard });

        input.SetTouchInputState(
            steering: 0,
            throttle: 0,
            brake: 0,
            clutch: 0,
            horn: true,
            gearUp: true,
            gearDown: false,
            startEngine: true);
        input.Run(new InputState(), 0f);

        input.Intents.IsTriggered(DriveIntent.Horn).Should().BeTrue();
        input.Intents.IsTriggered(DriveIntent.GearUp).Should().BeTrue();
        input.Intents.IsTriggered(DriveIntent.StartEngine).Should().BeTrue();

        input.SetTouchInputState(
            steering: 0,
            throttle: 0,
            brake: 0,
            clutch: 0,
            horn: false,
            gearUp: false,
            gearDown: false,
            startEngine: false);
        input.Run(new InputState(), 0f);

        input.Intents.IsTriggered(DriveIntent.Horn).Should().BeFalse();
        input.Intents.IsTriggered(DriveIntent.GearUp).Should().BeFalse();
        input.Intents.IsTriggered(DriveIntent.StartEngine).Should().BeFalse();
    }

    [Fact]
    public void TouchClutch_MapsToClutchAxisAndIntent()
    {
        var input = new DriveInput(new DriveSettings { DeviceMode = InputDeviceMode.Keyboard });

        input.SetTouchInputState(
            steering: 0,
            throttle: 0,
            brake: 0,
            clutch: 100,
            horn: false,
            gearUp: false,
            gearDown: false,
            startEngine: false);
        input.Run(new InputState(), 0f);

        input.Intents.GetAxisPercent(DriveIntent.Clutch).Should().Be(100);
        input.Intents.IsTriggered(DriveIntent.Clutch).Should().BeTrue();
    }
}
