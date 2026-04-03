using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        public void SetLeft(AxisOrButton a)
        {
            _left = a;
            _settings.ControllerLeft = a;
        }

        public void SetLeft(Key key)
        {
            _kbLeft = key;
            _settings.KeyLeft = key;
        }

        public void SetRight(AxisOrButton a)
        {
            _right = a;
            _settings.ControllerRight = a;
        }

        public void SetRight(Key key)
        {
            _kbRight = key;
            _settings.KeyRight = key;
        }

        public void SetThrottle(AxisOrButton a)
        {
            _throttle = a;
            _settings.ControllerThrottle = a;
        }

        public void SetThrottle(Key key)
        {
            _kbThrottle = key;
            _settings.KeyThrottle = key;
        }

        public void SetBrake(AxisOrButton a)
        {
            _brake = a;
            _settings.ControllerBrake = a;
        }

        public void SetBrake(Key key)
        {
            _kbBrake = key;
            _settings.KeyBrake = key;
        }

        public void SetClutch(AxisOrButton a)
        {
            _clutch = a;
            _settings.ControllerClutch = a;
        }

        public void SetClutch(Key key)
        {
            _kbClutch = key;
            _settings.KeyClutch = key;
        }

        public void SetGearUp(AxisOrButton a)
        {
            _gearUp = a;
            _settings.ControllerGearUp = a;
        }

        public void SetGearUp(Key key)
        {
            _kbGearUp = key;
            _settings.KeyGearUp = key;
        }

        public void SetGearDown(AxisOrButton a)
        {
            _gearDown = a;
            _settings.ControllerGearDown = a;
        }

        public void SetGearDown(Key key)
        {
            _kbGearDown = key;
            _settings.KeyGearDown = key;
        }

        public void SetHorn(AxisOrButton a)
        {
            _horn = a;
            _settings.ControllerHorn = a;
        }

        public void SetHorn(Key key)
        {
            _kbHorn = key;
            _settings.KeyHorn = key;
        }
    }
}


