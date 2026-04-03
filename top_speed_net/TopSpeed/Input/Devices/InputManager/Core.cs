using System;
using System.Collections.Generic;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Keyboard;
using TopSpeed.Input.Devices.Vibration;
using TopSpeed.Runtime;

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
        private bool _suspended;
        private bool _menuBackLatched;
        private bool _disposed;

        public InputState Current => _current;
        public bool IgnoreControllerAxesForMenuNavigation => _controllerBackend.IgnoreAxesForMenuNavigation;
        public bool ActiveControllerIsRacingWheel => _controllerBackend.ActiveControllerIsRacingWheel;
        public IVibrationDevice? VibrationDevice => _controllerBackend.VibrationDevice;

        public event Action? ControllerScanTimedOut;

        internal InputService(IntPtr windowHandle, IBackendRegistry backendRegistry, IKeyboardEventSource? keyboardEventSource = null)
        {
            if (backendRegistry == null)
                throw new ArgumentNullException(nameof(backendRegistry));

            IKeyboardDevice? keyboardBackend = null;
            IControllerBackend? controllerBackend = null;
            try
            {
                keyboardBackend = backendRegistry.CreateKeyboard(windowHandle, keyboardEventSource);
                controllerBackend = backendRegistry.CreateController(windowHandle);
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
            _controllerBackend.ScanTimedOut += OnControllerScanTimedOut;
        }

        internal InputService(IKeyboardDevice keyboardBackend, IControllerBackend controllerBackend)
        {
            _keyboardBackend = keyboardBackend ?? throw new ArgumentNullException(nameof(keyboardBackend));
            _controllerBackend = controllerBackend ?? throw new ArgumentNullException(nameof(controllerBackend));
            _current = new InputState();
            _previous = new InputState();
            _keyLatch = new bool[256];
            _controllerBackend.ScanTimedOut += OnControllerScanTimedOut;
        }

        public bool IsDown(InputKey key) => _current.IsDown(key);

        public void SetDeviceMode(InputDeviceMode mode)
        {
            var enableController = mode != InputDeviceMode.Keyboard;
            _controllerBackend.SetEnabled(enableController);
        }

        public bool TryGetPendingControllerChoices(out IReadOnlyList<Choice> choices)
        {
            return _controllerBackend.TryGetPendingChoices(out choices);
        }

        public bool TrySelectController(Guid instanceGuid)
        {
            return _controllerBackend.TrySelect(instanceGuid);
        }

        private void OnControllerScanTimedOut()
        {
            ControllerScanTimedOut?.Invoke();
        }
    }
}

