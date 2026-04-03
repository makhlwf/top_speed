using System;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        private const float KeyboardClutchRampSeconds = 0.15f;

        private void UpdateSimulatedInputs(float deltaSeconds)
        {
            UpdateSimulatedClutch(deltaSeconds);

            if (_settings.KeyboardProgressiveRate == KeyboardProgressiveRate.Off)
            {
                _simThrottle = _lastState.IsDown(_kbThrottle) ? 1f : 0f;
                _simBrake = _lastState.IsDown(_kbBrake) ? 1f : 0f;
                if (_lastState.IsDown(_kbLeft))
                    _simSteer = -1f;
                else if (_lastState.IsDown(_kbRight))
                    _simSteer = 1f;
                else
                    _simSteer = 0f;
                return;
            }

            if (deltaSeconds <= 0f)
                return;

            var rampSeconds = GetProgressiveRampSeconds(_settings.KeyboardProgressiveRate);
            var delta = deltaSeconds / rampSeconds;

            if (_lastState.IsDown(_kbThrottle))
            {
                _simBrake = 0f;
                _simThrottle = Math.Min(1f, _simThrottle + delta);
            }
            else
            {
                _simThrottle = Math.Max(0f, _simThrottle - delta);
            }

            if (_lastState.IsDown(_kbBrake))
            {
                _simThrottle = 0f;
                _simBrake = Math.Min(1f, _simBrake + delta);
            }
            else
            {
                _simBrake = Math.Max(0f, _simBrake - delta);
            }

            if (_lastState.IsDown(_kbLeft))
            {
                if (_simSteer > 0f)
                    _simSteer = 0f;
                _simSteer = Math.Max(-1f, _simSteer - delta);
            }
            else if (_lastState.IsDown(_kbRight))
            {
                if (_simSteer < 0f)
                    _simSteer = 0f;
                _simSteer = Math.Min(1f, _simSteer + delta);
            }
            else if (_simSteer > 0f)
            {
                _simSteer = Math.Max(0f, _simSteer - delta);
            }
            else if (_simSteer < 0f)
            {
                _simSteer = Math.Min(0f, _simSteer + delta);
            }
        }

        private void UpdateSimulatedClutch(float deltaSeconds)
        {
            var clutchDown = IsClutchKeyDown();
            if (deltaSeconds <= 0f)
            {
                _simClutch = clutchDown ? 1f : 0f;
                return;
            }

            var delta = deltaSeconds / KeyboardClutchRampSeconds;
            if (clutchDown)
                _simClutch = Math.Min(1f, _simClutch + delta);
            else
                _simClutch = Math.Max(0f, _simClutch - delta);
        }

        private static float GetProgressiveRampSeconds(KeyboardProgressiveRate rate)
        {
            switch (rate)
            {
                case KeyboardProgressiveRate.Fastest_0_25s:
                    return 0.25f;
                case KeyboardProgressiveRate.Fast_0_50s:
                    return 0.50f;
                case KeyboardProgressiveRate.Moderate_0_75s:
                    return 0.75f;
                case KeyboardProgressiveRate.Slowest_1_00s:
                    return 1.00f;
                default:
                    return 0.50f;
            }
        }

        private void ResetPedalBaseline()
        {
            _hasPedalBaseline = false;
            _pedalBaseline = default;
        }
    }
}

