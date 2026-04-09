using TopSpeed.Protocol;

namespace TopSpeed.Race
{
    internal sealed partial class MultiplayerMode
    {
        public void SetHostPaused(bool paused)
        {
            if (_serverStopReceived || _hostPaused == paused)
                return;

            _hostPaused = paused;
            if (paused)
            {
                _currentState = PlayerState.AwaitingStart;
                TrySendRace(_session.SendPlayerState(_raceInstanceId, _currentState));
                _car.SetOverrideController(_finishLockController);
                _car.SetNeutralGear();
                _car.Quiet();
                _car.ShutdownEngine();
                _car.StopMotionImmediately();
                _soundPause?.Play(loop: false);
                return;
            }

            _car.SetOverrideController(null);
            _soundUnpause?.Play(loop: false);
        }
    }
}
