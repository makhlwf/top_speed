using System;
using SharpDX;
using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class InputManager
    {
        public void Update()
        {
            _previous.CopyFrom(_current);
            _current.Clear();

            if (_suspended || _disposed)
                return;

            if (!TryGetKeyboardState(out var state))
                return;
            foreach (var key in state.PressedKeys)
            {
                _current.Set(key, true);
            }

            if (!_joystickEnabled)
                return;

            _gamepad.Update();
            if (!_gamepad.IsAvailable)
            {
                var joystick = GetJoystickDevice();
                if (joystick == null || !joystick.IsAvailable)
                    return;
                joystick.Update();
            }
        }

        public bool WasPressed(Key key)
        {
            if (_suspended)
                return false;

            var index = (int)key;
            if (index < 0 || index >= _keyLatch.Length)
                return false;

            if (!TryGetKeyboardState(out var state))
            {
                _keyLatch[index] = false;
                return false;
            }

            if (state.IsPressed(key))
            {
                if (_keyLatch[index])
                    return false;
                _keyLatch[index] = true;
                return true;
            }

            _keyLatch[index] = false;
            return false;
        }

        public bool TryGetJoystickState(out JoystickStateSnapshot state)
        {
            if (!_joystickEnabled)
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

        public void ResetState()
        {
            _current.Clear();
            _previous.Clear();
            for (var i = 0; i < _keyLatch.Length; i++)
                _keyLatch[i] = false;
        }

        private bool TryAcquire()
        {
            if (_disposed)
                return false;

            try
            {
                _keyboard.Acquire();
                return true;
            }
            catch (SharpDXException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }
    }
}
