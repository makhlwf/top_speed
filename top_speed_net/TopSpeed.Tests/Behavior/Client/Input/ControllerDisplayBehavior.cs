using System;
using TopSpeed.Input;
using TopSpeed.Input.Backends.Sdl;
using TS.Sdl.Input;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class ControllerDisplayBehaviorTests
{
    [Fact]
    public void FormatAxis_UsesXboxLabels_ForXboxGamepadProfile()
    {
        var profile = new ControllerDisplayProfile(ControllerDeviceType.Gamepad, ControllerGamepadFamily.Xbox);

        InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button1, profile).Should().Be("A");
        InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button7, profile).Should().Be("View");
        InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.AxisZPos, profile).Should().Be("Left trigger");
    }

    [Fact]
    public void FormatAxis_UsesPlayStationLabels_ForPlayStationGamepadProfile()
    {
        var profile = new ControllerDisplayProfile(ControllerDeviceType.Gamepad, ControllerGamepadFamily.PlayStation);

        InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button1, profile).Should().Be("Cross");
        InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button2, profile).Should().Be("Circle");
        InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button11, profile).Should().Be("PS button");
    }

    [Fact]
    public void FormatAxis_UsesNintendoLabels_ForNintendoGamepadProfile()
    {
        var profile = new ControllerDisplayProfile(ControllerDeviceType.Gamepad, ControllerGamepadFamily.Nintendo);

        InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button1, profile).Should().Be("B");
        InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button2, profile).Should().Be("A");
        InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button8, profile).Should().Be("Plus");
    }

    [Fact]
    public void FormatAxis_UsesSemanticLabels_ForNeutralGamepadProfile()
    {
        InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button1, ControllerDisplayProfile.SemanticGamepad).Should().Be("South");
        InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button8, ControllerDisplayProfile.SemanticGamepad).Should().Be("Start");
    }

    [Fact]
    public void FormatAxis_UsesGenericLabels_ForJoystickProfile()
    {
        InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Button1, ControllerDisplayProfile.Joystick).Should().Be("Button 1");
        InputDisplayText.Axis(TopSpeed.Input.Devices.Controller.AxisOrButton.Pov1, ControllerDisplayProfile.Joystick).Should().Be("POV 1 up");
    }

    [Fact]
    public void BuildChoiceLabel_IsAlwaysDetailed()
    {
        var metadata = CreateMetadata(
            42,
            isGamepad: false,
            name: "Wheel Pro",
            joystickType: JoystickType.Wheel,
            vendorId: 0x046D,
            productId: 0xC29B);

        Display.BuildChoiceLabel(metadata, isRacingWheel: true).Should().Be("Wheel Pro (Racing wheel, 046D:C29B)");
    }

    [Fact]
    public void CreateProfile_UsesNameHeuristics_ForUnknownGamepadTypes()
    {
        var metadata = CreateMetadata(
            7,
            isGamepad: true,
            name: "DualSense Wireless Controller",
            joystickType: JoystickType.Gamepad,
            gamepadType: GamepadType.Unknown);

        var profile = Display.CreateProfile(metadata, isRacingWheel: false);

        profile.DeviceType.Should().Be(ControllerDeviceType.Gamepad);
        profile.GamepadFamily.Should().Be(ControllerGamepadFamily.PlayStation);
    }

    private static DeviceMetadata CreateMetadata(
        uint instanceId,
        bool isGamepad,
        string name,
        JoystickType joystickType,
        GamepadType gamepadType = GamepadType.Unknown,
        ushort vendorId = 0,
        ushort productId = 0)
    {
        return new DeviceMetadata(
            instanceId,
            isGamepad,
            name,
            path: string.Empty,
            guid: Guid.Empty,
            joystickType,
            gamepadType,
            playerIndex: -1,
            vendorId,
            productId,
            productVersion: 0,
            firmwareVersion: 0,
            serial: string.Empty);
    }
}
