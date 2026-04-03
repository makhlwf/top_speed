namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
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

