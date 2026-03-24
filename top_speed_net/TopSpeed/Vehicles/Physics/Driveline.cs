using System;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private const float StallSpeedThresholdKph = 8f;
        private const float StallDemandRpmFraction = 0.55f;
        private const float StallCouplingThreshold = 0.75f;
        private const float StallDelaySeconds = 0.25f;

        private float UpdateDriveline(float elapsed, float speedMps, float throttle, bool inReverse, int clutchInput)
        {
            var type = EffectiveTransmissionType();
            if (TransmissionTypes.IsAutomaticFamily(type))
            {
                if (IsNeutralGear())
                {
                    _drivelineCouplingFactor = 0f;
                    _drivelineState = DrivelineState.Disengaged;
                    _effectiveDriveRatioOverride = 0f;
                    _automaticCreepAccelMps2 = 0f;
                    return _drivelineCouplingFactor;
                }

                UpdateAutomaticDriveline(type, elapsed, speedMps, throttle, inReverse);
                return _drivelineCouplingFactor;
            }

            if (_engineStalled)
            {
                _drivelineCouplingFactor = 0f;
                _drivelineState = DrivelineState.Disengaged;
                _effectiveDriveRatioOverride = 0f;
                _automaticCreepAccelMps2 = 0f;
                return _drivelineCouplingFactor;
            }

            if (IsNeutralGear())
            {
                _drivelineCouplingFactor = 0f;
                _drivelineState = DrivelineState.Disengaged;
                _effectiveDriveRatioOverride = 0f;
                _automaticCreepAccelMps2 = 0f;
                return _drivelineCouplingFactor;
            }

            _effectiveDriveRatioOverride = 0f;
            _automaticCreepAccelMps2 = 0f;
            var clutch = Math.Max(0f, Math.Min(100f, clutchInput)) / 100f;
            _drivelineCouplingFactor = _switchingGear != 0 ? 0f : 1f - clutch;
            if (_drivelineCouplingFactor <= 0.05f)
                _drivelineState = DrivelineState.Disengaged;
            else if (_drivelineCouplingFactor >= 0.98f)
                _drivelineState = DrivelineState.Locked;
            else
                _drivelineState = DrivelineState.Slipping;

            return _drivelineCouplingFactor;
        }

        private void UpdateAutomaticDriveline(TransmissionType type, float elapsed, float speedMps, float throttle, bool inReverse)
        {
            if (_engineStalled)
            {
                _drivelineCouplingFactor = 0f;
                _drivelineState = DrivelineState.Disengaged;
                _automaticCreepAccelMps2 = 0f;
                _effectiveDriveRatioOverride = 0f;
                return;
            }

            var brake = Math.Max(0f, Math.Min(100f, -_currentBrake)) / 100f;
            var output = AutomaticDrivelineModel.Step(
                type,
                _automaticTuning,
                new AutomaticDrivelineInput(
                    elapsed,
                    speedMps,
                    throttle,
                    brake,
                    shifting: _switchingGear != 0,
                    wheelCircumferenceM: _wheelRadiusM * 2f * (float)Math.PI,
                    finalDriveRatio: _finalDriveRatio,
                    idleRpm: _idleRpm,
                    revLimiter: _revLimiter),
                new AutomaticDrivelineState(_drivelineCouplingFactor, _cvtRatio));

            _drivelineCouplingFactor = output.CouplingFactor;
            _cvtRatio = output.CvtRatio > 0f ? output.CvtRatio : _cvtRatio;
            _effectiveDriveRatioOverride = inReverse ? 0f : output.EffectiveDriveRatio;
            _automaticCreepAccelMps2 = inReverse ? 0f : output.CreepAccelerationMps2;

            if (_drivelineCouplingFactor <= 0.05f)
                _drivelineState = DrivelineState.Disengaged;
            else if (_drivelineCouplingFactor >= 0.98f)
                _drivelineState = DrivelineState.Locked;
            else
                _drivelineState = DrivelineState.Slipping;
        }

        private TransmissionType EffectiveTransmissionType()
        {
            return _activeTransmissionType;
        }

        private void UpdateStallState(float elapsed, float speedMps, float throttle, int clutchInput)
        {
            if (_engineStalled)
                return;

            var type = EffectiveTransmissionType();
            if (type != TransmissionType.Manual || _switchingGear != 0)
            {
                _stallTimer = 0f;
                return;
            }
            if (IsNeutralGear())
            {
                _stallTimer = 0f;
                return;
            }

            var lowSpeed = _speed <= StallSpeedThresholdKph;
            var engagedEnough = _drivelineCouplingFactor >= StallCouplingThreshold;
            var clutchDown = clutchInput >= 90;
            var insufficientThrottle = throttle < 0.20f;
            var highLoadGear = _gear > FirstForwardGear;
            var reverseLoad = _gear == ReverseGear && throttle < 0.15f;
            if (!lowSpeed || !engagedEnough || clutchDown || (!insufficientThrottle && !highLoadGear && !reverseLoad))
            {
                _stallTimer = 0f;
                return;
            }

            var coupledDemandRpm = ComputeRawCoupledRpm(speedMps, inReverse: _gear == ReverseGear);
            var stallThresholdRpm = _idleRpm * StallDemandRpmFraction;
            if (coupledDemandRpm >= stallThresholdRpm)
            {
                _stallTimer = 0f;
                return;
            }

            _stallTimer += elapsed;
            if (_stallTimer >= StallDelaySeconds)
                StallEngine();
        }

        private float ComputeRawCoupledRpm(float speedMps, bool inReverse)
        {
            var wheelCircumference = _wheelRadiusM * 2.0f * (float)Math.PI;
            if (wheelCircumference <= 0.001f)
                return 0f;

            var gearRatio = inReverse ? _reverseGearRatio : _engine.GetGearRatio(GetDriveGear());
            return (speedMps / wheelCircumference) * 60f * gearRatio * _finalDriveRatio;
        }

        private void StallEngine()
        {
            _engineStalled = true;
            _stallTimer = 0f;
            _drivelineCouplingFactor = 0f;
            _drivelineState = DrivelineState.Disengaged;
            _effectiveDriveRatioOverride = 0f;
            _automaticCreepAccelMps2 = 0f;
            _engine.StopEngine();

            if (_soundEngine.IsPlaying)
                _soundEngine.Stop();
            if (_soundThrottle != null && _soundThrottle.IsPlaying)
                _soundThrottle.Stop();

            _vibration?.StopEffect(VibrationEffectType.Engine);
            _soundBadSwitch.Play(loop: false);
        }

        private void ClearStallState()
        {
            _engineStalled = false;
            _stallTimer = 0f;
            _drivelineCouplingFactor = 1f;
            _drivelineState = DrivelineState.Locked;
            _cvtRatio = _automaticTuning.Cvt.RatioMax;
            _effectiveDriveRatioOverride = 0f;
            _automaticCreepAccelMps2 = 0f;
        }

        private void ApplyStalledDecel(float elapsed)
        {
            var surfaceDecelMod = _deceleration > 0f ? _currentDeceleration / _deceleration : 1.0f;
            var brakeInput = Math.Max(0f, Math.Min(100f, -_currentBrake)) / 100f;
            var brakeDecel = CalculateBrakeDecel(brakeInput, surfaceDecelMod);
            var rollingDecel = Math.Max(0.1f, _currentDeceleration * 0.35f);
            _speedDiff = -(brakeDecel + rollingDecel) * elapsed;
            _lastDriveRpm = 0f;
        }
    }
}
