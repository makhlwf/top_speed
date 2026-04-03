using System;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        private int GetAxis(AxisOrButton axis)
        {
            return GetAxis(axis, _lastController);
        }

        private int GetAxis(AxisOrButton axis, State state)
        {
            if (axis == AxisOrButton.AxisNone)
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

        private static bool TryGetDigitalAxisValue(AxisOrButton axis, State state, out int value)
        {
            switch (axis)
            {
                case AxisOrButton.Button1:
                    value = state.B1 ? 100 : 0;
                    return true;
                case AxisOrButton.Button2:
                    value = state.B2 ? 100 : 0;
                    return true;
                case AxisOrButton.Button3:
                    value = state.B3 ? 100 : 0;
                    return true;
                case AxisOrButton.Button4:
                    value = state.B4 ? 100 : 0;
                    return true;
                case AxisOrButton.Button5:
                    value = state.B5 ? 100 : 0;
                    return true;
                case AxisOrButton.Button6:
                    value = state.B6 ? 100 : 0;
                    return true;
                case AxisOrButton.Button7:
                    value = state.B7 ? 100 : 0;
                    return true;
                case AxisOrButton.Button8:
                    value = state.B8 ? 100 : 0;
                    return true;
                case AxisOrButton.Button9:
                    value = state.B9 ? 100 : 0;
                    return true;
                case AxisOrButton.Button10:
                    value = state.B10 ? 100 : 0;
                    return true;
                case AxisOrButton.Button11:
                    value = state.B11 ? 100 : 0;
                    return true;
                case AxisOrButton.Button12:
                    value = state.B12 ? 100 : 0;
                    return true;
                case AxisOrButton.Button13:
                    value = state.B13 ? 100 : 0;
                    return true;
                case AxisOrButton.Button14:
                    value = state.B14 ? 100 : 0;
                    return true;
                case AxisOrButton.Button15:
                    value = state.B15 ? 100 : 0;
                    return true;
                case AxisOrButton.Button16:
                    value = state.B16 ? 100 : 0;
                    return true;
                case AxisOrButton.Pov1:
                    value = state.Pov1 ? 100 : 0;
                    return true;
                case AxisOrButton.Pov2:
                    value = state.Pov2 ? 100 : 0;
                    return true;
                case AxisOrButton.Pov3:
                    value = state.Pov3 ? 100 : 0;
                    return true;
                case AxisOrButton.Pov4:
                    value = state.Pov4 ? 100 : 0;
                    return true;
                case AxisOrButton.Pov5:
                    value = state.Pov5 ? 100 : 0;
                    return true;
                case AxisOrButton.Pov6:
                    value = state.Pov6 ? 100 : 0;
                    return true;
                case AxisOrButton.Pov7:
                    value = state.Pov7 ? 100 : 0;
                    return true;
                case AxisOrButton.Pov8:
                    value = state.Pov8 ? 100 : 0;
                    return true;
                default:
                    value = 0;
                    return false;
            }
        }
    }
}


