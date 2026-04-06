using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        private int GetPedalAxis(AxisOrButton axis, PedalInvertMode mode)
        {
            if (!UseController)
                return 0;

            if (!TryGetAxisComponent(axis, out var component, out var mappedPositive))
                return GetAxis(axis);

            if (!_controllerIsRacingWheel)
                return GetAxis(axis);

            var index = (int)component;
            var current = GetAxisComponentValue(_lastController, component);
            EnsurePedalCalibration(component, current);

            var rest = _pedalRestValues[index];
            var min = _pedalMinValues[index];
            var max = _pedalMaxValues[index];
            var directionPositive = ResolvePedalDirectionPositive(mode, mappedPositive, rest, min, max);
            return ResolvePedalValue(current, rest, min, max, directionPositive);
        }

        private int ResolvePedalValue(int current, int rest, int min, int max, bool directionPositive)
        {
            if (directionPositive)
            {
                var maxTravel = max - rest;
                if (maxTravel > 0)
                {
                    var movement = current - rest;
                    if (movement <= 0)
                        return 0;
                    return ClampPercent((movement * 100) / maxTravel);
                }
            }
            else
            {
                var maxTravel = rest - min;
                if (maxTravel > 0)
                {
                    var movement = rest - current;
                    if (movement <= 0)
                        return 0;
                    return ClampPercent((movement * 100) / maxTravel);
                }
            }

            return 0;
        }

        private static int ClampPercent(int value)
        {
            if (value <= 0)
                return 0;
            if (value >= 100)
                return 100;
            return value;
        }

        private static bool ResolvePedalDirectionPositive(PedalInvertMode mode, bool mappedPositive, int rest, int min, int max)
        {
            switch (mode)
            {
                case PedalInvertMode.Normal:
                    return mappedPositive;
                case PedalInvertMode.Inverted:
                    return !mappedPositive;
                default:
                    if (TryResolveAutoPedalDirection(rest, min, max, out var directionPositive))
                        return directionPositive;
                    return mappedPositive;
            }
        }

        private void EnsurePedalCalibration(AxisComponent component, int current)
        {
            var index = (int)component;
            if (!_hasPedalCalibration[index])
            {
                _hasPedalCalibration[index] = true;
                _pedalRestValues[index] = current;
                _pedalMinValues[index] = current;
                _pedalMaxValues[index] = current;
                return;
            }

            if (current < _pedalMinValues[index])
                _pedalMinValues[index] = current;
            if (current > _pedalMaxValues[index])
                _pedalMaxValues[index] = current;
            if (ShouldUpdatePedalRest(_pedalRestValues[index], current))
                _pedalRestValues[index] = current;
        }

        private void UpdatePedalCalibrationSamples()
        {
            EnsurePedalCalibration(AxisComponent.X, _lastController.X);
            EnsurePedalCalibration(AxisComponent.Y, _lastController.Y);
            EnsurePedalCalibration(AxisComponent.Z, _lastController.Z);
            EnsurePedalCalibration(AxisComponent.Rx, _lastController.Rx);
            EnsurePedalCalibration(AxisComponent.Ry, _lastController.Ry);
            EnsurePedalCalibration(AxisComponent.Rz, _lastController.Rz);
            EnsurePedalCalibration(AxisComponent.Slider1, _lastController.Slider1);
            EnsurePedalCalibration(AxisComponent.Slider2, _lastController.Slider2);
        }

        private static bool ShouldUpdatePedalRest(int currentRest, int candidate)
        {
            var currentAbs = System.Math.Abs(currentRest);
            var candidateAbs = System.Math.Abs(candidate);
            if (candidateAbs <= currentAbs + 2)
                return false;

            if (currentAbs < 20)
                return true;

            if (System.Math.Sign(candidate) != System.Math.Sign(currentRest))
                return false;

            return true;
        }

        private static bool TryResolveAutoPedalDirection(int rest, int min, int max, out bool directionPositive)
        {
            var span = max - min;
            if (span < 20)
            {
                directionPositive = false;
                return false;
            }

            var distanceToMin = rest - min;
            var distanceToMax = max - rest;
            var edgeThreshold = System.Math.Max(4, span / 4);
            if (distanceToMin <= edgeThreshold && distanceToMax > edgeThreshold)
            {
                directionPositive = true;
                return true;
            }

            if (distanceToMax <= edgeThreshold && distanceToMin > edgeThreshold)
            {
                directionPositive = false;
                return true;
            }

            directionPositive = false;
            return false;
        }
    }
}


