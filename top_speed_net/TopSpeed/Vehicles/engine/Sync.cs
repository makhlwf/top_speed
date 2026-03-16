using System;
using TopSpeed.Physics.Powertrain;

namespace TopSpeed.Vehicles
{
    internal sealed partial class EngineModel
    {
        public void SyncFromSpeed(float speedGameUnits, int gear, float elapsed, int throttleInput = 0, bool inReverse = false, float reverseGearRatio = 3.2f)
        {
            const float IdleControlRpmWindow = 150f;
            const float IdleGovernorTorqueGainNmPerRpm = 0.08f;

            var clampedGear = Math.Max(1, Math.Min(_gearCount, gear));
            var throttle = Math.Max(0, throttleInput) / 100f;
            var speedMps = speedGameUnits / 3.6f;
            var wheelCircumference = _tireCircumferenceM;
            var gearRatio = inReverse ? Math.Max(0.1f, reverseGearRatio) : _gearRatios[clampedGear - 1];
            var coupledRpm = wheelCircumference > 0f
                ? (speedMps / wheelCircumference) * 60f * gearRatio * _finalDriveRatio
                : _idleRpm;
            coupledRpm = Math.Max(_idleRpm, Math.Min(_revLimiter, coupledRpm));

            var baseRpm = _rpm > 0f ? _rpm : coupledRpm;
            var clampedBaseRpm = Math.Max(_idleRpm, Math.Min(_revLimiter, baseRpm));
            var torqueAvailable = _torqueCurve.EvaluateTorque(clampedBaseRpm);
            var maximumEngineTorque = torqueAvailable * _powerFactor;
            var requestedEngineTorque = maximumEngineTorque * throttle;
            var grossEngineTorque = requestedEngineTorque;

            var parasiticFrictionTorque = _engineFrictionTorqueNm;
            var idleControlActive = throttle <= 0.10f && clampedBaseRpm <= _idleRpm + IdleControlRpmWindow;
            if (idleControlActive)
            {
                var idleRpmDeficit = Math.Max(0f, _idleRpm - clampedBaseRpm);
                var idleTargetTorque = parasiticFrictionTorque + (idleRpmDeficit * IdleGovernorTorqueGainNmPerRpm);
                var idleCompensationTorque = Math.Min(maximumEngineTorque, idleTargetTorque);
                if (grossEngineTorque < idleCompensationTorque)
                    grossEngineTorque = idleCompensationTorque;
            }

            var rpmRange = Math.Max(1f, _revLimiter - _idleRpm);
            var rpmFactor = Math.Max(0f, Math.Min(1f, (clampedBaseRpm - _idleRpm) / rpmRange));
            var lossTorque = parasiticFrictionTorque;
            if (throttle <= 0.1f)
                lossTorque += _engineBrakingTorqueNm * _engineBraking * rpmFactor;

            var netEngineTorque = grossEngineTorque - lossTorque;
            var rpmPerSecond = (netEngineTorque / _engineInertiaKgm2) * (60f / (2f * (float)Math.PI));
            var torqueIntegratedRpm = clampedBaseRpm + (rpmPerSecond * elapsed);
            torqueIntegratedRpm = Math.Max(_idleRpm, Math.Min(_maxRpm, torqueIntegratedRpm));

            var launchTargetRpm = _idleRpm + (throttle * (_revLimiter - _idleRpm) * 0.35f);
            if (torqueIntegratedRpm < launchTargetRpm)
                torqueIntegratedRpm = launchTargetRpm;

            var couplingAlpha = Math.Max(0f, Math.Min(1f, _drivelineCouplingRate * elapsed));
            var blendedRpm = torqueIntegratedRpm + ((coupledRpm - torqueIntegratedRpm) * couplingAlpha);
            _rpm = Math.Max(_idleRpm, Math.Min(_maxRpm, blendedRpm));
            if (_rpm > _revLimiter)
                _rpm = _revLimiter;

            _grossHorsepower = Calculator.Horsepower(Math.Max(0f, grossEngineTorque), _rpm);
            _netHorsepower = Calculator.Horsepower(Math.Max(0f, netEngineTorque), _rpm);

            _distanceMeters += speedMps * elapsed;
            _speedMps = speedMps;
        }
    }
}


