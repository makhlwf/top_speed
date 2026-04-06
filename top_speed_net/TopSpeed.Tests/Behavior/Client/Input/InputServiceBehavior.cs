using System;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Vibration;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class InputServiceBehaviorTests
{
    [Fact]
    public void SetDeviceMode_DelegatesControllerEnablement()
    {
        var (service, _, controller) = InputHarness.CreateService();

        service.SetDeviceMode(InputDeviceMode.Keyboard);
        controller.Enabled.Should().BeFalse();

        service.SetDeviceMode(InputDeviceMode.Controller);
        controller.Enabled.Should().BeTrue();

        service.SetDeviceMode(InputDeviceMode.Both);
        controller.Enabled.Should().BeTrue();
    }

    [Fact]
    public void WasPressed_LatchesUntilRelease()
    {
        var (service, keyboard, _) = InputHarness.CreateService();

        keyboard.SetDown(InputKey.Return);
        service.WasPressed(InputKey.Return).Should().BeTrue();
        service.WasPressed(InputKey.Return).Should().BeFalse();

        keyboard.SetDown();
        service.WasPressed(InputKey.Return).Should().BeFalse();

        keyboard.SetDown(InputKey.Return);
        service.WasPressed(InputKey.Return).Should().BeTrue();
    }

    [Fact]
    public void ResetState_ClearsCurrentKeys()
    {
        var (service, keyboard, _) = InputHarness.CreateService();

        keyboard.SetDown(InputKey.Up);
        service.Update();
        service.IsDown(InputKey.Up).Should().BeTrue();

        service.ResetState();
        service.IsDown(InputKey.Up).Should().BeFalse();
    }

    [Fact]
    public void IsAnyMenuInputHeld_IgnoresModifierOnlyChords()
    {
        var (service, keyboard, _) = InputHarness.CreateService();

        keyboard.SetDown(InputKey.LeftShift);
        service.IsAnyMenuInputHeld().Should().BeFalse();

        keyboard.SetDown(InputKey.LeftShift, InputKey.A);
        service.IsAnyMenuInputHeld().Should().BeTrue();
    }

    [Fact]
    public void IsAnyInputHeld_UsesControllerButtonsWhenEnabled()
    {
        var (service, keyboard, controller) = InputHarness.CreateService();

        keyboard.SetDown();
        service.SetDeviceMode(InputDeviceMode.Controller);
        controller.AnyButtonHeld = true;
        service.IsAnyInputHeld().Should().BeTrue();

        controller.AnyButtonHeld = false;
        service.IsAnyInputHeld().Should().BeFalse();
    }

    [Fact]
    public void MenuBackLatch_ClearsAfterRelease()
    {
        var (service, _, controller) = InputHarness.CreateService();
        service.SetDeviceMode(InputDeviceMode.Controller);

        controller.SetState(new State { Pov4 = true }, hasState: true);
        controller.SetPollState(new State { Pov4 = true }, hasState: true);
        service.LatchMenuBack();
        service.ShouldIgnoreMenuBack().Should().BeTrue();

        controller.SetState(default, hasState: true);
        controller.SetPollState(default, hasState: true);
        service.ShouldIgnoreMenuBack().Should().BeFalse();
    }

    [Fact]
    public void NoControllerDetected_IsForwardedFromControllerBackend()
    {
        var (service, _, controller) = InputHarness.CreateService();
        var raised = 0;
        service.NoControllerDetected += () => raised++;

        controller.RaiseNoControllerDetected();

        raised.Should().Be(1);
    }

    [Fact]
    public void ControllerState_AndVibration_AreExposedThroughService()
    {
        var (service, _, controller) = InputHarness.CreateService();
        service.SetDeviceMode(InputDeviceMode.Controller);

        var expected = new State { X = 12, B1 = true };
        var vibration = new FakeVibrationDevice { IsAvailable = true, Snapshot = expected };
        controller.VibrationDevice = vibration;
        controller.SetState(expected, hasState: true);

        service.VibrationDevice.Should().BeSameAs(vibration);
        service.TryGetControllerState(out var actual).Should().BeTrue();
        actual.X.Should().Be(12);
        actual.B1.Should().BeTrue();
    }

    [Fact]
    public void PendingChoices_AndSelection_AreDelegated()
    {
        var (service, _, controller) = InputHarness.CreateService();
        service.SetDeviceMode(InputDeviceMode.Controller);

        var choice = new Choice(Guid.NewGuid(), "Wheel", true);
        controller.PendingChoices = new[] { choice };
        controller.SelectResult = true;

        service.TryGetPendingControllerChoices(out var choices).Should().BeTrue();
        choices.Should().ContainSingle();
        service.TrySelectController(choice.InstanceGuid).Should().BeTrue();
        controller.LastSelectedGuid.Should().Be(choice.InstanceGuid);
    }

    [Fact]
    public void Dispose_DisposesBackends()
    {
        var (service, keyboard, controller) = InputHarness.CreateService();

        service.Dispose();

        keyboard.DisposeCalls.Should().Be(1);
        controller.DisposeCalls.Should().Be(1);
    }

    private sealed class FakeVibrationDevice : IVibrationDevice
    {
        public bool IsAvailable { get; set; }
        public State Snapshot { get; set; }
        public State State => Snapshot;
        public bool ForceFeedbackCapable => true;

        public bool Update() => true;

        public void PlayEffect(VibrationEffectType type, int intensity = 10000)
        {
        }

        public void StopEffect(VibrationEffectType type)
        {
        }

        public void Gain(VibrationEffectType type, int value)
        {
        }

        public void Dispose()
        {
        }
    }
}
