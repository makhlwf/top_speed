using System;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Race.Events;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Race
{
    internal sealed class LevelSingleRace : Level
    {
        private const int MaxComputerPlayers = 7;
        private const int MaxPlayers = 8;
        private const float StartLineY = 140.0f;

        private readonly ComputerPlayer?[] _computerPlayers;
        private readonly AudioSourceHandle?[] _soundPosition;
        private readonly AudioSourceHandle?[] _soundPlayerNr;
        private readonly AudioSourceHandle?[] _soundFinished;

        private AudioSourceHandle? _soundYouAre;
        private AudioSourceHandle? _soundPlayer;
        private float _lastComment;
        private bool _infoKeyReleased;
        private int _positionFinish;
        private int _position;
        private int _positionComment;
        private int _playerNumber;
        private int _nComputerPlayers;
        private bool _pauseKeyReleased = true;
        private float _raceStartDelay;
        private bool _botsScheduled;

        public LevelSingleRace(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            RaceInput input,
            string track,
            bool automaticTransmission,
            int nrOfLaps,
            int vehicle,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice)
            : base(audio, speech, settings, input, track, automaticTransmission, nrOfLaps, vehicle, vehicleFile, vibrationDevice)
        {
            _nComputerPlayers = Math.Min(settings.NrOfComputers, MaxComputerPlayers);
            _playerNumber = 1;
            _lastComment = 0.0f;
            _infoKeyReleased = true;
            _positionFinish = 0;

            _computerPlayers = new ComputerPlayer?[MaxComputerPlayers];
            _soundPosition = new AudioSourceHandle?[MaxPlayers];
            _soundPlayerNr = new AudioSourceHandle?[MaxPlayers];
            _soundFinished = new AudioSourceHandle?[MaxPlayers];
        }

        public void Initialize(int playerNumber)
        {
            InitializeLevel();
            _playerNumber = playerNumber;
            _position = playerNumber + 1;
            _positionComment = playerNumber + 1;
            _raceStartDelay = DefaultRaceStartDelaySeconds;
            _botsScheduled = false;

            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var botNumber = i;
                if (botNumber >= _playerNumber)
                    botNumber++;
                _computerPlayers[i] = GenerateRandomPlayer(botNumber);
            }

            var maxLength = _car.LengthM;
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot != null && bot.LengthM > maxLength)
                    maxLength = bot.LengthM;
            }

            var rowSpacing = Math.Max(10.0f, maxLength * 1.5f);
            var playerX = CalculateGridStartX(_playerNumber, _car.WidthM, StartLineY);
            var playerY = CalculateGridStartY(_playerNumber, rowSpacing, StartLineY);
            _car.SetPosition(playerX, playerY);

            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot == null)
                    continue;
                var botX = CalculateGridStartX(bot.PlayerNumber, bot.WidthM, StartLineY);
                var botY = CalculateGridStartY(bot.PlayerNumber, rowSpacing, StartLineY);
                bot.Initialize(botX, botY, _track.Length);
            }

            for (var i = 0; i <= _nComputerPlayers; i++)
            {
                _soundPlayerNr[i] = LoadLanguageSound($"race\\info\\player{i + 1}");

                var positionIndex = i == _nComputerPlayers ? MaxPlayers : i + 1;
                _soundPosition[i] = LoadLanguageSound($"race\\info\\youarepos{positionIndex}");
                _soundFinished[i] = LoadLanguageSound($"race\\info\\finished{positionIndex}");
            }

            LoadRandomSounds(RandomSound.Front, "race\\info\\front");
            LoadRandomSounds(RandomSound.Tail, "race\\info\\tail");

            _soundYouAre = LoadLanguageSound("race\\youare");
            _soundPlayer = LoadLanguageSound("race\\player");
            _soundTheme4 = LoadLanguageSound("music\\theme4", streamFromDisk: false);
            _soundPause = LoadLanguageSound("race\\pause");
            _soundUnpause = LoadLanguageSound("race\\unpause");
            _soundTheme4.SetVolumePercent((int)Math.Round(_settings.MusicVolume * 100f));

            Speak(_soundYouAre);
            Speak(_soundPlayer);
            Speak(_soundNumbers[_playerNumber + 1]);
        }

        public void FinalizeLevelSingleRace()
        {
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                _computerPlayers[i]?.FinalizePlayer();
                _computerPlayers[i]?.Dispose();
            }

            for (var i = 0; i <= _nComputerPlayers; i++)
            {
                DisposeSound(_soundPosition[i]);
                DisposeSound(_soundPlayerNr[i]);
                DisposeSound(_soundFinished[i]);
            }

            DisposeSound(_soundYouAre);
            DisposeSound(_soundPlayer);
            FinalizeLevel();
        }

        public void Run(float elapsed)
        {
            BeginFrame(_raceStartDelay);

            UpdatePositions();

            for (var botIndex = 0; botIndex < _nComputerPlayers; botIndex++)
            {
                var bot = _computerPlayers[botIndex];
                if (bot == null)
                    continue;
                bot.Run(elapsed, _car.PositionX, _car.PositionY);
                if (_track.Lap(bot.PositionY) > _nrOfLaps && !bot.Finished)
                {
                    bot.Stop();
                    bot.SetFinished(true);
                    Speak(_soundPlayerNr[bot.PlayerNumber]!, true);
                    Speak(_soundFinished[_positionFinish++]!, true);
                    if (CheckFinish())
                        PushEvent(RaceEventType.RaceFinish, 1.0f + _speakTime - _elapsedTotal);
                }
            }

            RunPlayerVehicleStep(elapsed);
            HandlePlayerLapProgress(
                onPlayerFinished: () =>
                {
                    Speak(_soundPlayerNr[_playerNumber]!, true);
                    Speak(_soundFinished[_positionFinish++]!, true);
                    if (CheckFinish())
                        PushEvent(RaceEventType.RaceFinish, 1.0f + _speakTime - _elapsedTotal);
                });

            CheckForBumps();

            HandleCoreRaceMetricsRequests(includeFinishedRaceTime: true);
            HandleCommentRequests(elapsed, Comment, ref _lastComment, ref _infoKeyReleased);

            if (_input.TryGetPlayerInfo(out var infoPlayer) && _acceptPlayerInfo && infoPlayer <= _nComputerPlayers)
            {
                _acceptPlayerInfo = false;
                SpeakText(GetVehicleNameForPlayer(infoPlayer));
                PushEvent(RaceEventType.AcceptPlayerInfo, 0.5f);
            }

            if (_input.TryGetPlayerPosition(out var positionPlayer) && _acceptPlayerInfo && positionPlayer <= _nComputerPlayers && _started)
            {
                _acceptPlayerInfo = false;
                var perc = CalculatePlayerPerc(positionPlayer);
                SpeakText(FormatPercentageText(string.Empty, perc));
                PushEvent(RaceEventType.AcceptPlayerInfo, 0.5f);
            }

            HandlePlayerNumberRequest(_playerNumber);
            HandleGeneralInfoRequests(ref _pauseKeyReleased);

            if (CompleteFrame(elapsed))
                return;
        }

        protected override void OnRaceStartEvent()
        {
            base.OnRaceStartEvent();
            if (_botsScheduled)
                return;

            for (var botIndex = 0; botIndex < _nComputerPlayers; botIndex++)
                _computerPlayers[botIndex]?.PendingStart(0.0f);
            _botsScheduled = true;
        }

        protected override void OnRaceTimeFinalizeEvent()
        {
            base.OnRaceTimeFinalizeEvent();
            RequestExitWhenQueueIdle();
        }

        public void Pause()
        {
            PauseCore(() =>
            {
                for (var i = 0; i < _nComputerPlayers; i++)
                    _computerPlayers[i]?.Pause();
            });
        }

        public void Unpause()
        {
            UnpauseCore(() =>
            {
                for (var i = 0; i < _nComputerPlayers; i++)
                    _computerPlayers[i]?.Unpause();
            });
        }

        private ComputerPlayer GenerateRandomPlayer(int playerNumber)
        {
            var vehicleIndex = Algorithm.RandomInt(VehicleCatalog.VehicleCount);
            return new ComputerPlayer(
                _audio,
                _track,
                _settings,
                vehicleIndex,
                playerNumber,
                () => _elapsedTotal,
                () => _started,
                null);
        }

        private void UpdatePositions()
        {
            _position = 1;
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                if (_computerPlayers[i]?.PositionY > _car.PositionY)
                    _position++;
            }
        }

        private void Comment(bool automatic)
        {
            if (!_started || _lap > _nrOfLaps)
                return;

            var position = 1;
            var inFront = -1;
            var inFrontDist = 500.0f;
            var onTail = -1;
            var onTailDist = 500.0f;

            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot == null)
                    continue;

                if (bot.PositionY > _car.PositionY)
                {
                    position++;
                }

                var delta = GetRelativeTrackDelta(bot.PositionY);
                if (delta > 0f)
                {
                    var dist = delta;
                    if (dist < inFrontDist)
                    {
                        inFront = i;
                        inFrontDist = dist;
                    }
                }
                else if (delta < 0f)
                {
                    var dist = -delta;
                    if (dist < onTailDist)
                    {
                        onTail = i;
                        onTailDist = dist;
                    }
                }
            }

            if (automatic && position != _positionComment)
            {
                if (position == _nComputerPlayers + 1)
                    Speak(_soundPosition[_nComputerPlayers]!, true);
                else
                    Speak(_soundPosition[position - 1]!, true);
                _positionComment = position;
                return;
            }

            if (inFrontDist < onTailDist)
            {
                if (inFront != -1)
                {
                    var bot = _computerPlayers[inFront]!;
                    Speak(_soundPlayerNr[bot.PlayerNumber]!, true);
                    var sound = _randomSounds[(int)RandomSound.Front][Algorithm.RandomInt(_totalRandomSounds[(int)RandomSound.Front])];
                    if (sound != null)
                        Speak(sound, true);
                    return;
                }
            }
            else
            {
                if (onTail != -1)
                {
                    var bot = _computerPlayers[onTail]!;
                    Speak(_soundPlayerNr[bot.PlayerNumber]!, true);
                    var sound = _randomSounds[(int)RandomSound.Tail][Algorithm.RandomInt(_totalRandomSounds[(int)RandomSound.Tail])];
                    if (sound != null)
                        Speak(sound, true);
                    return;
                }
            }

            if (inFront == -1 && onTail == -1 && !automatic)
            {
                if (position == _nComputerPlayers + 1)
                    Speak(_soundPosition[_nComputerPlayers]!, true);
                else
                    Speak(_soundPosition[position - 1]!, true);
                _positionComment = position;
            }
        }

        private void CheckForBumps()
        {
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot == null)
                    continue;
                if (_car.State == CarState.Running && !bot.Finished)
                {
                    var dx = _car.PositionX - bot.PositionX;
                    var dy = _car.PositionY - bot.PositionY;
                    var xThreshold = (_car.WidthM + bot.WidthM) * 0.5f;
                    var yThreshold = (_car.LengthM + bot.LengthM) * 0.5f;
                    if (Math.Abs(dx) < xThreshold && Math.Abs(dy) < yThreshold)
                    {
                        var bumpX = dx;
                        var bumpY = dy;
                        var bumpSpeed = _car.Speed - bot.Speed;
                        _car.Bump(bumpX, bumpY, bumpSpeed);
                        bot.Bump(-bumpX, -bumpY, -bumpSpeed);
                    }
                }
            }
        }

        private bool CheckFinish()
        {
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                if (_computerPlayers[i]?.Finished == false)
                    return false;
            }
            if (_lap <= _nrOfLaps)
                return false;
            return true;
        }

        private int CalculatePlayerPerc(int player)
        {
            int perc;
            if (player == _playerNumber)
                perc = (int)((_car.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f);
            else if (player > _playerNumber)
                perc = (int)((_computerPlayers[player - 1]!.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f);
            else
                perc = (int)((_computerPlayers[player]!.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f);
            if (perc > 100)
                perc = 100;
            return perc;
        }

        private string GetVehicleNameForPlayer(int playerIndex)
        {
            if (playerIndex == _playerNumber)
            {
                if (_car.UserDefined && !string.IsNullOrWhiteSpace(_car.CustomFile))
                    return FormatVehicleName(_car.CustomFile);
                return _car.VehicleName;
            }

            if (playerIndex < _playerNumber)
            {
                var bot = _computerPlayers[playerIndex];
                if (bot != null)
                    return VehicleCatalog.Vehicles[bot.VehicleIndex].Name;
            }
            else if (playerIndex > _playerNumber)
            {
                var bot = _computerPlayers[playerIndex - 1];
                if (bot != null)
                    return VehicleCatalog.Vehicles[bot.VehicleIndex].Name;
            }

            return "Vehicle";
        }
    }
}
