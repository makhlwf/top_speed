using System;
using SharpDX;
using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class InputManager
    {
        public void Suspend()
        {
            _suspended = true;
            try
            {
                _keyboard.Unacquire();
            }
            catch (SharpDXException)
            {
            }

            JoystickDevice? joystick;
            lock (_hidLock)
            {
                joystick = _joystick;
            }

            if (joystick?.Device != null)
            {
                try
                {
                    joystick.Device.Unacquire();
                }
                catch (SharpDXException)
                {
                }
            }
        }

        public void Resume()
        {
            _suspended = false;
            TryAcquire();

            JoystickDevice? joystick;
            lock (_hidLock)
            {
                joystick = _joystick;
            }

            if (joystick?.Device != null)
            {
                try
                {
                    joystick.Device.Acquire();
                }
                catch (SharpDXException)
                {
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopHidScan();
            SafeRelease(() => _keyboard.Unacquire());
            SafeRelease(() => _keyboard.Dispose());
            SafeRelease(() => _gamepad.Dispose());

            JoystickDevice? joystick;
            lock (_hidLock)
            {
                joystick = _joystick;
                _joystick = null;
            }

            SafeRelease(() => joystick?.Dispose());
            SafeRelease(() => _directInput.Dispose());
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
