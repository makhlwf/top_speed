using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;

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

        private static bool TryGetAxisComponent(AxisOrButton axis, out AxisComponent component, out bool mappedPositive)
        {
            switch (axis)
            {
                case AxisOrButton.AxisXNeg:
                    component = AxisComponent.X;
                    mappedPositive = false;
                    return true;
                case AxisOrButton.AxisXPos:
                    component = AxisComponent.X;
                    mappedPositive = true;
                    return true;
                case AxisOrButton.AxisYNeg:
                    component = AxisComponent.Y;
                    mappedPositive = false;
                    return true;
                case AxisOrButton.AxisYPos:
                    component = AxisComponent.Y;
                    mappedPositive = true;
                    return true;
                case AxisOrButton.AxisZNeg:
                    component = AxisComponent.Z;
                    mappedPositive = false;
                    return true;
                case AxisOrButton.AxisZPos:
                    component = AxisComponent.Z;
                    mappedPositive = true;
                    return true;
                case AxisOrButton.AxisRxNeg:
                    component = AxisComponent.Rx;
                    mappedPositive = false;
                    return true;
                case AxisOrButton.AxisRxPos:
                    component = AxisComponent.Rx;
                    mappedPositive = true;
                    return true;
                case AxisOrButton.AxisRyNeg:
                    component = AxisComponent.Ry;
                    mappedPositive = false;
                    return true;
                case AxisOrButton.AxisRyPos:
                    component = AxisComponent.Ry;
                    mappedPositive = true;
                    return true;
                case AxisOrButton.AxisRzNeg:
                    component = AxisComponent.Rz;
                    mappedPositive = false;
                    return true;
                case AxisOrButton.AxisRzPos:
                    component = AxisComponent.Rz;
                    mappedPositive = true;
                    return true;
                case AxisOrButton.AxisSlider1Neg:
                    component = AxisComponent.Slider1;
                    mappedPositive = false;
                    return true;
                case AxisOrButton.AxisSlider1Pos:
                    component = AxisComponent.Slider1;
                    mappedPositive = true;
                    return true;
                case AxisOrButton.AxisSlider2Neg:
                    component = AxisComponent.Slider2;
                    mappedPositive = false;
                    return true;
                case AxisOrButton.AxisSlider2Pos:
                    component = AxisComponent.Slider2;
                    mappedPositive = true;
                    return true;
                default:
                    component = AxisComponent.X;
                    mappedPositive = false;
                    return false;
            }
        }

        private static int GetAxisComponentValue(State state, AxisComponent component)
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


