using SharpDX.DirectInput;
using TopSpeed.Race;

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
            if (_timeTrial.WantsExit || _input.WasPressed(Key.Escape))
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
            if (_singleRace.WantsExit || _input.WasPressed(Key.Escape))
                EndRace(_singleRace.WantsExit ? _singleRace.ConsumeResultSummary() : null);
        }

        private void EndRace(RaceResultSummary? resultSummary = null)
        {
            _timeTrial?.FinalizeTimeTrialMode();
            _timeTrial?.Dispose();
            _timeTrial = null;

            _singleRace?.FinalizeSingleRaceMode();
            _singleRace?.Dispose();
            _singleRace = null;

            _state = AppState.Menu;
            _menu.ShowRoot("main");
            _menu.FadeInMenuMusic();
            if (resultSummary != null)
                ShowRaceResultDialog(resultSummary);
        }
    }
}

