namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        public void Update(float deltaSeconds)
        {
            _input.Update();
            if (_input.TryGetControllerState(out var controller))
                _raceInput.Run(_input.Current, controller, deltaSeconds, _input.ActiveControllerIsRacingWheel);
            else
                _raceInput.Run(_input.Current, deltaSeconds);

            TryShowDeviceChoiceDialog();

            _raceInput.SetOverlayInputBlocked(
                _state == AppState.MultiplayerRace &&
                (_multiplayerCoordinator.Questions.HasActiveOverlayQuestion || _dialogs.HasActiveOverlayDialog));

            UpdateTextInputPrompt();
            _stateMachine.Update(deltaSeconds);

            if (_pendingRaceStart)
            {
                _pendingRaceStart = false;
                StartRace(_pendingMode);
            }

            SyncAudioLoopState();
        }
    }
}

