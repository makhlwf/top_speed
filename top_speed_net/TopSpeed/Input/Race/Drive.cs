using Key = TopSpeed.Input.InputKey;
using System;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        public int GetSteering()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var controllerSteer = 0;
            if (UseController)
            {
                var left = ApplySteeringDeadZone(GetAxis(_left));
                var right = ApplySteeringDeadZone(GetAxis(_right));
                controllerSteer = left != 0 ? -left : right;
            }

            if (!UseKeyboard)
                return controllerSteer;

            var keyboardSteer = _settings.KeyboardProgressiveRate == KeyboardProgressiveRate.Off
                ? (_lastState.IsDown(_kbLeft) ? -100 : (_lastState.IsDown(_kbRight) ? 100 : 0))
                : (int)(_simSteer * 100f);

            return Math.Abs(keyboardSteer) > Math.Abs(controllerSteer) ? keyboardSteer : controllerSteer;
        }

        public int GetThrottle()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var controllerThrottle = UseController ? GetPedalAxis(_throttle, _settings.ControllerThrottleInvertMode) : 0;
            if (!UseKeyboard)
                return controllerThrottle;

            var keyboardThrottle = _settings.KeyboardProgressiveRate == KeyboardProgressiveRate.Off
                ? (_lastState.IsDown(_kbThrottle) ? 100 : 0)
                : (int)(_simThrottle * 100f);

            return Math.Max(controllerThrottle, keyboardThrottle);
        }

        public int GetBrake()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var controllerBrake = UseController ? -GetPedalAxis(_brake, _settings.ControllerBrakeInvertMode) : 0;
            if (!UseKeyboard)
                return controllerBrake;

            var keyboardBrake = _settings.KeyboardProgressiveRate == KeyboardProgressiveRate.Off
                ? (_lastState.IsDown(_kbBrake) ? -100 : 0)
                : (int)(_simBrake * -100f);

            return Math.Min(controllerBrake, keyboardBrake);
        }

        public int GetClutch()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var controllerClutch = UseController ? GetAxis(_clutch) : 0;
            if (!UseKeyboard)
                return controllerClutch;

            var keyboardClutch = (int)Math.Round(_simClutch * 100f);
            return Math.Max(controllerClutch, keyboardClutch);
        }

        private int ApplySteeringDeadZone(int value)
        {
            var deadZone = _settings.ControllerSteeringDeadZone;
            if (deadZone < 1 || deadZone > 5)
                deadZone = 1;

            return Math.Abs(value) <= deadZone ? 0 : value;
        }

        private bool IsClutchKeyDown()
        {
            if (_kbClutch == Key.LeftShift || _kbClutch == Key.RightShift)
                return _lastState.IsDown(Key.LeftShift) || _lastState.IsDown(Key.RightShift);
            return _lastState.IsDown(_kbClutch);
        }
    }
}


