using System;
using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        private int GetAxis(JoystickAxisOrButton axis)
        {
            return GetAxis(axis, _lastJoystick);
        }

        private int GetAxis(JoystickAxisOrButton axis, JoystickStateSnapshot state)
        {
            if (axis == JoystickAxisOrButton.AxisNone)
                return 0;

            if (TryGetAxisComponent(axis, out var component, out var mappedPositive))
            {
                var centerValue = GetAxisComponentValue(_center, component);
                var currentValue = GetAxisComponentValue(state, component);
                var delta = mappedPositive ? (currentValue - centerValue) : (centerValue - currentValue);
                return delta > 0 ? Math.Min(delta, 100) : 0;
            }

            if (TryGetDigitalAxisValue(axis, state, out var value))
                return value;

            return 0;
        }

        private static bool TryGetDigitalAxisValue(JoystickAxisOrButton axis, JoystickStateSnapshot state, out int value)
        {
            switch (axis)
            {
                case JoystickAxisOrButton.Button1:
                    value = state.B1 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button2:
                    value = state.B2 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button3:
                    value = state.B3 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button4:
                    value = state.B4 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button5:
                    value = state.B5 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button6:
                    value = state.B6 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button7:
                    value = state.B7 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button8:
                    value = state.B8 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button9:
                    value = state.B9 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button10:
                    value = state.B10 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button11:
                    value = state.B11 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button12:
                    value = state.B12 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button13:
                    value = state.B13 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button14:
                    value = state.B14 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button15:
                    value = state.B15 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Button16:
                    value = state.B16 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Pov1:
                    value = state.Pov1 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Pov2:
                    value = state.Pov2 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Pov3:
                    value = state.Pov3 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Pov4:
                    value = state.Pov4 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Pov5:
                    value = state.Pov5 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Pov6:
                    value = state.Pov6 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Pov7:
                    value = state.Pov7 ? 100 : 0;
                    return true;
                case JoystickAxisOrButton.Pov8:
                    value = state.Pov8 ? 100 : 0;
                    return true;
                default:
                    value = 0;
                    return false;
            }
        }
    }
}
