using System;
using System.Collections.Generic;
using System.Diagnostics;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Keyboard;
using TopSpeed.Input.Devices.Vibration;
using TopSpeed.Runtime;
using TS.Sdl.Input;

namespace TopSpeed.Input
{
    internal sealed partial class InputService : IInputService
    {
        private const int MenuBackThreshold = 50;

        private readonly IKeyboardDevice _keyboardBackend;
        private readonly IControllerBackend _controllerBackend;
        private readonly InputState _current;
        private readonly InputState _previous;
        private readonly bool[] _keyLatch;
        private readonly object _gestureSync;
        private readonly Dictionary<GestureIntent, int> _gesturePressCounts;
        private readonly Dictionary<ZoneGestureKey, int> _zoneGesturePressCounts;
        private readonly Dictionary<TouchPointKey, TouchPointState> _zoneTouchPoints;
        private readonly IGestureEventSource? _gestureEventSource;
        private readonly ITouchZoneGestureEventSource? _touchZoneGestureEventSource;
        private readonly ITouchZoneTouchEventSource? _touchZoneTouchEventSource;
        private readonly string? _controllerBackendUnavailableMessage;
        private bool _suspended;
        private bool _menuBackLatched;
        private bool _disposed;

        public InputState Current => _current;
        public bool IgnoreControllerAxesForMenuNavigation => _controllerBackend.IgnoreAxesForMenuNavigation;
        public bool ActiveControllerIsRacingWheel => _controllerBackend.ActiveControllerIsRacingWheel;
        public IVibrationDevice? VibrationDevice => _controllerBackend.VibrationDevice;

        public event Action? NoControllerDetected;
        public event Action<string>? ControllerBackendUnavailable;

        internal InputService(
            IntPtr windowHandle,
            IBackendRegistry backendRegistry,
            IKeyboardEventSource? keyboardEventSource = null,
            IGestureEventSource? gestureEventSource = null)
        {
            if (backendRegistry == null)
                throw new ArgumentNullException(nameof(backendRegistry));

            IKeyboardDevice? keyboardBackend = null;
            IControllerBackend? controllerBackend = null;
            try
            {
                keyboardBackend = backendRegistry.CreateKeyboard(windowHandle, keyboardEventSource);
                try
                {
                    controllerBackend = backendRegistry.CreateController(windowHandle);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Controller backend unavailable: {ex}");
                    _controllerBackendUnavailableMessage = ex.Message;
                    controllerBackend = new Backends.Disabled.Controller(ex.Message);
                }
                _keyboardBackend = keyboardBackend;
                _controllerBackend = controllerBackend;
            }
            catch
            {
                keyboardBackend?.Dispose();
                controllerBackend?.Dispose();
                throw;
            }

            _current = new InputState();
            _previous = new InputState();
            _keyLatch = new bool[256];
            _gestureSync = new object();
            _gesturePressCounts = new Dictionary<GestureIntent, int>();
            _zoneGesturePressCounts = new Dictionary<ZoneGestureKey, int>();
            _zoneTouchPoints = new Dictionary<TouchPointKey, TouchPointState>();
            _gestureEventSource = gestureEventSource;
            _touchZoneGestureEventSource = gestureEventSource as ITouchZoneGestureEventSource;
            _touchZoneTouchEventSource = gestureEventSource as ITouchZoneTouchEventSource;
            if (_gestureEventSource != null)
                _gestureEventSource.GestureRaised += OnGestureRaised;
            if (_touchZoneGestureEventSource != null)
                _touchZoneGestureEventSource.TouchZoneGestureRaised += OnTouchZoneGestureRaised;
            if (_touchZoneTouchEventSource != null)
                _touchZoneTouchEventSource.TouchZoneTouchRaised += OnTouchZoneTouchRaised;
            _controllerBackend.NoControllerDetected += OnNoControllerDetected;
        }

        internal InputService(IKeyboardDevice keyboardBackend, IControllerBackend controllerBackend)
        {
            _keyboardBackend = keyboardBackend ?? throw new ArgumentNullException(nameof(keyboardBackend));
            _controllerBackend = controllerBackend ?? throw new ArgumentNullException(nameof(controllerBackend));
            _current = new InputState();
            _previous = new InputState();
            _keyLatch = new bool[256];
            _gestureSync = new object();
            _gesturePressCounts = new Dictionary<GestureIntent, int>();
            _zoneGesturePressCounts = new Dictionary<ZoneGestureKey, int>();
            _zoneTouchPoints = new Dictionary<TouchPointKey, TouchPointState>();
            _controllerBackend.NoControllerDetected += OnNoControllerDetected;
        }

        public bool IsDown(InputKey key) => _current.IsDown(key);

        public void SetDeviceMode(InputDeviceMode mode)
        {
            var enableController = mode != InputDeviceMode.Keyboard;
            _controllerBackend.SetEnabled(enableController);
            var message = _controllerBackendUnavailableMessage;
            if (enableController && message != null && message.Length > 0)
                ControllerBackendUnavailable?.Invoke(message);
        }

        public bool TryGetPendingControllerChoices(out IReadOnlyList<Choice> choices)
        {
            return _controllerBackend.TryGetPendingChoices(out choices);
        }

        public bool TrySelectController(Guid instanceGuid)
        {
            return _controllerBackend.TrySelect(instanceGuid);
        }

        public bool TryGetControllerDisplayProfile(out ControllerDisplayProfile profile)
        {
            return _controllerBackend.TryGetDisplayProfile(out profile);
        }

        private void OnNoControllerDetected()
        {
            NoControllerDetected?.Invoke();
        }

        private void OnGestureRaised(GestureEvent value)
        {
            SubmitGesture(value);
        }

        private void OnTouchZoneGestureRaised(TouchZoneGestureEvent value)
        {
            SubmitTouchZoneGesture(value);
        }

        private void OnTouchZoneTouchRaised(TouchZoneTouchEvent value)
        {
            SubmitTouchZoneTouch(value);
        }

        private readonly struct ZoneGestureKey : IEquatable<ZoneGestureKey>
        {
            public ZoneGestureKey(GestureIntent intent, string zoneId)
            {
                Intent = intent;
                ZoneId = zoneId;
            }

            public GestureIntent Intent { get; }
            public string ZoneId { get; }

            public bool Equals(ZoneGestureKey other)
            {
                return Intent == other.Intent && string.Equals(ZoneId, other.ZoneId, StringComparison.Ordinal);
            }

            public override bool Equals(object? obj)
            {
                return obj is ZoneGestureKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)Intent * 397) ^ (ZoneId != null ? StringComparer.Ordinal.GetHashCode(ZoneId) : 0);
                }
            }
        }

        private readonly struct TouchPointKey : IEquatable<TouchPointKey>
        {
            public TouchPointKey(ulong touchId, ulong fingerId)
            {
                TouchId = touchId;
                FingerId = fingerId;
            }

            public ulong TouchId { get; }
            public ulong FingerId { get; }

            public bool Equals(TouchPointKey other)
            {
                return TouchId == other.TouchId && FingerId == other.FingerId;
            }

            public override bool Equals(object? obj)
            {
                return obj is TouchPointKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (TouchId.GetHashCode() * 397) ^ FingerId.GetHashCode();
                }
            }
        }

        private readonly struct TouchPointState
        {
            public TouchPointState(
                string zoneId,
                float startX,
                float startY,
                float x,
                float y,
                float pressure,
                ulong startTimestamp,
                ulong timestamp)
            {
                ZoneId = zoneId;
                StartX = startX;
                StartY = startY;
                X = x;
                Y = y;
                Pressure = pressure;
                StartTimestamp = startTimestamp;
                Timestamp = timestamp;
            }

            public string ZoneId { get; }
            public float StartX { get; }
            public float StartY { get; }
            public float X { get; }
            public float Y { get; }
            public float Pressure { get; }
            public ulong StartTimestamp { get; }
            public ulong Timestamp { get; }
        }
    }
}

