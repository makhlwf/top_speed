using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        private enum AxisComponent
        {
            X,
            Y,
            Z,
            Rx,
            Ry,
            Rz,
            Slider1,
            Slider2
        }

        private static bool TryGetAxisComponent(JoystickAxisOrButton axis, out AxisComponent component, out bool mappedPositive)
        {
            switch (axis)
            {
                case JoystickAxisOrButton.AxisXNeg:
                    component = AxisComponent.X;
                    mappedPositive = false;
                    return true;
                case JoystickAxisOrButton.AxisXPos:
                    component = AxisComponent.X;
                    mappedPositive = true;
                    return true;
                case JoystickAxisOrButton.AxisYNeg:
                    component = AxisComponent.Y;
                    mappedPositive = false;
                    return true;
                case JoystickAxisOrButton.AxisYPos:
                    component = AxisComponent.Y;
                    mappedPositive = true;
                    return true;
                case JoystickAxisOrButton.AxisZNeg:
                    component = AxisComponent.Z;
                    mappedPositive = false;
                    return true;
                case JoystickAxisOrButton.AxisZPos:
                    component = AxisComponent.Z;
                    mappedPositive = true;
                    return true;
                case JoystickAxisOrButton.AxisRxNeg:
                    component = AxisComponent.Rx;
                    mappedPositive = false;
                    return true;
                case JoystickAxisOrButton.AxisRxPos:
                    component = AxisComponent.Rx;
                    mappedPositive = true;
                    return true;
                case JoystickAxisOrButton.AxisRyNeg:
                    component = AxisComponent.Ry;
                    mappedPositive = false;
                    return true;
                case JoystickAxisOrButton.AxisRyPos:
                    component = AxisComponent.Ry;
                    mappedPositive = true;
                    return true;
                case JoystickAxisOrButton.AxisRzNeg:
                    component = AxisComponent.Rz;
                    mappedPositive = false;
                    return true;
                case JoystickAxisOrButton.AxisRzPos:
                    component = AxisComponent.Rz;
                    mappedPositive = true;
                    return true;
                case JoystickAxisOrButton.AxisSlider1Neg:
                    component = AxisComponent.Slider1;
                    mappedPositive = false;
                    return true;
                case JoystickAxisOrButton.AxisSlider1Pos:
                    component = AxisComponent.Slider1;
                    mappedPositive = true;
                    return true;
                case JoystickAxisOrButton.AxisSlider2Neg:
                    component = AxisComponent.Slider2;
                    mappedPositive = false;
                    return true;
                case JoystickAxisOrButton.AxisSlider2Pos:
                    component = AxisComponent.Slider2;
                    mappedPositive = true;
                    return true;
                default:
                    component = AxisComponent.X;
                    mappedPositive = false;
                    return false;
            }
        }

        private static int GetAxisComponentValue(JoystickStateSnapshot state, AxisComponent component)
        {
            switch (component)
            {
                case AxisComponent.X:
                    return state.X;
                case AxisComponent.Y:
                    return state.Y;
                case AxisComponent.Z:
                    return state.Z;
                case AxisComponent.Rx:
                    return state.Rx;
                case AxisComponent.Ry:
                    return state.Ry;
                case AxisComponent.Rz:
                    return state.Rz;
                case AxisComponent.Slider1:
                    return state.Slider1;
                case AxisComponent.Slider2:
                    return state.Slider2;
                default:
                    return 0;
            }
        }
    }
}
