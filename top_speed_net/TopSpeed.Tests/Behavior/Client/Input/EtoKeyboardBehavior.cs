using TopSpeed.Input;
using TopSpeed.Input.Devices.Keyboard.Backends.Eto;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class EtoKeyboardBehaviorTests
{
    [Fact]
    public void KeyDownAndUp_UpdatesState()
    {
        var source = new InputHarness.FakeKeyboardEventSource();
        using var device = new Device(source);
        var state = new InputState();

        source.RaiseKeyDown(InputKey.Up);
        device.TryPopulateState(state).Should().BeTrue();
        state.IsDown(InputKey.Up).Should().BeTrue();
        device.IsDown(InputKey.Up).Should().BeTrue();

        source.RaiseKeyUp(InputKey.Up);
        state.Clear();
        device.TryPopulateState(state).Should().BeTrue();
        state.IsDown(InputKey.Up).Should().BeFalse();
        device.IsDown(InputKey.Up).Should().BeFalse();
    }

    [Fact]
    public void IsAnyKeyHeld_IgnoresOnlyModifiersWhenRequested()
    {
        var source = new InputHarness.FakeKeyboardEventSource();
        using var device = new Device(source);

        source.RaiseKeyDown(InputKey.LeftShift);
        device.IsAnyKeyHeld(ignoreModifiers: false).Should().BeTrue();
        device.IsAnyKeyHeld(ignoreModifiers: true).Should().BeFalse();

        source.RaiseKeyDown(InputKey.A);
        device.IsAnyKeyHeld(ignoreModifiers: false).Should().BeTrue();
        device.IsAnyKeyHeld(ignoreModifiers: true).Should().BeTrue();
    }

    [Fact]
    public void Suspend_ClearsAndBlocksStateUntilResume()
    {
        var source = new InputHarness.FakeKeyboardEventSource();
        using var device = new Device(source);
        var state = new InputState();

        source.RaiseKeyDown(InputKey.Space);
        device.Suspend();
        state.Clear();
        device.TryPopulateState(state).Should().BeTrue();
        state.IsDown(InputKey.Space).Should().BeFalse();

        source.RaiseKeyDown(InputKey.Space);
        state.Clear();
        device.TryPopulateState(state).Should().BeTrue();
        state.IsDown(InputKey.Space).Should().BeFalse();

        device.Resume();
        source.RaiseKeyDown(InputKey.Space);
        state.Clear();
        device.TryPopulateState(state).Should().BeTrue();
        state.IsDown(InputKey.Space).Should().BeTrue();
    }
}
