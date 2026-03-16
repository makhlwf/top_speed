using System;
using TopSpeed.Bots;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Vehicles.Events;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private float CalculateDriveRpm(float speedMps, float throttle)
        {
            return Calculator.DriveRpm(
                _powertrainConfiguration,
                GetDriveGear(),
                speedMps,
                throttle,
                _gear == ReverseGear);
        }

        private void UpdateAutomaticGear(float elapsed, float speedMps, float throttle, float surfaceTractionMod, float longitudinalGripFactor)
        {
            if (_gears <= 1)
                return;

            if (_autoShiftCooldown > 0f)
            {
                _autoShiftCooldown -= elapsed;
                return;
            }

            var currentAccel = ComputeNetAccelForGear(_gear, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor);
            var currentRpm = SpeedToRpm(speedMps, _gear);
            var upAccel = _gear < _gears
                ? ComputeNetAccelForGear(_gear + 1, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor)
                : float.NegativeInfinity;
            var downAccel = _gear > 1
                ? ComputeNetAccelForGear(_gear - 1, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor)
                : float.NegativeInfinity;

            var decision = AutomaticTransmissionLogic.Decide(
                new AutomaticShiftInput(
                    _gear,
                    _gears,
                    speedMps,
                    _topSpeed / 3.6f,
                    _idleRpm,
                    _revLimiter,
                    currentRpm,
                    currentAccel,
                    upAccel,
                    downAccel),
                _transmissionPolicy);

            if (decision.Changed)
                ShiftAutomaticGear(decision.NewGear, decision.CooldownSeconds);
        }

        private void ShiftAutomaticGear(int newGear, float cooldownSeconds)
        {
            if (newGear == _gear)
                return;
            var upshift = newGear > _gear;
            _switchingGear = upshift ? 1 : -1;
            _gear = newGear;
            var inGearDelay = upshift ? Math.Max(0.2f, cooldownSeconds) : 0.2f;
            PushEvent(EventType.InGear, inGearDelay);
            _autoShiftCooldown = Math.Max(0f, cooldownSeconds);
        }

        private float ComputeNetAccelForGear(int gear, float speedMps, float throttle, float surfaceTractionMod, float longitudinalGripFactor)
        {
            var rpm = SpeedToRpm(speedMps, gear);
            if (rpm <= 0f)
                return float.NegativeInfinity;
            if (rpm > _revLimiter && gear < _gears)
                return float.NegativeInfinity;
            return Calculator.DriveAccel(
                _powertrainConfiguration,
                gear,
                speedMps,
                throttle,
                surfaceTractionMod,
                longitudinalGripFactor);
        }

        private float SpeedToRpm(float speedMps, int gear)
        {
            return Calculator.RpmAtSpeed(_powertrainConfiguration, speedMps, gear);
        }

        private float CalculateEngineTorqueNm(float rpm)
        {
            return Calculator.EngineTorque(_powertrainConfiguration, rpm);
        }

        private static float SmoothStep(float a, float b, float t)
        {
            var clamped = Math.Max(0f, Math.Min(1f, t));
            clamped = clamped * clamped * (3f - 2f * clamped);
            return a + (b - a) * clamped;
        }
    }
}


