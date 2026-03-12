using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Race.Events;
using TopSpeed.Vehicles;

namespace TopSpeed.Race
{
    internal sealed partial class MultiplayerMode
    {
        public void Run(float elapsed)
        {
            BeginFrame();

            ApplyBufferedRaceSnapshots(elapsed);
            UpdatePositions();
            RunPlayerVehicleStep(elapsed, afterTrackUpdate: () =>
            {
                var spatialTrackLength = GetSpatialTrackLength();
                foreach (var remote in _remotePlayers.Values)
                    remote.Player.UpdateRemoteAudio(_car.PositionX, _car.PositionY, spatialTrackLength, elapsed);
            });
            DrainRemoteLiveFrames();

            if (_started
                && !_sentFinish
                && _lastCarState != CarState.Crashing
                && _lastCarState != CarState.Crashed
                && (_car.State == CarState.Crashing || _car.State == CarState.Crashed))
            {
                TrySendRace(_session.SendPlayerCrashed(), "crash event");
            }
            _lastCarState = _car.State;

            HandlePlayerLapProgress(
                onPlayerFinished: () =>
                {
                    AnnounceFinishOrder(_soundPlayerNr, _soundFinished, _playerNumber, ref _positionFinish);
                    if (!_sentFinish)
                    {
                        _sentFinish = true;
                        _currentState = PlayerState.Finished;
                        TrySendRace(_session.SendPlayerFinished(), "finish event");
                        TrySendRace(_session.SendPlayerState(_currentState), "finished state");
                    }
                    PushEvent(RaceEventType.RaceFinish, 1.0f + _speakTime - _elapsedTotal);
                });

            HandleCoreRaceMetricsRequests(includeFinishedRaceTime: true);
            HandleCommentRequests(elapsed, Comment, ref _lastComment, ref _infoKeyReleased);

            HandlePlayerInfoRequests(
                MaxPlayers - 1,
                HasPlayerInRace,
                GetVehicleNameForPlayer,
                CalculatePlayerPerc);

            HandlePlayerNumberRequest(_playerNumber);
            HandleGeneralInfoRequests(ref _pauseKeyReleased);
            if (!_liveTx.Update(elapsed, out var liveError))
            {
                if (!_liveFailureAnnounced)
                {
                    _liveFailureAnnounced = true;
                    SpeakText(liveError);
                }
            }

            _sendAccumulator += elapsed;
            if (_sendAccumulator >= SendIntervalSeconds)
            {
                _sendAccumulator = 0.0f;
                var state = _currentState;
                if (_sentFinish)
                    state = PlayerState.Finished;
                else if (_started)
                    state = PlayerState.Racing;

                var raceData = new PlayerRaceData
                {
                    PositionX = _car.PositionX,
                    PositionY = _car.PositionY,
                    Speed = (ushort)_car.Speed,
                    Frequency = _car.Frequency
                };
                TrySendRace(_session.SendPlayerData(
                    raceData,
                    _car.CarType,
                    state,
                    _car.EngineRunning,
                    _car.Braking,
                    _car.Horning,
                    _car.Backfiring(),
                    LocalMediaLoaded,
                    LocalMediaPlaying,
                    LocalMediaId),
                    "player state update");
            }

            if (CompleteFrame(elapsed))
                return;
        }

        protected override void OnRaceStartEvent()
        {
            base.OnRaceStartEvent();
            if (_sentStart)
                return;

            _sentStart = true;
            _currentState = PlayerState.Racing;
            TrySendRace(_session.SendPlayerStarted(), "race start event");
            TrySendRace(_session.SendPlayerState(_currentState), "racing state");
        }

        private bool TrySendRace(bool sent, string action)
        {
            if (sent)
                return true;

            if (_sendFailureAnnounced)
                return false;

            _sendFailureAnnounced = true;
            SpeakText($"Network send failed while sending {action}.");
            return false;
        }
    }
}

