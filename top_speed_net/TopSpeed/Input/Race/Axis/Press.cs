using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        private bool AxisPressed(AxisOrButton axis)
        {
            if (!UseController)
                return false;
            var current = GetAxis(axis, _lastController);
            var previous = _hasPrevController ? GetAxis(axis, _prevController) : 0;
            return current > 50 && previous <= 50;
        }
    }
}


