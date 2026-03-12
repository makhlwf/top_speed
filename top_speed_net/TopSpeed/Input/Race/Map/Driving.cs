using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        public void SetLeft(JoystickAxisOrButton a)
        {
            _left = a;
            _settings.JoystickLeft = a;
        }

        public void SetLeft(Key key)
        {
            _kbLeft = key;
            _settings.KeyLeft = key;
        }

        public void SetRight(JoystickAxisOrButton a)
        {
            _right = a;
            _settings.JoystickRight = a;
        }

        public void SetRight(Key key)
        {
            _kbRight = key;
            _settings.KeyRight = key;
        }

        public void SetThrottle(JoystickAxisOrButton a)
        {
            _throttle = a;
            _settings.JoystickThrottle = a;
        }

        public void SetThrottle(Key key)
        {
            _kbThrottle = key;
            _settings.KeyThrottle = key;
        }

        public void SetBrake(JoystickAxisOrButton a)
        {
            _brake = a;
            _settings.JoystickBrake = a;
        }

        public void SetBrake(Key key)
        {
            _kbBrake = key;
            _settings.KeyBrake = key;
        }

        public void SetGearUp(JoystickAxisOrButton a)
        {
            _gearUp = a;
            _settings.JoystickGearUp = a;
        }

        public void SetGearUp(Key key)
        {
            _kbGearUp = key;
            _settings.KeyGearUp = key;
        }

        public void SetGearDown(JoystickAxisOrButton a)
        {
            _gearDown = a;
            _settings.JoystickGearDown = a;
        }

        public void SetGearDown(Key key)
        {
            _kbGearDown = key;
            _settings.KeyGearDown = key;
        }

        public void SetHorn(JoystickAxisOrButton a)
        {
            _horn = a;
            _settings.JoystickHorn = a;
        }

        public void SetHorn(Key key)
        {
            _kbHorn = key;
            _settings.KeyHorn = key;
        }
    }
}
