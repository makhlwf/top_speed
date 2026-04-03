using System;
using TopSpeed.Protocol;

namespace TopSpeed.Race
{
    internal sealed partial class MultiplayerMode
    {
        public void Initialize()
        {
            InitializeMode();
            var localPlayerNumber = LocalPlayerNumber;
            _positionFinish = 0;
            _position = localPlayerNumber + 1;
            _positionComment = _position;
            _lastComment = 0.0f;
            _infoKeyReleased = true;
            _sendAccumulator = 0.0f;
            _sentStart = false;
            _sentFinish = false;
            _serverStopReceived = false;
            _lastCarState = _car.State;
            _lastRaceSnapshotSequence = 0;
            _lastRaceSnapshotTick = 0;
            _hasRaceSnapshotSequence = false;
            _snapshotFrames.Clear();
            _snapshotTickNow = 0f;
            _hasSnapshotTickNow = false;
            _sendFailureAnnounced = false;
            _liveFailureAnnounced = false;
            Array.Clear(_disconnectedPlayerSlots, 0, _disconnectedPlayerSlots.Length);
            _remoteLiveStates.Clear();
            _liveTx.Resume();

            var rowSpacing = Math.Max(10.0f, _car.LengthM * 1.5f);
            var positionX = CalculateGridStartX(localPlayerNumber, _car.WidthM, StartLineY);
            var positionY = CalculateGridStartY(localPlayerNumber, rowSpacing, StartLineY);
            _car.SetPosition(positionX, positionY);

            LoadPositionSounds(
                _soundPlayerNr,
                _soundPosition,
                _soundFinished,
                MaxPlayers,
                MaxPlayers);
            LoadRaceUiSounds(out _soundYouAre, out _soundPlayer);
            SpeakRaceIntro(_soundYouAre, _soundPlayer, localPlayerNumber + 1);

            _currentState = PlayerState.AwaitingStart;
            TrySendRace(_session.SendPlayerState(_raceInstanceId, _currentState));
        }

        public void FinalizeMultiplayerMode()
        {
            foreach (var remote in _remotePlayers.Values)
            {
                remote.Player.FinalizePlayer();
                remote.Player.Dispose();
            }
            _remotePlayers.Clear();
            _remoteMediaTransfers.Clear();
            _remoteLiveStates.Clear();
            _snapshotFrames.Clear();
            _liveTx.Dispose();

            DisposePositionSounds(
                _soundPlayerNr,
                _soundPosition,
                _soundFinished,
                _soundPosition.Length);

            DisposeSound(_soundYouAre);
            DisposeSound(_soundPlayer);
            FinalizeMode();
        }

        public void Pause()
        {
            _liveTx.Pause();
            PauseCore(() =>
            {
                foreach (var remote in _remotePlayers.Values)
                    remote.Player.Pause();
            });
        }

        public void Unpause()
        {
            _liveTx.Resume();
            UnpauseCore(() =>
            {
                foreach (var remote in _remotePlayers.Values)
                    remote.Player.Unpause();
            });
        }

        protected override void OnLocalRadioMediaLoaded(uint mediaId, string mediaPath)
        {
            if (!_liveTx.SetMedia(mediaId, mediaPath, out var error))
                SpeakText(error);
        }

        protected override void OnLocalRadioPlaybackChanged(bool loaded, bool playing, uint mediaId)
        {
            if (!_liveTx.SetPlayback(loaded, playing, mediaId, out var error))
                SpeakText(error);
        }
    }
}



