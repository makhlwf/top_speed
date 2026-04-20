using TopSpeed.Input;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void UpdatePaused()
        {
            if (!_driveInput.Intents.IsTriggered(DriveIntent.Pause) && !_pauseKeyReleased)
            {
                _pauseKeyReleased = true;
                return;
            }

            if (_driveInput.Intents.IsTriggered(DriveIntent.Pause) && _pauseKeyReleased)
            {
                _pauseKeyReleased = false;
                switch (_pausedState)
                {
                    case AppState.TimeTrial:
                        _timeTrial?.Resume();
                        _state = AppState.TimeTrial;
                        break;
                    case AppState.SingleRace:
                        _singleRace?.Resume();
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
                    _timeTrial?.Pause();
                    _timeTrial?.ClearPauseRequest();
                    _state = AppState.Paused;
                    break;
                case AppState.SingleRace:
                    _singleRace?.Pause();
                    _singleRace?.ClearPauseRequest();
                    _state = AppState.Paused;
                    break;
            }
        }
    }
}


