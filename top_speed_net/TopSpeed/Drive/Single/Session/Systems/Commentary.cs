using System;
using TopSpeed.Common;
using TopSpeed.Input;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Drive.Single.Session.Systems
{
    internal sealed class Commentary : TopSpeed.Drive.Session.Subsystem
    {
        private const int FrontSlot = 14;
        private const int TailSlot = 15;
        private readonly DriveSettings _settings;
        private readonly DriveInput _input;
        private readonly Vehicles.ICar _car;
        private readonly ComputerPlayer?[] _players;
        private readonly int _playerCount;
        private readonly Func<int> _getPlayerNumber;
        private readonly Source?[] _positionSounds;
        private readonly Source?[] _playerNumberSounds;
        private readonly Source?[][] _randomSounds;
        private readonly int[] _totalRandomSounds;
        private readonly Func<bool> _isStarted;
        private readonly Func<int> _getLap;
        private readonly Func<int> _getLapLimit;
        private readonly Func<int> _getPositionComment;
        private readonly Action<int> _setPositionComment;
        private readonly Action<Source?, bool> _speakIfLoaded;
        private readonly Action<Source, bool> _speak;
        private bool _infoKeyReleased = true;
        private float _lastComment;

        public Commentary(
            string name,
            int order,
            DriveSettings settings,
            DriveInput input,
            Vehicles.ICar car,
            ComputerPlayer?[] players,
            int playerCount,
            Func<int> getPlayerNumber,
            Source?[] positionSounds,
            Source?[] playerNumberSounds,
            Source?[][] randomSounds,
            int[] totalRandomSounds,
            Func<bool> isStarted,
            Func<int> getLap,
            Func<int> getLapLimit,
            Func<int> getPositionComment,
            Action<int> setPositionComment,
            Action<Source?, bool> speakIfLoaded,
            Action<Source, bool> speak)
            : base(name, order)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _car = car ?? throw new ArgumentNullException(nameof(car));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _playerCount = playerCount;
            _getPlayerNumber = getPlayerNumber ?? throw new ArgumentNullException(nameof(getPlayerNumber));
            _positionSounds = positionSounds ?? throw new ArgumentNullException(nameof(positionSounds));
            _playerNumberSounds = playerNumberSounds ?? throw new ArgumentNullException(nameof(playerNumberSounds));
            _randomSounds = randomSounds ?? throw new ArgumentNullException(nameof(randomSounds));
            _totalRandomSounds = totalRandomSounds ?? throw new ArgumentNullException(nameof(totalRandomSounds));
            _isStarted = isStarted ?? throw new ArgumentNullException(nameof(isStarted));
            _getLap = getLap ?? throw new ArgumentNullException(nameof(getLap));
            _getLapLimit = getLapLimit ?? throw new ArgumentNullException(nameof(getLapLimit));
            _getPositionComment = getPositionComment ?? throw new ArgumentNullException(nameof(getPositionComment));
            _setPositionComment = setPositionComment ?? throw new ArgumentNullException(nameof(setPositionComment));
            _speakIfLoaded = speakIfLoaded ?? throw new ArgumentNullException(nameof(speakIfLoaded));
            _speak = speak ?? throw new ArgumentNullException(nameof(speak));
        }

        public override void Update(TopSpeed.Drive.Session.SessionContext context, float elapsed)
        {
            _lastComment += elapsed;
            if (_settings.AutomaticInfo == AutomaticInfoMode.On && _lastComment > 6.0f)
            {
                Comment(automatic: true);
                _lastComment = 0.0f;
            }

            if (_input.Intents.IsTriggered(DriveIntent.RequestInfo) && _infoKeyReleased)
            {
                _infoKeyReleased = false;
                Comment(automatic: false);
                _lastComment = 0.0f;
            }
            else if (!_input.Intents.IsTriggered(DriveIntent.RequestInfo) && !_infoKeyReleased)
            {
                _infoKeyReleased = true;
            }
        }

        public void Reset()
        {
            _infoKeyReleased = true;
            _lastComment = 0.0f;
        }

        private void Comment(bool automatic)
        {
            if (!_isStarted() || _getLap() > _getLapLimit())
                return;

            var position = 1;
            var inFront = -1;
            var inFrontDist = 500.0f;
            var onTail = -1;
            var onTailDist = 500.0f;

            for (var i = 0; i < _playerCount; i++)
            {
                var bot = _players[i];
                if (bot == null)
                    continue;

                if (bot.PositionY > _car.PositionY)
                    position++;

                var delta = bot.PositionY - _car.PositionY;
                if (delta > 0f)
                {
                    if (delta < inFrontDist)
                    {
                        inFront = i;
                        inFrontDist = delta;
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

            if (automatic && position != _getPositionComment())
            {
                if (position == _playerCount + 1)
                    _speakIfLoaded(_positionSounds[_playerCount], true);
                else
                    _speakIfLoaded(_positionSounds[position - 1], true);

                _setPositionComment(position);
                return;
            }

            if (inFrontDist < onTailDist)
            {
                if (inFront != -1)
                {
                    var bot = _players[inFront]!;
                    _speakIfLoaded(_playerNumberSounds[bot.PlayerNumber], true);
                    SpeakRandom(FrontSlot);
                    return;
                }
            }
            else if (onTail != -1)
            {
                var bot = _players[onTail]!;
                _speakIfLoaded(_playerNumberSounds[bot.PlayerNumber], true);
                SpeakRandom(TailSlot);
                return;
            }

            if (inFront == -1 && onTail == -1 && !automatic)
            {
                if (position == _playerCount + 1)
                    _speakIfLoaded(_positionSounds[_playerCount], true);
                else
                    _speakIfLoaded(_positionSounds[position - 1], true);

                _setPositionComment(position);
            }
        }

        private void SpeakRandom(int slot)
        {
            if (slot < 0 || slot >= _randomSounds.Length || slot >= _totalRandomSounds.Length || _totalRandomSounds[slot] <= 0)
                return;

            var sound = _randomSounds[slot][Algorithm.RandomInt(_totalRandomSounds[slot])];
            if (sound != null)
                _speak(sound, true);
        }
    }
}
