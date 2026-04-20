using Key = TopSpeed.Input.InputKey;
using TopSpeed.Drive.Single;
using TopSpeed.Drive;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
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
            if (_timeTrial.WantsExit || _input.WasPressed(Key.Escape) || ConsumeDriveTouchExitRequest())
                EndRace(_timeTrial.WantsExit ? _timeTrial.ConsumeResultSummary() : null);
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
            if (_singleRace.WantsExit || _input.WasPressed(Key.Escape) || ConsumeDriveTouchExitRequest())
                EndRace(_singleRace.WantsExit ? _singleRace.ConsumeResultSummary() : null);
        }

        private void EndRace(DriveResultSummary? resultSummary = null)
        {
            _timeTrial?.FinalizeSession();
            _timeTrial?.Dispose();
            _timeTrial = null;

            _singleRace?.FinalizeSession();
            _singleRace?.Dispose();
            _singleRace = null;

            _input.ResetState();
            _state = AppState.Menu;
            _menu.ShowRoot("main");
            _menu.FadeInMenuMusic();
            if (resultSummary != null)
                ShowRaceResultDialog(resultSummary);
        }
    }
}





