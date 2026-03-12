using System;
using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        public void Run(InputState input, float deltaSeconds)
        {
            Run(input, null, deltaSeconds, joystickIsRacingWheel: false);
        }

        public void Run(InputState input, JoystickStateSnapshot? joystick, float deltaSeconds)
        {
            Run(input, joystick, deltaSeconds, joystickIsRacingWheel: false);
        }

        public void Run(InputState input, JoystickStateSnapshot? joystick, float deltaSeconds, bool joystickIsRacingWheel)
        {
            _prevState.CopyFrom(_lastState);
            _lastState.CopyFrom(input);

            var wasJoystickAvailable = _joystickAvailable;
            var nextWheelMode = joystick.HasValue && joystickIsRacingWheel;
            var wheelModeChanged = _joystickIsRacingWheel != nextWheelMode;
            _joystickIsRacingWheel = nextWheelMode;

            if (joystick.HasValue)
            {
                if (_hasPrevJoystick)
                    _prevJoystick = _lastJoystick;
                _lastJoystick = joystick.Value;
                if (!_hasCenter)
                {
                    _center = joystick.Value;
                    _hasCenter = true;
                }
                if (!_hasPrevJoystick)
                    _prevJoystick = joystick.Value;
                _hasPrevJoystick = true;
            }
            _joystickAvailable = joystick.HasValue;
            if (!joystick.HasValue)
            {
                _hasPrevJoystick = false;
                _joystickIsRacingWheel = false;
            }

            if (!wasJoystickAvailable || !_joystickAvailable || wheelModeChanged)
                ResetPedalBaseline();

            if (_joystickAvailable && _joystickIsRacingWheel && !_hasPedalBaseline)
            {
                _pedalBaseline = _lastJoystick;
                _hasPedalBaseline = true;
            }

            UpdateSimulatedInputs(deltaSeconds);
        }

        public void SetCenter(JoystickStateSnapshot center)
        {
            _center = center;
            _hasCenter = true;
            _settings.JoystickCenter = center;
        }

        public void SetDevice(bool useJoystick)
        {
            SetDevice(useJoystick ? InputDeviceMode.Joystick : InputDeviceMode.Keyboard);
        }

        public void SetDevice(InputDeviceMode mode)
        {
            _deviceMode = mode;
            _settings.DeviceMode = mode;
        }
    }
}
