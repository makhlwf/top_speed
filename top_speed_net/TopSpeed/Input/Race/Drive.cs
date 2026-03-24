using SharpDX.DirectInput;
using System;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        public int GetSteering()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var joystickSteer = 0;
            if (UseJoystick)
            {
                var left = ApplySteeringDeadZone(GetAxis(_left));
                var right = ApplySteeringDeadZone(GetAxis(_right));
                joystickSteer = left != 0 ? -left : right;
            }

            if (!UseKeyboard)
                return joystickSteer;

            var keyboardSteer = _settings.KeyboardProgressiveRate == KeyboardProgressiveRate.Off
                ? (_lastState.IsDown(_kbLeft) ? -100 : (_lastState.IsDown(_kbRight) ? 100 : 0))
                : (int)(_simSteer * 100f);

            return Math.Abs(keyboardSteer) > Math.Abs(joystickSteer) ? keyboardSteer : joystickSteer;
        }

        public int GetThrottle()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var joystickThrottle = UseJoystick ? GetPedalAxis(_throttle, _settings.JoystickThrottleInvertMode) : 0;
            if (!UseKeyboard)
                return joystickThrottle;

            var keyboardThrottle = _settings.KeyboardProgressiveRate == KeyboardProgressiveRate.Off
                ? (_lastState.IsDown(_kbThrottle) ? 100 : 0)
                : (int)(_simThrottle * 100f);

            return Math.Max(joystickThrottle, keyboardThrottle);
        }

        public int GetBrake()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var joystickBrake = UseJoystick ? -GetPedalAxis(_brake, _settings.JoystickBrakeInvertMode) : 0;
            if (!UseKeyboard)
                return joystickBrake;

            var keyboardBrake = _settings.KeyboardProgressiveRate == KeyboardProgressiveRate.Off
                ? (_lastState.IsDown(_kbBrake) ? -100 : 0)
                : (int)(_simBrake * -100f);

            return Math.Min(joystickBrake, keyboardBrake);
        }

        public int GetClutch()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var joystickClutch = UseJoystick ? GetAxis(_clutch) : 0;
            if (!UseKeyboard)
                return joystickClutch;

            var keyboardClutch = (int)Math.Round(_simClutch * 100f);
            return Math.Max(joystickClutch, keyboardClutch);
        }

        private int ApplySteeringDeadZone(int value)
        {
            var deadZone = _settings.JoystickSteeringDeadZone;
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
