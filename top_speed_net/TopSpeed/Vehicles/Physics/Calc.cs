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

        private void SyncEngineFromSpeed(float elapsed)
        {
            if (_engineStalled)
            {
                _engine.UpdateKinematicsOnly(_speed, elapsed);
                _engine.StopEngine();
                return;
            }

            var driveRatioOverride = _effectiveDriveRatioOverride > 0f ? _effectiveDriveRatioOverride : (float?)null;
            var syncState = EngineStateRuntime.Resolve(
                new EngineStateRuntimeInput(
                    _powertrainConfiguration,
                    EffectiveTransmissionType(),
                    IsNeutralGear(),
                    _engineStalled,
                    _drivelineState == DrivelineState.Locked,
                    _drivelineState == DrivelineState.Disengaged,
                    _speed / 3.6f,
                    Math.Max(0f, Math.Min(100f, _currentThrottle)) / 100f,
                    _drivelineCouplingFactor,
                    _switchingGear,
                    _engine.Rpm,
                    ResolveCoupledDriveRpm()));
            var couplingMode = (EngineCouplingMode)syncState.CouplingMode;

            _engine.SyncFromSpeed(
                _speed,
                GetDriveGear(),
                elapsed,
                _currentThrottle,
                _gear == ReverseGear,
                _reverseGearRatio,
                couplingMode,
                _drivelineCouplingFactor,
                driveRatioOverride,
                syncState.MinimumCoupledRpm);
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



