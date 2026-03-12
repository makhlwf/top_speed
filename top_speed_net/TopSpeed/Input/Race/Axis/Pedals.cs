using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        private int GetPedalAxis(JoystickAxisOrButton axis, PedalInvertMode mode)
        {
            if (!UseJoystick)
                return 0;

            if (!TryGetAxisComponent(axis, out var component, out var mappedPositive))
                return GetAxis(axis);

            if (!_joystickIsRacingWheel)
                return GetAxis(axis);

            if (!_hasPedalBaseline)
            {
                _pedalBaseline = _lastJoystick;
                _hasPedalBaseline = true;
            }

            var baseline = GetAxisComponentValue(_pedalBaseline, component);
            var current = GetAxisComponentValue(_lastJoystick, component);
            var directionPositive = ResolvePedalDirectionPositive(mode, mappedPositive, baseline);
            var useEndpointScaling = IsEndpointBaseline(baseline);
            return ResolvePedalValue(component, current, baseline, directionPositive, useEndpointScaling);
        }

        private int ResolvePedalValue(AxisComponent component, int current, int baseline, bool directionPositive, bool useEndpointScaling)
        {
            if (useEndpointScaling && IsEndpointBaseline(baseline))
            {
                if (directionPositive)
                {
                    var maxTravel = 100 - baseline;
                    if (maxTravel <= 0)
                        return 0;
                    var movement = current - baseline;
                    if (movement <= 0)
                        return 0;
                    return ClampPercent((movement * 100) / maxTravel);
                }

                var negativeTravel = baseline + 100;
                if (negativeTravel <= 0)
                    return 0;
                var reverseMovement = baseline - current;
                if (reverseMovement <= 0)
                    return 0;
                return ClampPercent((reverseMovement * 100) / negativeTravel);
            }

            var center = GetAxisComponentValue(_center, component);
            var delta = directionPositive ? (current - center) : (center - current);
            if (delta <= 0)
                return 0;
            return ClampPercent(delta);
        }

        private static int ClampPercent(int value)
        {
            if (value <= 0)
                return 0;
            if (value >= 100)
                return 100;
            return value;
        }

        private static bool ResolvePedalDirectionPositive(PedalInvertMode mode, bool mappedPositive, int baseline)
        {
            switch (mode)
            {
                case PedalInvertMode.Normal:
                    return mappedPositive;
                case PedalInvertMode.Inverted:
                    return !mappedPositive;
                default:
                    if (IsEndpointBaseline(baseline))
                        return baseline < 0;
                    return mappedPositive;
            }
        }

        private static bool IsEndpointBaseline(int value)
        {
            return value >= 85 || value <= -85;
        }
    }
}
