using System;
using TopSpeed.Drive.Session;
using TopSpeed.Localization;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        public void Run(float elapsed)
        {
            RefreshCategoryVolumes();
            if (!_hostPaused && _currentState == Protocol.PlayerState.AwaitingStart && _car.EngineRunning && _car.State == Vehicles.CarState.Running)
                _currentState = Protocol.PlayerState.Racing;

            _session.Update(elapsed);
        }

        private void HandleSessionEvent(SessionContext context, Event sessionEvent)
        {
            switch (sessionEvent.Id)
            {
                case Events.VehicleStart:
                    break;
                case Events.ProgressStart:
                    _session.SetPhase(Phase.Running);
                    _raceTime = 0;
                    _lap = 0;
                    _started = true;
                    OnRaceStartEvent();
                    break;
                case Events.PlaySound:
                    QueueSound(sessionEvent.Data as TS.Audio.Source);
                    break;
                case Events.PlayUnkey:
                    _unkeyQueue--;
                    if (_unkeyQueue == 0)
                        Speak(_soundUnkey[TopSpeed.Common.Algorithm.RandomInt(MaxUnkeys)]);
                    break;
            }
        }

        private void HandlePhaseEvent(SessionContext context, Event sessionEvent)
        {
            if (sessionEvent.Id != Events.PhaseChanged || sessionEvent.Data is not PhaseChanged phaseChanged)
                return;

            _panels.ApplyInputPolicy(context.InputPolicy);
            if (phaseChanged.Current == Phase.Paused)
            {
                _localRadio.PauseForGame();
                _liveTx.Pause();
                _soundPause?.Play(loop: false);
                return;
            }

            if (phaseChanged.Previous == Phase.Paused)
            {
                _localRadio.ResumeFromGame();
                _liveTx.Resume();
                _soundResume?.Play(loop: false);
            }
        }

        private void OnRaceStartEvent()
        {
            if (_sentStart)
                return;

            _sentStart = true;
            _currentState = Protocol.PlayerState.Racing;
            TrySendRace(_network.SendPlayerStarted(_raceInstanceId));
            SendPlayerState(sendStarted: false);
        }

        private void RefreshCategoryVolumes()
        {
            var ambientPercent = _settings.AudioVolumes?.AmbientsAndSourcesPercent ?? 100;
            _track.SetAmbientVolumePercent(ambientPercent);
        }

        private void TrackLocalCrashState()
        {
            var currentState = _car.State;
            var wasCrashing = _lastRecordedCarState == Vehicles.CarState.Crashing || _lastRecordedCarState == Vehicles.CarState.Crashed;
            var isCrashing = currentState == Vehicles.CarState.Crashing || currentState == Vehicles.CarState.Crashed;
            if (!wasCrashing && isCrashing)
                _localCrashCount++;

            _lastRecordedCarState = currentState;
        }

        private void ApplyPlayerFinishState()
        {
            _finished = true;
            var finishSounds = _randomSounds[(int)RandomSoundSlot.Finish];
            var finishSoundCount = _totalRandomSounds[(int)RandomSoundSlot.Finish];
            if (finishSoundCount > 0)
            {
                var finishSound = finishSounds[TopSpeed.Common.Algorithm.RandomInt(finishSoundCount)];
                if (finishSound != null)
                    Speak(finishSound, true);
            }

            _car.ManualTransmission = false;
            _car.SetOverrideController(_finishLockController);
            _car.SetNeutralGear();
            _car.Quiet();
            _car.ShutdownEngine();
            _car.StopMotionImmediately();
            _raceTime = Math.Max(0, _session.Context.ProgressMilliseconds);
            _requirePostFinishStopBeforeExit = true;
        }

        private float GetSpatialTrackLength()
        {
            if (_track.Length <= 0f)
                return 0f;

            return _track.Length * Math.Max(1, _lapLimit);
        }
    }
}
