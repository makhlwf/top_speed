using System;
using TopSpeed.Physics.Powertrain;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private int CalculateAcceleration()
        {
            var driveGear = GetDriveGear();
            var gearRange = _engine.GetGearRangeKmh(driveGear);
            var gearMin = _engine.GetGearMinSpeedKmh(driveGear);
            var gearCenter = gearMin + (gearRange * 0.18f);
            _speedDiff = _speed - gearCenter;
            var relSpeedDiff = _speedDiff / gearRange;
            if (Math.Abs(relSpeedDiff) < 1.9f)
            {
                var acceleration = (int)(100.0f * (0.5f + Math.Cos(relSpeedDiff * Math.PI * 0.5f)));
                return acceleration < 5 ? 5 : acceleration;
            }

            {
                var acceleration = (int)(100.0f * (0.5f + Math.Cos(0.95f * Math.PI)));
                return acceleration < 5 ? 5 : acceleration;
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private float ResolveForwardSafetySpeedKph()
        {
            var referenceTopSpeed = Math.Max(1f, _topSpeed);
            var scaledSafetySpeed = referenceTopSpeed * 1.08f;
            var safetySpeed = Math.Min(550f, scaledSafetySpeed);
            return Math.Max(5f, safetySpeed);
        }

        private static float ClampRatio(float value, float max)
        {
            if (value <= 0f)
                return 0f;
            if (value >= max)
                return max;
            return value;
        }

        private float NormalizeSpeedByTopSpeed(float speedKph, float maxRatio = 1f)
        {
            var referenceTopSpeed = Math.Max(1f, _topSpeed);
            var ratio = speedKph / referenceTopSpeed;
            return ClampRatio(ratio, Math.Max(0f, maxRatio));
        }

        private static float SanitizeFinite(float value, float fallback)
        {
            return IsFinite(value) ? value : fallback;
        }

        private float CalculateBrakeDecel(float brakeInput, float surfaceDecelMod)
        {
            return Calculator.BrakeDecelKph(
                _powertrainConfiguration,
                brakeInput,
                surfaceDecelMod);
        }

        private float CalculateEngineBrakingDecel(float surfaceDecelMod)
        {
            if (IsNeutralGear() || _drivelineCouplingFactor <= 0.05f)
                return 0f;

            return Calculator.EngineBrakeDecelKph(
                _powertrainConfiguration,
                GetDriveGear(),
                _gear == ReverseGear,
                _speed / 3.6f,
                surfaceDecelMod,
                _engine.Rpm,
                _effectiveDriveRatioOverride > 0f ? _effectiveDriveRatioOverride : (float?)null);
        }

        private float GetLapStartPosition(float position)
        {
            var lapLength = _track.Length;
            if (lapLength <= 0f)
                return 0f;
            var lapIndex = (float)Math.Floor(position / lapLength);
            if (lapIndex < 0f)
                lapIndex = 0f;
            return lapIndex * lapLength;
        }

        private int GetDriveGear()
        {
            return _gear < FirstForwardGear ? FirstForwardGear : _gear;
        }

        private bool IsNeutralGear()
        {
            return _gear == NeutralGear;
        }
    }
}


