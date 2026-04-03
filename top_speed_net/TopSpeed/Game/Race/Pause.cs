namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
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
    }
}

