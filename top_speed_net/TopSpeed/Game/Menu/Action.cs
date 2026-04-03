using TopSpeed.Menu;
using TopSpeed.Core;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void HandleMenuAction(MenuAction action)
        {
            switch (action)
            {
                case MenuAction.Exit:
                    ExitRequested?.Invoke();
                    break;
                case MenuAction.QuickStart:
                    PrepareQuickStart();
                    QueueRaceStart(RaceMode.QuickStart);
                    break;
                default:
                    break;
            }
        }

        private bool UpdateModalOperations()
        {
            return _multiplayerCoordinator.UpdatePendingOperations();
        }

        private void EnterMenuState()
        {
            _state = AppState.Menu;
        }
    }
}

