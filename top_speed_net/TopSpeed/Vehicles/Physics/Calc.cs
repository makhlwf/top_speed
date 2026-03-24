using System;
using TopSpeed.Common;
using TopSpeed.Physics.Surface;
using TopSpeed.Physics.Powertrain;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private void GuardDynamicInputs()
        {
            if (!IsFinite(_speed))
                _speed = 0f;
            if (!IsFinite(_positionX))
                _positionX = 0f;
            if (!IsFinite(_positionY))
                _positionY = 0f;
            if (_positionY < 0f)
                _positionY = 0f;
        }

        private void ApplySurfaceModifiers()
        {
            var modifiers = SurfaceModel.Resolve(_surface, _surfaceTractionFactor, _deceleration);
            _currentSurfaceTractionFactor = modifiers.Traction;
            _currentDeceleration = modifiers.Deceleration;
            _currentSurfaceLateralMultiplier = modifiers.LateralSpeedMultiplier;
            _speedDiff = 0f;
        }

        private int ResolveThrust()
        {
            if (_currentThrottle == 0)
                return _currentBrake;
            if (_currentBrake == 0)
                return _currentThrottle;
            return -_currentBrake > _currentThrottle ? _currentBrake : _currentThrottle;
        }

        private void ApplyThrottleDrive(
            float elapsed,
            float speedMpsCurrent,
            float throttle,
            bool inReverse,
            bool reverseBlockedAtLapStart,
            float surfaceTractionMod,
            float drivelineCouplingFactor,
            ref float longitudinalGripFactor)
        {
            if (reverseBlockedAtLapStart)
            {
                _speedDiff = 0f;
                _lastDriveRpm = 0f;
                return;
            }

            var tireOutput = SolveTireModel(elapsed, speedMpsCurrent, _currentSteering, surfaceTractionMod, 1f, commitState: false);
            longitudinalGripFactor = tireOutput.LongitudinalGripFactor;

            var accelMps2 = inReverse
                ? Calculator.ReverseAccel(
                    _powertrainConfiguration,
                    speedMpsCurrent,
                    throttle,
                    surfaceTractionMod,
                    longitudinalGripFactor)
                : Calculator.DriveAccel(
                    _powertrainConfiguration,
                    GetDriveGear(),
                    speedMpsCurrent,
                    throttle,
                    surfaceTractionMod,
                    longitudinalGripFactor,
                    _effectiveDriveRatioOverride > 0f ? _effectiveDriveRatioOverride : (float?)null);
            accelMps2 *= Math.Max(0f, Math.Min(1f, drivelineCouplingFactor));
            accelMps2 *= (_factor1 / 100f);
            var newSpeedMps = speedMpsCurrent + (accelMps2 * elapsed);
            if (newSpeedMps < 0f)
                newSpeedMps = 0f;

            _speedDiff = (newSpeedMps - speedMpsCurrent) * 3.6f;
            var wheelCircumference = _wheelRadiusM * 2.0f * (float)Math.PI;
            if (wheelCircumference > 0f)
            {
                var gearRatio = inReverse
                    ? _reverseGearRatio
                    : (_effectiveDriveRatioOverride > 0f ? _effectiveDriveRatioOverride : _engine.GetGearRatio(GetDriveGear()));
                var coupledRpm = (newSpeedMps / wheelCircumference) * 60f * gearRatio * _finalDriveRatio;
                _lastDriveRpm = Math.Max(_idleRpm, Math.Min(_revLimiter, coupledRpm));
            }
            else
            {
                _lastDriveRpm = _idleRpm;
            }
            if (_backfirePlayed)
                _backfirePlayed = false;
        }

        private void ApplyCoastDecel(float elapsed)
        {
            var surfaceDecelMod = _deceleration > 0f ? _currentDeceleration / _deceleration : 1.0f;
            var brakeInput = Math.Max(0f, Math.Min(100f, -_currentBrake)) / 100f;
            var brakeDecel = CalculateBrakeDecel(brakeInput, surfaceDecelMod);
            var engineBrakeDecel = CalculateEngineBrakingDecel(surfaceDecelMod);
            var totalDecel = _thrust < -10 ? (brakeDecel + engineBrakeDecel) : engineBrakeDecel;
            _speedDiff = -totalDecel * elapsed;
            if (_automaticCreepAccelMps2 > 0f)
                _speedDiff += _automaticCreepAccelMps2 * elapsed * 3.6f;
            _lastDriveRpm = 0f;
        }

        private void ClampSpeedAndTransmission(
            float elapsed,
            float throttle,
            bool inReverse,
            bool reverseBlockedAtLapStart,
            float surfaceTractionMod,
            float longitudinalGripFactor)
        {
            _speed += _speedDiff;
            if (!inReverse)
            {
                var safetySpeed = ResolveForwardSafetySpeedKph();
                if (_speed > safetySpeed)
                    _speed = safetySpeed;
            }
            if (_speed < 0f)
                _speed = 0f;
            if (!IsFinite(_speed))
            {
                _speed = 0f;
                _speedDiff = 0f;
            }

            if (!IsFinite(_lastDriveRpm))
                _lastDriveRpm = _idleRpm;

            if (reverseBlockedAtLapStart && _thrust > 10f)
            {
                _speed = 0f;
                _speedDiff = 0f;
                _lastDriveRpm = 0f;
            }

            if (inReverse)
            {
                var reverseMax = Math.Max(5.0f, _reverseMaxSpeedKph);
                if (_speed > reverseMax)
                    _speed = reverseMax;
                return;
            }

            if (_manualTransmission)
            {
                if (_gear >= FirstForwardGear)
                {
                    var gearMax = _engine.GetGearMaxSpeedKmh(_gear);
                    if (_speed > gearMax)
                        _speed = gearMax;
                }
            }
            else
            {
                UpdateAutomaticGear(elapsed, _speed / 3.6f, throttle, surfaceTractionMod, longitudinalGripFactor);
            }
        }

        private void SyncEngineFromSpeed(float elapsed)
        {
            if (_engineStalled)
            {
                _engine.UpdateKinematicsOnly(_speed, elapsed);
                _engine.StopEngine();
                return;
            }

            var couplingMode = ResolveEngineCouplingMode();
            _engine.SyncFromSpeed(
                _speed,
                GetDriveGear(),
                elapsed,
                _currentThrottle,
                _gear == ReverseGear,
                _reverseGearRatio,
                couplingMode,
                _drivelineCouplingFactor,
                _effectiveDriveRatioOverride > 0f ? _effectiveDriveRatioOverride : (float?)null);
            if (couplingMode == EngineCouplingMode.Blended
                && _switchingGear == 0
                && _drivelineCouplingFactor > 0.65f
                && _lastDriveRpm > 0f
                && _lastDriveRpm > _engine.Rpm)
                _engine.OverrideRpm(_lastDriveRpm);
        }

        private float ResolveCoupledDriveRpm()
        {
            var wheelCircumference = _wheelRadiusM * 2.0f * (float)Math.PI;
            if (wheelCircumference <= 0.001f)
                return _idleRpm;

            var speedMps = _speed / 3.6f;
            var gearRatio = _gear == ReverseGear
                ? _reverseGearRatio
                : (_effectiveDriveRatioOverride > 0f ? _effectiveDriveRatioOverride : _engine.GetGearRatio(GetDriveGear()));
            var coupledRpm = (speedMps / wheelCircumference) * 60f * gearRatio * _finalDriveRatio;
            return Math.Max(_idleRpm, Math.Min(_revLimiter, coupledRpm));
        }

        private EngineCouplingMode ResolveEngineCouplingMode()
        {
            var type = EffectiveTransmissionType();
            if (TransmissionTypes.IsAutomaticFamily(type))
            {
                if (_drivelineState == DrivelineState.Disengaged)
                    return EngineCouplingMode.Disengaged;

                if (type == TransmissionType.Cvt)
                    return EngineCouplingMode.Blended;

                if (type == TransmissionType.Atc)
                {
                    var throttle = Math.Max(0f, Math.Min(100f, _currentThrottle)) / 100f;
                    var lockEligible = _switchingGear == 0
                        && _speed >= _automaticTuning.Atc.LockSpeedKph
                        && throttle >= _automaticTuning.Atc.LockThrottleMin
                        && _drivelineCouplingFactor >= 0.93f;
                    if (!lockEligible)
                        return EngineCouplingMode.Blended;

                    var coupledRpm = ResolveCoupledDriveRpm();
                    var slipRpm = Math.Abs(coupledRpm - _engine.Rpm);
                    var lockSlipWindowRpm = Math.Max(300f, (_revLimiter - _idleRpm) * 0.07f);
                    if (slipRpm > lockSlipWindowRpm)
                        return EngineCouplingMode.Blended;
                }
            }

            switch (_drivelineState)
            {
                case DrivelineState.Locked:
                    return EngineCouplingMode.Locked;
                case DrivelineState.Disengaged:
                    return EngineCouplingMode.Disengaged;
                default:
                    return EngineCouplingMode.Blended;
            }
        }

        private void UpdateBackfireStateAfterDrive()
        {
            if (_thrust > 0)
                return;

            if (!AnyBackfirePlaying() && !_backfirePlayed && Algorithm.RandomInt(5) == 1)
                PlayRandomBackfire();
            _backfirePlayed = true;
        }

        private void IntegrateVehiclePosition(float elapsed, float currentLapStart)
        {
            var speedMps = _speed / 3.6f;
            var longitudinalDelta = speedMps * elapsed;
            if (_gear == ReverseGear)
            {
                var nextPositionY = _positionY - longitudinalDelta;
                if (nextPositionY < currentLapStart)
                    nextPositionY = currentLapStart;
                if (nextPositionY < 0f)
                    nextPositionY = 0f;
                _positionY = nextPositionY;
            }
            else
            {
                _positionY += longitudinalDelta;
            }

            var surfaceTractionModLat = _surfaceTractionFactor > 0f ? _currentSurfaceTractionFactor / _surfaceTractionFactor : 1.0f;
            var tireOutput = SolveTireModel(elapsed, speedMps, _currentSteering, surfaceTractionModLat, _currentSurfaceLateralMultiplier);
            _positionX += tireOutput.LateralSpeedMps * elapsed;
        }
    }
}


