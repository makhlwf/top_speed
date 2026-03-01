using System.Collections.Generic;
using SharpDX.DirectInput;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Race;
using TopSpeed.Core;
using TopSpeed.Tracks;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void PrepareQuickStart()
        {
            _setup.Mode = RaceMode.QuickStart;
            _setup.ClearSelection();
            _selection.SelectRandomTrackAny(_settings.RandomCustomTracks);
            _selection.SelectRandomVehicle();
            _setup.Transmission = TransmissionMode.Automatic;
        }

        private void QueueRaceStart(RaceMode mode)
        {
            _pendingRaceStart = true;
            _pendingMode = mode;
        }

        private void RunTimeTrial(float elapsed)
        {
            if (_timeTrial == null)
            {
                EndRace();
                return;
            }

            _timeTrial.Run(elapsed);
            if (_timeTrial.WantsPause)
                EnterPause(AppState.TimeTrial);
            if (_timeTrial.WantsExit || _input.WasPressed(Key.Escape))
                EndRace();
        }

        private void RunSingleRace(float elapsed)
        {
            if (_singleRace == null)
            {
                EndRace();
                return;
            }

            _singleRace.Run(elapsed);
            if (_singleRace.WantsPause)
                EnterPause(AppState.SingleRace);
            if (_singleRace.WantsExit || _input.WasPressed(Key.Escape))
                EndRace();
        }

        private void UpdatePaused()
        {
            if (!_raceInput.GetPause() && !_pauseKeyReleased)
            {
                _pauseKeyReleased = true;
                return;
            }

            if (_raceInput.GetPause() && _pauseKeyReleased)
            {
                _pauseKeyReleased = false;
                switch (_pausedState)
                {
                    case AppState.TimeTrial:
                        _timeTrial?.Unpause();
                        _timeTrial?.StopStopwatchDiff();
                        _state = AppState.TimeTrial;
                        break;
                    case AppState.SingleRace:
                        _singleRace?.Unpause();
                        _singleRace?.StopStopwatchDiff();
                        _state = AppState.SingleRace;
                        break;
                }
            }
        }

        private void EnterPause(AppState state)
        {
            _pausedState = state;
            _pauseKeyReleased = false;
            switch (_pausedState)
            {
                case AppState.TimeTrial:
                    _timeTrial?.StartStopwatchDiff();
                    _timeTrial?.Pause();
                    _timeTrial?.ClearPauseRequest();
                    _state = AppState.Paused;
                    break;
                case AppState.SingleRace:
                    _singleRace?.StartStopwatchDiff();
                    _singleRace?.Pause();
                    _singleRace?.ClearPauseRequest();
                    _state = AppState.Paused;
                    break;
            }
        }

        private void StartRace(RaceMode mode)
        {
            FadeOutMenuMusic();
            var track = string.IsNullOrWhiteSpace(_setup.TrackNameOrFile)
                ? TrackList.RaceTracks[0].Key
                : _setup.TrackNameOrFile!;
            var vehicleIndex = _setup.VehicleIndex ?? 0;
            var vehicleFile = _setup.VehicleFile;
            var automatic = _setup.Transmission == TransmissionMode.Automatic;

            try
            {
                switch (mode)
                {
                    case RaceMode.TimeTrial:
                        _timeTrial?.FinalizeLevelTimeTrial();
                        _timeTrial?.Dispose();
                        _timeTrial = null;

                        var timeTrial = new LevelTimeTrial(
                            _audio,
                            _speech,
                            _settings,
                            _raceInput,
                            track,
                            automatic,
                            _settings.NrOfLaps,
                            vehicleIndex,
                            vehicleFile,
                            _input.VibrationDevice);
                        timeTrial.Initialize();
                        _timeTrial = timeTrial;
                        _state = AppState.TimeTrial;
                        _speech.Speak("Time trial.");
                        break;
                    case RaceMode.QuickStart:
                    case RaceMode.SingleRace:
                        _singleRace?.FinalizeLevelSingleRace();
                        _singleRace?.Dispose();
                        _singleRace = null;

                        var singleRace = new LevelSingleRace(
                            _audio,
                            _speech,
                            _settings,
                            _raceInput,
                            track,
                            automatic,
                            _settings.NrOfLaps,
                            vehicleIndex,
                            vehicleFile,
                            _input.VibrationDevice);
                        singleRace.Initialize(Algorithm.RandomInt(_settings.NrOfComputers + 1));
                        _singleRace = singleRace;
                        _state = AppState.SingleRace;
                        _speech.Speak(mode == RaceMode.QuickStart ? "Quick start." : "Single race.");
                        break;
                }
            }
            catch (TrackLoadException ex)
            {
                HandleTrackLoadFailure(ex);
            }
        }

        private void HandleTrackLoadFailure(TrackLoadException ex)
        {
            _state = AppState.Menu;
            _menu.FadeInMenuMusic(force: true);

            var items = new List<string>();
            if (ex.Details != null && ex.Details.Count > 0)
            {
                for (var i = 0; i < ex.Details.Count; i++)
                    items.Add(ex.Details[i]);
            }
            else
            {
                items.Add(ex.Message);
            }

            ShowMessageDialog(
                "Track load error",
                "The selected track could not be loaded. The race was not started.",
                items);
        }

        private void EndRace()
        {
            _timeTrial?.FinalizeLevelTimeTrial();
            _timeTrial?.Dispose();
            _timeTrial = null;

            _singleRace?.FinalizeLevelSingleRace();
            _singleRace?.Dispose();
            _singleRace = null;

            _state = AppState.Menu;
            _menu.ShowRoot("main");
            _menu.FadeInMenuMusic();
        }

        private void SyncAudioLoopState()
        {
            var shouldRun = IsRaceState(_state);
            if (shouldRun && !_audioLoopActive)
            {
                _audio.StartUpdateThread(8);
                _audioLoopActive = true;
            }
            else if (!shouldRun && _audioLoopActive)
            {
                _audio.StopUpdateThread();
                _audioLoopActive = false;
            }
        }

        private static bool IsRaceState(AppState state)
        {
            return state == AppState.TimeTrial
                || state == AppState.SingleRace
                || state == AppState.MultiplayerRace
                || state == AppState.Paused;
        }

        private static bool IsMenuState(AppState state)
        {
            return state == AppState.Logo
                || state == AppState.Menu
                || state == AppState.Calibration;
        }
    }
}
