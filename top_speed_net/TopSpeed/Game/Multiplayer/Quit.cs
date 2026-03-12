using TopSpeed.Menu;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void OpenMultiplayerRaceQuitConfirmation()
        {
            if (_multiplayerRace == null)
                return;
            if (_multiplayerRaceQuitConfirmActive)
                return;
            if (_multiplayerCoordinator.Questions.IsQuestionMenu(_menu.CurrentId))
                return;

            _multiplayerRaceQuitConfirmActive = true;

            var question = new Question(
                "Quit race?",
                "Are you sure you want to quit this multiplayer race?",
                QuestionId.No,
                HandleMultiplayerRaceQuitQuestionResult,
                new QuestionButton(QuestionId.Yes, "Yes, quit the race"),
                new QuestionButton(QuestionId.No, "No, continue racing", flags: QuestionButtonFlags.Default))
            {
                OpenAsOverlay = true
            };
            _multiplayerCoordinator.Questions.Show(question);
        }

        private void HandleMultiplayerRaceQuitQuestionResult(int resultId)
        {
            if (resultId == QuestionId.Yes)
                ConfirmQuitMultiplayerRace();
            else if (resultId == QuestionId.No || resultId == QuestionId.Cancel || resultId == QuestionId.Close)
                CancelMultiplayerRaceQuitConfirmation();
        }

        private void CancelMultiplayerRaceQuitConfirmation()
        {
            if (!_multiplayerRaceQuitConfirmActive)
                return;

            _multiplayerRaceQuitConfirmActive = false;
        }

        private void ConfirmQuitMultiplayerRace()
        {
            if (!_multiplayerRaceQuitConfirmActive)
                return;

            _multiplayerRaceQuitConfirmActive = false;
            if (_session != null)
                TrySendSession(_session.SendRoomLeave(), "room leave request");

            _multiplayerRace?.FinalizeMultiplayerMode();
            _multiplayerRace?.Dispose();
            _multiplayerRace = null;

            ResetPendingMultiplayerState();
            _state = AppState.Menu;
            _menu.ShowRoot("multiplayer_lobby");
        }

        private bool TrySendSession(bool sent, string action)
        {
            if (sent)
                return true;

            _speech.Speak($"Failed to send {action}. Please check your connection.");
            return false;
        }
    }
}

