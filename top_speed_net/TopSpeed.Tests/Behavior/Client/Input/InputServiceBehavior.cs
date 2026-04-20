using System;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Vibration;
using TopSpeed.Runtime;
using TS.Sdl.Events;
using TS.Sdl.Input;
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
    public void WasGesturePressed_ConsumesMappedGestureOnce()
    {
        var (service, _, _) = InputHarness.CreateService();

        service.SubmitGesture(new GestureEvent { Kind = GestureKind.Swipe, Direction = SwipeDirection.Right });

        service.WasGesturePressed(GestureIntent.SwipeRight).Should().BeTrue();
        service.WasGesturePressed(GestureIntent.SwipeRight).Should().BeFalse();
    }

    [Fact]
    public void WasGesturePressed_MapsTwoFingerSwipeSeparatelyFromSingleFinger()
    {
        var (service, _, _) = InputHarness.CreateService();

        service.SubmitGesture(new GestureEvent
        {
            Kind = GestureKind.Swipe,
            FingerCount = 2,
            Direction = SwipeDirection.Left
        });

        service.WasGesturePressed(GestureIntent.SwipeLeft).Should().BeFalse();
        service.WasGesturePressed(GestureIntent.TwoFingerSwipeLeft).Should().BeTrue();
        service.WasGesturePressed(GestureIntent.TwoFingerSwipeLeft).Should().BeFalse();
    }

    [Fact]
    public void SubmitGesture_IgnoresUnsupportedGestureKinds()
    {
        var (service, _, _) = InputHarness.CreateService();

        service.SubmitGesture(new GestureEvent { Kind = GestureKind.PinchUpdate });

        service.WasGesturePressed(GestureIntent.Tap).Should().BeFalse();
        service.WasGesturePressed(GestureIntent.SwipeLeft).Should().BeFalse();
    }

    [Fact]
    public void WasZoneGesturePressed_ConsumesMappedZoneGestureOnce()
    {
        var (service, _, _) = InputHarness.CreateService();
        service.SubmitTouchZoneGesture(new TouchZoneGestureEvent(
            new GestureEvent
            {
                Kind = GestureKind.Swipe,
                FingerCount = 2,
                Direction = SwipeDirection.Up
            },
            new TouchZoneHit("drive_bottom", priority: 0, assigned: true)));

        service.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeUp, "drive_bottom").Should().BeTrue();
        service.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeUp, "drive_bottom").Should().BeFalse();
        service.WasGesturePressed(GestureIntent.TwoFingerSwipeUp).Should().BeFalse();
    }

    [Fact]
    public void SubmitTouchZoneGesture_IgnoresUnassignedZone()
    {
        var (service, _, _) = InputHarness.CreateService();
        service.SubmitTouchZoneGesture(new TouchZoneGestureEvent(
            new GestureEvent
            {
                Kind = GestureKind.DoubleTap
            },
            TouchZoneHit.None));

        service.WasZoneGesturePressed(GestureIntent.DoubleTap, "info_top").Should().BeFalse();
    }

    [Fact]
    public void SetTouchZones_ForServiceWithoutZoneSource_IsNoOp()
    {
        var (service, _, _) = InputHarness.CreateService();
        var zones = TouchZoneLayout.Horizontal("top", "bottom");

        var act = () => service.SetTouchZones(zones);
        var clear = () => service.ClearTouchZones();

        act.Should().NotThrow();
        clear.Should().NotThrow();
    }

    [Fact]
    public void ResetState_ClearsZoneGestureLatch()
    {
        var (service, _, _) = InputHarness.CreateService();
        service.SubmitTouchZoneGesture(new TouchZoneGestureEvent(
            new GestureEvent
            {
                Kind = GestureKind.Swipe,
                Direction = SwipeDirection.Down
            },
            new TouchZoneHit("menu_top", priority: 0, assigned: true)));

        service.ResetState();

        service.WasZoneGesturePressed(GestureIntent.SwipeDown, "menu_top").Should().BeFalse();
    }

    [Fact]
    public void TryGetTouchZoneState_TracksPrimaryTouchAndFingerCount()
    {
        using var fixture = CreateTouchServiceFixture();
        fixture.Source.RaiseTouch(new TouchZoneTouchEvent(
            EventType.FingerDown,
            new TouchFingerEvent
            {
                Timestamp = 1000,
                TouchId = 1,
                FingerId = 10,
                X = 0.40f,
                Y = 0.80f,
                Pressure = 1f
            },
            new TouchZoneHit("drive_vehicle", priority: 0, assigned: true)));
        fixture.Source.RaiseTouch(new TouchZoneTouchEvent(
            EventType.FingerDown,
            new TouchFingerEvent
            {
                Timestamp = 1100,
                TouchId = 1,
                FingerId = 11,
                X = 0.60f,
                Y = 0.82f,
                Pressure = 1f
            },
            new TouchZoneHit("drive_vehicle", priority: 0, assigned: true)));
        fixture.Source.RaiseTouch(new TouchZoneTouchEvent(
            EventType.FingerMotion,
            new TouchFingerEvent
            {
                Timestamp = 1200,
                TouchId = 1,
                FingerId = 10,
                X = 0.45f,
                Y = 0.74f,
                Pressure = 1f
            },
            new TouchZoneHit("drive_vehicle", priority: 0, assigned: true)));

        fixture.Service.TryGetTouchZoneState("drive_vehicle", out var state).Should().BeTrue();
        state.IsActive.Should().BeTrue();
        state.FingerCount.Should().Be(2);
        state.StartX.Should().BeApproximately(0.40f, 0.0001f);
        state.StartY.Should().BeApproximately(0.80f, 0.0001f);
        state.X.Should().BeApproximately(0.45f, 0.0001f);
        state.Y.Should().BeApproximately(0.74f, 0.0001f);
    }

    [Fact]
    public void TryGetTouchZoneState_ClearsWhenTouchEndsOrZonesAreCleared()
    {
        using var fixture = CreateTouchServiceFixture();
        fixture.Source.RaiseTouch(new TouchZoneTouchEvent(
            EventType.FingerDown,
            new TouchFingerEvent
            {
                Timestamp = 1000,
                TouchId = 2,
                FingerId = 1,
                X = 0.50f,
                Y = 0.88f,
                Pressure = 1f
            },
            new TouchZoneHit("drive_vehicle", priority: 0, assigned: true)));

        fixture.Service.TryGetTouchZoneState("drive_vehicle", out _).Should().BeTrue();

        fixture.Source.RaiseTouch(new TouchZoneTouchEvent(
            EventType.FingerUp,
            new TouchFingerEvent
            {
                Timestamp = 1200,
                TouchId = 2,
                FingerId = 1,
                X = 0.50f,
                Y = 0.88f,
                Pressure = 0f
            },
            new TouchZoneHit("drive_vehicle", priority: 0, assigned: true)));
        fixture.Service.TryGetTouchZoneState("drive_vehicle", out _).Should().BeFalse();

        fixture.Source.RaiseTouch(new TouchZoneTouchEvent(
            EventType.FingerDown,
            new TouchFingerEvent
            {
                Timestamp = 1300,
                TouchId = 2,
                FingerId = 1,
                X = 0.55f,
                Y = 0.84f,
                Pressure = 1f
            },
            new TouchZoneHit("drive_vehicle", priority: 0, assigned: true)));
        fixture.Service.TryGetTouchZoneState("drive_vehicle", out _).Should().BeTrue();

        fixture.Service.ClearTouchZones();
        fixture.Service.TryGetTouchZoneState("drive_vehicle", out _).Should().BeFalse();
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
    public void Constructor_FallsBackToDisabledControllerBackend_WhenControllerCreationFails()
    {
        var keyboard = new InputHarness.FakeKeyboardDevice();
        var registry = new BackendRegistry(
            new IKeyboardBackendFactory[]
            {
                new InputHarness.FakeKeyboardFactory("keyboard", 1, true, keyboard)
            },
            new IControllerBackendFactory[]
            {
                new InputHarness.FakeControllerFactory("sdl", 1, supported: true, created: null, exception: new InvalidOperationException("sdl: unsupported"))
            });

        using var service = new InputService(IntPtr.Zero, registry, keyboardEventSource: null);
        string? reason = null;
        service.ControllerBackendUnavailable += value => reason = value;

        service.SetDeviceMode(InputDeviceMode.Controller);

        reason.Should().Contain("sdl");
        service.TryGetControllerState(out _).Should().BeFalse();
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

    private static TouchServiceFixture CreateTouchServiceFixture()
    {
        var keyboard = new InputHarness.FakeKeyboardDevice();
        var controller = new InputHarness.FakeControllerBackend();
        var source = new FakeTouchEventSource();
        var registry = new BackendRegistry(
            new IKeyboardBackendFactory[]
            {
                new InputHarness.FakeKeyboardFactory("keyboard", 1, true, keyboard)
            },
            new IControllerBackendFactory[]
            {
                new InputHarness.FakeControllerFactory("controller", 1, true, controller)
            });

        var service = new InputService(IntPtr.Zero, registry, keyboardEventSource: null, source);
        return new TouchServiceFixture(service, source);
    }

    private sealed class TouchServiceFixture : IDisposable
    {
        public TouchServiceFixture(InputService service, FakeTouchEventSource source)
        {
            Service = service;
            Source = source;
        }

        public InputService Service { get; }
        public FakeTouchEventSource Source { get; }

        public void Dispose()
        {
            Service.Dispose();
        }
    }

    private sealed class FakeTouchEventSource : IGestureEventSource, ITouchZoneGestureEventSource, ITouchZoneTouchEventSource
    {
        public event Action<GestureEvent>? GestureRaised;
        public event Action<TouchZoneGestureEvent>? TouchZoneGestureRaised;
        public event Action<TouchZoneTouchEvent>? TouchZoneTouchRaised;

        public void SetTouchZones(System.Collections.Generic.IReadOnlyList<TouchZone> zones)
        {
        }

        public void ClearTouchZones()
        {
        }

        public void RaiseGesture(GestureEvent value)
        {
            GestureRaised?.Invoke(value);
        }

        public void RaiseZoneGesture(TouchZoneGestureEvent value)
        {
            TouchZoneGestureRaised?.Invoke(value);
        }

        public void RaiseTouch(TouchZoneTouchEvent value)
        {
            TouchZoneTouchRaised?.Invoke(value);
        }
    }
}
