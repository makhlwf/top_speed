using System;
using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Keyboard;
using TopSpeed.Input.Devices.Vibration;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class InputServiceTests
    {
        [Fact]
        public void SetDeviceMode_DelegatesControllerEnablement()
        {
            var (service, _, controller) = CreateService();

            service.SetDeviceMode(InputDeviceMode.Keyboard);
            Assert.False(controller.Enabled);

            service.SetDeviceMode(InputDeviceMode.Controller);
            Assert.True(controller.Enabled);

            service.SetDeviceMode(InputDeviceMode.Both);
            Assert.True(controller.Enabled);
        }

        [Fact]
        public void WasPressed_LatchesUntilRelease()
        {
            var (service, keyboard, _) = CreateService();

            keyboard.SetDown(InputKey.Return);
            Assert.True(service.WasPressed(InputKey.Return));
            Assert.False(service.WasPressed(InputKey.Return));

            keyboard.SetDown();
            Assert.False(service.WasPressed(InputKey.Return));

            keyboard.SetDown(InputKey.Return);
            Assert.True(service.WasPressed(InputKey.Return));
        }

        [Fact]
        public void ResetState_ClearsCurrentKeys()
        {
            var (service, keyboard, _) = CreateService();

            keyboard.SetDown(InputKey.Up);
            service.Update();
            Assert.True(service.IsDown(InputKey.Up));

            service.ResetState();
            Assert.False(service.IsDown(InputKey.Up));
        }

        [Fact]
        public void IsAnyMenuInputHeld_IgnoresModifierOnlyChords()
        {
            var (service, keyboard, _) = CreateService();

            keyboard.SetDown(InputKey.LeftShift);
            Assert.False(service.IsAnyMenuInputHeld());

            keyboard.SetDown(InputKey.LeftShift, InputKey.A);
            Assert.True(service.IsAnyMenuInputHeld());
        }

        [Fact]
        public void IsAnyInputHeld_UsesControllerButtonsWhenEnabled()
        {
            var (service, keyboard, controller) = CreateService();

            keyboard.SetDown();
            service.SetDeviceMode(InputDeviceMode.Controller);
            controller.AnyButtonHeld = true;
            Assert.True(service.IsAnyInputHeld());

            controller.AnyButtonHeld = false;
            Assert.False(service.IsAnyInputHeld());
        }

        [Fact]
        public void MenuBackLatch_ClearsAfterRelease()
        {
            var (service, _, controller) = CreateService();
            service.SetDeviceMode(InputDeviceMode.Controller);

            controller.SetState(new State { Pov4 = true }, hasState: true);
            controller.SetPollState(new State { Pov4 = true }, hasState: true);
            service.LatchMenuBack();
            Assert.True(service.ShouldIgnoreMenuBack());

            controller.SetState(default, hasState: true);
            controller.SetPollState(default, hasState: true);
            Assert.False(service.ShouldIgnoreMenuBack());
        }

        [Fact]
        public void ScanTimeout_IsForwardedFromControllerBackend()
        {
            var (service, _, controller) = CreateService();
            var raised = 0;
            service.ControllerScanTimedOut += () => raised++;

            controller.RaiseScanTimedOut();

            Assert.Equal(1, raised);
        }

        [Fact]
        public void ControllerState_AndVibration_AreExposedThroughService()
        {
            var (service, _, controller) = CreateService();
            service.SetDeviceMode(InputDeviceMode.Controller);

            var expected = new State { X = 12, B1 = true };
            var vibration = new FakeVibrationDevice { IsAvailable = true, Snapshot = expected };
            controller.VibrationDevice = vibration;
            controller.SetState(expected, hasState: true);

            Assert.Same(vibration, service.VibrationDevice);
            Assert.True(service.TryGetControllerState(out var actual));
            Assert.Equal(12, actual.X);
            Assert.True(actual.B1);
        }

        [Fact]
        public void PendingChoices_AndSelection_AreDelegated()
        {
            var (service, _, controller) = CreateService();
            service.SetDeviceMode(InputDeviceMode.Controller);

            var choice = new Choice(Guid.NewGuid(), "Wheel", true);
            controller.PendingChoices = new[] { choice };
            controller.SelectResult = true;

            Assert.True(service.TryGetPendingControllerChoices(out var choices));
            Assert.Single(choices);
            Assert.True(service.TrySelectController(choice.InstanceGuid));
            Assert.Equal(choice.InstanceGuid, controller.LastSelectedGuid);
        }

        [Fact]
        public void Dispose_DisposesBackends()
        {
            var (service, keyboard, controller) = CreateService();

            service.Dispose();

            Assert.Equal(1, keyboard.DisposeCalls);
            Assert.Equal(1, controller.DisposeCalls);
        }

        private static (InputService Service, FakeKeyboardDevice Keyboard, FakeControllerBackend Controller) CreateService()
        {
            var keyboard = new FakeKeyboardDevice();
            var controller = new FakeControllerBackend();
            var service = new InputService(keyboard, controller);
            return (service, keyboard, controller);
        }

        private sealed class FakeKeyboardDevice : IKeyboardDevice
        {
            private readonly HashSet<InputKey> _down = new HashSet<InputKey>();
            public bool PopulateResult { get; set; } = true;
            public int DisposeCalls { get; private set; }

            public void SetDown(params InputKey[] keys)
            {
                _down.Clear();
                for (var i = 0; i < keys.Length; i++)
                    _down.Add(keys[i]);
            }

            public bool TryPopulateState(InputState state)
            {
                if (!PopulateResult)
                    return false;

                foreach (var key in _down)
                    state.Set(key, true);
                return true;
            }

            public bool IsDown(InputKey key) => _down.Contains(key);

            public bool IsAnyKeyHeld(bool ignoreModifiers)
            {
                if (!ignoreModifiers)
                    return _down.Count > 0;

                foreach (var key in _down)
                {
                    if (key == InputKey.LeftControl || key == InputKey.RightControl ||
                        key == InputKey.LeftShift || key == InputKey.RightShift ||
                        key == InputKey.LeftAlt || key == InputKey.RightAlt)
                    {
                        continue;
                    }

                    return true;
                }

                return false;
            }

            public void ResetHeldState()
            {
                _down.Clear();
            }

            public void Suspend()
            {
            }

            public void Resume()
            {
            }

            public void Dispose()
            {
                DisposeCalls++;
            }
        }

        private sealed class FakeControllerBackend : IControllerBackend
        {
            public event Action? ScanTimedOut;

            public bool Enabled { get; private set; }
            public bool ActiveControllerIsRacingWheel { get; set; }
            public bool IgnoreAxesForMenuNavigation { get; set; }
            public IVibrationDevice? VibrationDevice { get; set; }
            public bool AnyButtonHeld { get; set; }
            public IReadOnlyList<Choice>? PendingChoices { get; set; }
            public bool SelectResult { get; set; }
            public Guid LastSelectedGuid { get; private set; }
            public int DisposeCalls { get; private set; }

            private State _state;
            private bool _hasState;
            private State _pollState;
            private bool _hasPollState;

            public void SetEnabled(bool enabled)
            {
                Enabled = enabled;
            }

            public void Update()
            {
            }

            public bool TryGetState(out State state)
            {
                if (!Enabled || !_hasState)
                {
                    state = default;
                    return false;
                }

                state = _state;
                return true;
            }

            public bool TryPollState(out State state)
            {
                if (!Enabled || !_hasPollState)
                {
                    state = default;
                    return false;
                }

                state = _pollState;
                return true;
            }

            public bool IsAnyButtonHeld()
            {
                return Enabled && AnyButtonHeld;
            }

            public bool TryGetPendingChoices(out IReadOnlyList<Choice> choices)
            {
                if (PendingChoices == null || PendingChoices.Count == 0)
                {
                    choices = Array.Empty<Choice>();
                    return false;
                }

                choices = PendingChoices;
                return true;
            }

            public bool TrySelect(Guid instanceGuid)
            {
                LastSelectedGuid = instanceGuid;
                return Enabled && SelectResult && instanceGuid != Guid.Empty;
            }

            public void Suspend()
            {
            }

            public void Resume()
            {
            }

            public void Dispose()
            {
                DisposeCalls++;
            }

            public void SetState(State state, bool hasState)
            {
                _state = state;
                _hasState = hasState;
            }

            public void SetPollState(State state, bool hasState)
            {
                _pollState = state;
                _hasPollState = hasState;
            }

            public void RaiseScanTimedOut()
            {
                ScanTimedOut?.Invoke();
            }
        }

        private sealed class FakeVibrationDevice : IVibrationDevice
        {
            public bool IsAvailable { get; set; }
            public State Snapshot { get; set; }
            public bool ForceFeedbackCapable => true;
            public State State => Snapshot;

            public bool Update() => IsAvailable;

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
}

