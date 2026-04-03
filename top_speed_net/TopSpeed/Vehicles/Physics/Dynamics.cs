using System;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Vehicles.Control;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        internal void RunDynamics(float elapsed, in CarControlIntent controlIntent)
        {
            if (_state == CarState.Running && _started())
                RunRunningDynamics(elapsed, controlIntent);
            else if (_state == CarState.Stopping)
                RunStoppingDynamics(elapsed);
        }

        private void RunRunningDynamics(float elapsed, in CarControlIntent controlIntent)
        {
            GuardDynamicInputs();

            _currentSteering = controlIntent.Steering;
            _currentThrottle = controlIntent.Throttle;
            _currentBrake = controlIntent.Brake;
            var clutchInput = controlIntent.Clutch;

            ApplySurfaceModifiers();
            _factor1 = 100;
            HandleTransmissionInput(controlIntent);
            UpdateThrottleLoopAudio(elapsed);

            _thrust = ResolveThrust();
            var speedMpsCurrent = _speed / 3.6f;
            var throttle = Math.Max(0f, Math.Min(100f, _currentThrottle)) / 100f;
            var inReverse = _gear == ReverseGear;
            var currentLapStart = GetLapStartPosition(_positionY);
            var reverseBlockedAtLapStart = inReverse && _positionY <= currentLapStart + 0.001f;
            var surfaceTractionMod = _surfaceTractionFactor > 0f
                ? _currentSurfaceTractionFactor / _surfaceTractionFactor
                : 1.0f;
            var longitudinalGripFactor = 1.0f;
            var drivelineCouplingFactor = UpdateDriveline(elapsed, speedMpsCurrent, throttle, inReverse, clutchInput);

            if (_engineStalled)
            {
                ApplyStalledDecel(elapsed);
            }
            else if (_thrust > 10f)
            {
                ApplyThrottleDrive(
                    elapsed,
                    speedMpsCurrent,
                    throttle,
                    inReverse,
                    reverseBlockedAtLapStart,
                    surfaceTractionMod,
                    drivelineCouplingFactor,
                    ref longitudinalGripFactor);
            }
            else
            {
                ApplyCoastDecel(elapsed);
            }

            ClampSpeedAndTransmission(elapsed, throttle, inReverse, reverseBlockedAtLapStart, surfaceTractionMod, longitudinalGripFactor);
            SyncEngineFromSpeed(elapsed);
            UpdateStallState(elapsed, _speed / 3.6f, throttle, clutchInput);
            UpdateBackfireStateAfterDrive();
            UpdateBrakeAndSteeringOutput();
            IntegrateVehiclePosition(elapsed, currentLapStart);
            UpdateFrameAudioAndFeedback();
            EnsureSurfaceLoopPlaying();
        }

        private void RunStoppingDynamics(float elapsed)
        {
            _currentThrottle = 0;
            _currentBrake = 0;
            _speed -= (elapsed * 100f * _deceleration);
            if (_speed < 0f)
                _speed = 0f;

            if (_engineLifecycleState == EngineLifecycleState.Stopping)
            {
                _engine.StepShutdown(_speed, elapsed);
            }
            else if (_soundEngine.IsPlaying)
            {
                _engine.SyncFromSpeed(
                    _speed,
                    GetDriveGear(),
                    elapsed,
                    throttleInput: 0,
                    inReverse: _gear == ReverseGear,
                    reverseGearRatio: _reverseGearRatio,
                    couplingMode: EngineCouplingMode.Locked,
                    couplingFactor: 1f,
                    driveRatioOverride: _effectiveDriveRatioOverride > 0f ? _effectiveDriveRatioOverride : (float?)null);
            }
            else
            {
                _engine.UpdateKinematicsOnly(_speed, elapsed);
            }

            UpdateEngineFreq();

            if (_engineLifecycleState == EngineLifecycleState.Stopping
                && _speed <= 0.05f
                && _engine.Rpm <= 1f)
            {
                FinalizeEngineShutdown();
                return;
            }

            if (_frame % 4 != 0)
                return;

            _frame = 0;
            UpdateSoundRoad();
        }
    }
}

