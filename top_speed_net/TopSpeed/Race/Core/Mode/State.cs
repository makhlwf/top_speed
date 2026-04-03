using System;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Data;

namespace TopSpeed.Race
{
    internal abstract partial class RaceMode
    {
        public void ClearPauseRequest()
        {
            PauseRequested = false;
        }

        public void StartStopwatchDiff()
        {
            _oldStopwatchMs = _stopwatch.ElapsedMilliseconds;
        }

        public void StopStopwatchDiff()
        {
            var now = _stopwatch.ElapsedMilliseconds;
            _stopwatchDiffMs += (now - _oldStopwatchMs);
        }

        protected void InitializeMode()
        {
            _track.Initialize();
            _car.Initialize();
            _car.SetOverrideController(null);
            _elapsedTotal = 0.0f;
            _oldStopwatchMs = 0;
            _stopwatchDiffMs = 0;
            _started = false;
            _finished = false;
            _engineStarted = false;
            _pendingResultSummary = null;
            _requirePostFinishStopBeforeExit = false;
            _currentRoad.Surface = _track.InitialSurface;
            _lastRoadTypeAtPosition = TrackType.Straight;
            _hasLastRoadTypeAtPosition = false;
            _car.ManualTransmission = _manualTransmission;
            _listenerInitialized = false;
            _lastListenerPosition = Vector3.Zero;
            ApplyActivePanelInputAccess();
            _panelManager.Resume();
        }

        protected void FinalizeMode()
        {
            _panelManager.Pause();
            _car.FinalizeCar();
            _track.FinalizeTrack();
        }

        protected void ApplyPlayerFinishState()
        {
            _finished = true;
            var finishSounds = _randomSounds[(int)RandomSound.Finish];
            var finishSoundCount = _totalRandomSounds[(int)RandomSound.Finish];
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
            _car.Stop();
            _raceTime = (int)(_stopwatch.ElapsedMilliseconds - _stopwatchDiffMs);
            _requirePostFinishStopBeforeExit = true;
        }

        protected void PauseCore(Action? pauseExtras = null)
        {
            _soundTheme4?.SetVolumePercent((int)Math.Round(_settings.MusicVolume * 100f));
            _soundTheme4?.Play(loop: true);
            FadeIn();
            PauseVehiclePanels();
            _car.Pause();
            pauseExtras?.Invoke();
            _soundPause?.Play(loop: false);
        }

        protected void UnpauseCore(Action? unpauseExtras = null)
        {
            _car.Unpause();
            ResumeVehiclePanels();
            unpauseExtras?.Invoke();
            FadeOut();
            _soundTheme4?.Stop();
            _soundTheme4?.SeekToStart();
            _soundUnpause?.Play(loop: false);
        }

        protected void RequestExitWhenQueueIdle()
        {
            _exitWhenQueueIdle = true;
        }

        protected void SetResultSummary(RaceResultSummary summary)
        {
            _pendingResultSummary = summary;
        }

        protected virtual bool AreVehiclesSettledForExit()
        {
            return _car.Speed <= PostFinishStopSpeedKph;
        }

        protected void ScheduleDefaultStartSequence(float raceStartDelaySeconds = DefaultRaceStartDelaySeconds)
        {
            PushEvent(Events.RaceEventType.CarStart, DefaultCarStartDelaySeconds);
            PushEvent(Events.RaceEventType.RaceStart, raceStartDelaySeconds);
            PushEvent(Events.RaceEventType.PlaySound, DefaultStartCueDelaySeconds, _soundStart);
        }

        protected bool UpdateExitWhenQueueIdle()
        {
            if (!_exitWhenQueueIdle)
                return false;
            if (_requirePostFinishStopBeforeExit && !AreVehiclesSettledForExit())
                return false;
            if (!_soundQueue.IsIdle)
                return false;
            ExitRequested = true;
            return true;
        }
    }
}


