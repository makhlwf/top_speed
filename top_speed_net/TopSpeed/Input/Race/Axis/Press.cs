using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        private bool AxisPressed(JoystickAxisOrButton axis)
        {
            if (!UseJoystick)
                return false;
            var current = GetAxis(axis, _lastJoystick);
            var previous = _hasPrevJoystick ? GetAxis(axis, _prevJoystick) : 0;
            return current > 50 && previous <= 50;
        }
    }
}
