using System;
using System.Collections.Generic;
using System.Threading;
using SharpDX;
using Di = SharpDX.DirectInput;
using TopSpeed.Input.Backends.XInput;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Vibration;
using ControllerDevice = TopSpeed.Input.Devices.Controller.Device;

namespace TopSpeed.Input.Backends.DirectInput
{
    internal sealed class ControllerBackend : IControllerBackend
    {
        private const int ControllerRescanIntervalMs = 1000;
        private const int ControllerScanTimeoutMs = 5000;

        private readonly Di.DirectInput _directInput;
        private readonly IntPtr _windowHandle;
        private readonly Pad _gamepad;
        private readonly object _hidLock = new object();
        private readonly object _hidScanLock = new object();

        private ControllerDevice? _controller;
        private Thread? _hidScanThread;
        private CancellationTokenSource? _hidScanCts;
        private List<Choice>? _pendingControllerChoices;
        private int _lastControllerScan;
        private bool _enabled;
        private bool _activeControllerIsRacingWheel;
        private bool _disposed;

        public event Action? ScanTimedOut;

        public ControllerBackend(IntPtr windowHandle)
        {
            _directInput = new Di.DirectInput();
            _windowHandle = windowHandle;
            _gamepad = new Pad();
        }

        public bool ActiveControllerIsRacingWheel => _enabled && !_gamepad.IsAvailable && _activeControllerIsRacingWheel;

        public bool IgnoreAxesForMenuNavigation => _enabled && !_gamepad.IsAvailable && _activeControllerIsRacingWheel;

        public IVibrationDevice? VibrationDevice => _gamepad.IsAvailable
            ? (_enabled ? _gamepad : null)
            : (_enabled ? GetControllerDevice() : null);

        public void SetEnabled(bool enabled)
        {
            if (enabled == _enabled)
                return;

            _enabled = enabled;
            if (!_enabled)
            {
                StopHidScan();
                lock (_hidLock)
                {
                    _pendingControllerChoices = null;
                }

                ClearControllerDevice();
                return;
            }

            if (_gamepad.IsAvailable)
            {
                lock (_hidLock)
                {
                    _pendingControllerChoices = null;
                    _activeControllerIsRacingWheel = false;
                }

                return;
            }

            if (GetControllerDevice() == null)
                StartHidScan();
        }

        public void Update()
        {
            if (_disposed || !_enabled)
                return;

            _gamepad.Update();
            if (_gamepad.IsAvailable)
                return;

            var controller = GetControllerDevice();
            if (controller == null || !controller.IsAvailable)
                return;

            controller.Update();
        }

        public bool TryGetState(out State state)
        {
            if (!_enabled)
            {
                state = default;
                return false;
            }

            var device = VibrationDevice;
            if (device != null && device.IsAvailable)
            {
                state = device.State;
                return true;
            }

            state = default;
            return false;
        }

        public bool TryPollState(out State state)
        {
            if (_disposed || !_enabled)
            {
                state = default;
                return false;
            }

            _gamepad.Update();
            if (_gamepad.IsAvailable)
            {
                state = _gamepad.State;
                return true;
            }

            var controller = GetControllerDevice();
            if (controller == null || !controller.IsAvailable)
            {
                state = default;
                return false;
            }

            if (!controller.Update())
            {
                state = default;
                return false;
            }

            state = controller.State;
            return true;
        }

        public bool IsAnyButtonHeld()
        {
            return TryPollState(out var state) && state.HasAnyButtonDown();
        }

        public bool TryGetPendingChoices(out IReadOnlyList<Choice> choices)
        {
            lock (_hidLock)
            {
                if (_pendingControllerChoices == null || _pendingControllerChoices.Count == 0)
                {
                    choices = Array.Empty<Choice>();
                    return false;
                }

                choices = _pendingControllerChoices.ToArray();
                return true;
            }
        }

        public bool TrySelect(Guid instanceGuid)
        {
            if (instanceGuid == Guid.Empty)
                return false;

            List<Choice>? pendingChoices;
            lock (_hidLock)
            {
                pendingChoices = _pendingControllerChoices == null
                    ? null
                    : new List<Choice>(_pendingControllerChoices);
            }

            Choice? selected = null;
            if (pendingChoices != null)
            {
                for (var i = 0; i < pendingChoices.Count; i++)
                {
                    if (pendingChoices[i].InstanceGuid == instanceGuid)
                    {
                        selected = pendingChoices[i];
                        break;
                    }
                }
            }

            if (selected == null)
            {
                var discovered = ControllerDevice.Discover(_directInput);
                for (var i = 0; i < discovered.Count; i++)
                {
                    if (discovered[i].InstanceGuid == instanceGuid)
                    {
                        selected = discovered[i];
                        break;
                    }
                }
            }

            if (selected == null)
                return false;

            if (!TryAttachController(selected))
                return false;

            lock (_hidLock)
            {
                _pendingControllerChoices = null;
            }

            StopHidScan();
            return true;
        }

        public void Suspend()
        {
            ControllerDevice? controller;
            lock (_hidLock)
            {
                controller = _controller;
            }

            if (controller?.Native == null)
                return;

            try
            {
                controller.Native.Unacquire();
            }
            catch (SharpDXException)
            {
            }
        }

        public void Resume()
        {
            ControllerDevice? controller;
            lock (_hidLock)
            {
                controller = _controller;
            }

            if (controller?.Native == null)
                return;

            try
            {
                controller.Native.Acquire();
            }
            catch (SharpDXException)
            {
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopHidScan();
            SafeRelease(() => _gamepad.Dispose());

            ControllerDevice? controller;
            lock (_hidLock)
            {
                controller = _controller;
                _controller = null;
                _pendingControllerChoices = null;
                _activeControllerIsRacingWheel = false;
            }

            SafeRelease(() => controller?.Dispose());
            SafeRelease(() => _directInput.Dispose());
        }

        private ControllerDevice? GetControllerDevice()
        {
            lock (_hidLock)
            {
                return _controller != null && _controller.IsAvailable ? _controller : null;
            }
        }

        private bool TryRescanController(bool force = false)
        {
            if (_disposed)
                return false;

            var now = Environment.TickCount;
            if (!force && unchecked((uint)(now - _lastControllerScan)) < (uint)ControllerRescanIntervalMs)
                return false;

            _lastControllerScan = now;

            List<Choice> discovered;
            try
            {
                discovered = ControllerDevice.Discover(_directInput);
            }
            catch (SharpDXException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }

            if (discovered.Count == 0)
                return false;

            if (discovered.Count == 1)
            {
                lock (_hidLock)
                {
                    _pendingControllerChoices = null;
                }

                return TryAttachController(discovered[0]);
            }

            lock (_hidLock)
            {
                _pendingControllerChoices = discovered;
                _activeControllerIsRacingWheel = false;
            }

            ClearControllerDevice();
            return true;
        }

        private void StartHidScan()
        {
            if (_disposed || !_enabled || _gamepad.IsAvailable)
                return;

            lock (_hidScanLock)
            {
                if (_hidScanThread != null && _hidScanThread.IsAlive)
                    return;

                _hidScanCts?.Cancel();
                _hidScanCts?.Dispose();
                _hidScanCts = new CancellationTokenSource();
                var token = _hidScanCts.Token;
                _hidScanThread = new Thread(() => ControllerScanWorker(token))
                {
                    IsBackground = true,
                    Name = "ControllerScan"
                };
                _hidScanThread.Start();
            }
        }

        private void StopHidScan()
        {
            CancellationTokenSource? cts;
            Thread? thread;
            lock (_hidScanLock)
            {
                cts = _hidScanCts;
                thread = _hidScanThread;
                _hidScanCts = null;
                _hidScanThread = null;
            }

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            if (thread != null && thread.IsAlive)
                thread.Join(ControllerRescanIntervalMs + 500);
        }

        private void ControllerScanWorker(CancellationToken token)
        {
            var start = Environment.TickCount;
            while (true)
            {
                if (token.IsCancellationRequested || _disposed || !_enabled)
                    return;

                if (_gamepad.IsAvailable)
                    return;

                if (TryRescanController(force: true))
                    return;

                var elapsed = unchecked((uint)(Environment.TickCount - start));
                if (elapsed >= (uint)ControllerScanTimeoutMs)
                {
                    ScanTimedOut?.Invoke();
                    return;
                }

                if (token.WaitHandle.WaitOne(ControllerRescanIntervalMs))
                    return;
            }
        }

        private void ClearControllerDevice()
        {
            ControllerDevice? oldController;
            lock (_hidLock)
            {
                oldController = _controller;
                _controller = null;
                _activeControllerIsRacingWheel = false;
            }

            oldController?.Dispose();
        }

        private bool TryAttachController(Choice choice)
        {
            ControllerDevice? newController;
            try
            {
                newController = new ControllerDevice(_directInput, _windowHandle, choice);
            }
            catch (SharpDXException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }

            if (!newController.IsAvailable)
            {
                newController.Dispose();
                return false;
            }

            ControllerDevice? oldController;
            lock (_hidLock)
            {
                oldController = _controller;
                _controller = newController;
                _activeControllerIsRacingWheel = choice.IsRacingWheel;
            }

            oldController?.Dispose();
            return true;
        }

        private static void SafeRelease(Action release)
        {
            try
            {
                release();
            }
            catch (SharpDXException)
            {
            }
            catch (NullReferenceException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}

