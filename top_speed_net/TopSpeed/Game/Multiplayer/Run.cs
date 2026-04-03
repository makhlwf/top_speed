using TopSpeed.Race;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void RunMultiplayerRace(float elapsed)
        {
            _multiplayerRaceRuntime.Run(elapsed);
        }

        private void StartMultiplayerRace()
        {
            _multiplayerRaceRuntime.Start();
        }

        private void EndMultiplayerRace(RaceResultSummary? resultSummary = null)
        {
            _multiplayerRaceRuntime.End(resultSummary);
        }
    }
}




