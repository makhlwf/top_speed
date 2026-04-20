using System;
using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Keyboard;
using TopSpeed.Input.Devices.Vibration;
using TopSpeed.Runtime;

namespace TopSpeed.Tests;

internal static class InputHarness
{
    public static (InputService Service, FakeKeyboardDevice Keyboard, FakeControllerBackend Controller) CreateService()
    {
        var keyboard = new FakeKeyboardDevice();
        var controller = new FakeControllerBackend();
        var service = new InputService(keyboard, controller);
        return (service, keyboard, controller);
    }

    public sealed class FakeKeyboardEventSource : IKeyboardEventSource
    {
        public event Action<InputKey>? KeyDown;
        public event Action<InputKey>? KeyUp;

        public void RaiseKeyDown(InputKey key) => KeyDown?.Invoke(key);

        public void RaiseKeyUp(InputKey key) => KeyUp?.Invoke(key);
    }

    public sealed class FakeKeyboardDevice : IKeyboardDevice
    {
        private readonly HashSet<InputKey> _down = new();

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

        public void ResetHeldState() => _down.Clear();

        public void Suspend()
        {
        }

        public void Resume()
        {
        }

        public void Dispose() => DisposeCalls++;
    }

    public sealed class FakeControllerBackend : IControllerBackend
    {
        public event Action? NoControllerDetected;

        public bool Enabled { get; private set; }
        public bool ActiveControllerIsRacingWheel { get; set; }
        public bool IgnoreAxesForMenuNavigation { get; set; }
        public IVibrationDevice? VibrationDevice { get; set; }
        public ControllerDisplayProfile DisplayProfile { get; set; }
        public bool HasDisplayProfile { get; set; }
        public bool AnyButtonHeld { get; set; }
        public IReadOnlyList<Choice>? PendingChoices { get; set; }
        public bool SelectResult { get; set; }
        public Guid LastSelectedGuid { get; private set; }
        public int DisposeCalls { get; private set; }

        private State _state;
        private bool _hasState;
        private State _pollState;
        private bool _hasPollState;

        public void SetEnabled(bool enabled) => Enabled = enabled;

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

        public bool IsAnyButtonHeld() => Enabled && AnyButtonHeld;

        public bool TryGetPendingChoices(out IReadOnlyList<Choice> choices)
        {
            if (!Enabled || PendingChoices == null || PendingChoices.Count == 0)
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
            return SelectResult;
        }

        public bool TryGetDisplayProfile(out ControllerDisplayProfile profile)
        {
            if (!HasDisplayProfile)
            {
                profile = default;
                return false;
            }

            profile = DisplayProfile;
            return true;
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

        public void RaiseNoControllerDetected() => NoControllerDetected?.Invoke();

        public void Suspend()
        {
        }

        public void Resume()
        {
        }

        public void Dispose() => DisposeCalls++;
    }

    public sealed class FakeKeyboardFactory : IKeyboardBackendFactory
    {
        private readonly bool _supported;
        private readonly IKeyboardDevice? _created;
        private readonly Exception? _exception;

        public FakeKeyboardFactory(string id, int priority, bool supported, IKeyboardDevice? created, Exception? exception = null)
        {
            Id = id;
            Priority = priority;
            _supported = supported;
            _created = created;
            _exception = exception;
        }

        public string Id { get; }
        public int Priority { get; }

        public bool IsSupported() => _supported;

        public IKeyboardDevice Create(IntPtr windowHandle, IKeyboardEventSource? eventSource)
        {
            if (_exception != null)
                throw _exception;

            return _created!;
        }
    }

    public sealed class FakeControllerFactory : IControllerBackendFactory, IBackendSupportDiagnostics
    {
        private readonly bool _supported;
        private readonly IControllerBackend? _created;
        private readonly Exception? _exception;
        private readonly string? _unsupportedReason;

        public FakeControllerFactory(string id, int priority, bool supported, IControllerBackend? created, Exception? exception = null, string? unsupportedReason = null)
        {
            Id = id;
            Priority = priority;
            _supported = supported;
            _created = created;
            _exception = exception;
            _unsupportedReason = unsupportedReason;
        }

        public string Id { get; }
        public int Priority { get; }

        public bool IsSupported() => _supported;

        public string? GetUnsupportedReason() => _unsupportedReason;

        public IControllerBackend Create(IntPtr windowHandle)
        {
            if (_exception != null)
                throw _exception;

            return _created!;
        }
    }

    public sealed record WheelPedalTrace(string Scenario, IReadOnlyList<WheelPedalSample> Samples);

    public sealed record WheelPedalSample(int Step, int RawZ, int RawRz, int RawSlider1, int Throttle, int Brake, int Clutch);

    public static WheelPedalTrace SimulateFullRangeCalibration()
    {
        var settings = new DriveSettings { DeviceMode = InputDeviceMode.Controller };
        var input = new DriveInput(settings);
        var steps = new[]
        {
            new State { Z = 100, Rz = 100, Slider1 = 100 },
            new State { Z = -100, Rz = -100, Slider1 = -100 },
            new State { Z = 0, Rz = 0, Slider1 = 0 }
        };

        return RunWheelSequence("FullRange", input, steps);
    }

    public static WheelPedalTrace SimulatePartialRangeCalibration()
    {
        var settings = new DriveSettings { DeviceMode = InputDeviceMode.Controller };
        var input = new DriveInput(settings);
        var steps = new[]
        {
            new State { Rz = 31 },
            new State { Rz = -31 },
            new State { Rz = 0 }
        };

        return RunWheelSequence("PartialRange", input, steps);
    }

    private static WheelPedalTrace RunWheelSequence(string scenario, DriveInput input, IReadOnlyList<State> steps)
    {
        var samples = new List<WheelPedalSample>(steps.Count);

        for (var i = 0; i < steps.Count; i++)
        {
            var state = steps[i];
            input.Run(new InputState(), state, 0f, controllerIsRacingWheel: true);
            samples.Add(new WheelPedalSample(
                i,
                state.Z,
                state.Rz,
                state.Slider1,
                input.Intents.GetAxisPercent(DriveIntent.Throttle),
                input.Intents.GetAxisPercent(DriveIntent.Brake),
                input.Intents.GetAxisPercent(DriveIntent.Clutch)));
        }

        return new WheelPedalTrace(scenario, samples);
    }
}

