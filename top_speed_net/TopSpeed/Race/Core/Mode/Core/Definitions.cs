using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Race.Events;
using TopSpeed.Race.Panels;
using TopSpeed.Race.Runtime;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Core;
using TS.Audio;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Race
{
    internal abstract partial class RaceMode : IDisposable
    {
        protected const int MaxLaps = 16;
        protected const int MaxUnkeys = 12;
        protected const int RandomSoundGroups = 16;
        protected const int RandomSoundMax = 32;
        protected const float DefaultCarStartDelaySeconds = 3.0f;
        protected const float DefaultStartCueDelaySeconds = 1.0f;
        protected const float DefaultRaceStartDelaySeconds = 4.0f;
        private const float PostFinishStopSpeedKph = 0.5f;
        private const float KmToMiles = 0.621371f;
        private const float MetersPerMile = 1609.344f;
        private const float MetersToFeet = 3.28084f;

        public enum RandomSound
        {
            EasyLeft = 0,
            Left = 1,
            HardLeft = 2,
            HairpinLeft = 3,
            EasyRight = 4,
            Right = 5,
            HardRight = 6,
            HairpinRight = 7,
            Asphalt = 8,
            Gravel = 9,
            Water = 10,
            Sand = 11,
            Snow = 12,
            Finish = 13,
            Front = 14,
            Tail = 15
        }

        protected readonly AudioManager _audio;
        protected readonly SpeechService _speech;
        protected readonly RaceSettings _settings;
        protected readonly RaceInput _input;
        protected readonly IVibrationDevice? _vibrationDevice;
        protected readonly Track _track;
        protected readonly ICar _car;
        protected readonly List<RaceEvent> _events;
        protected readonly Stopwatch _stopwatch;
        protected readonly AudioSourceHandle[] _soundNumbers;
        protected readonly AudioSourceHandle?[][] _randomSounds;
        protected readonly int[] _totalRandomSounds;
        private readonly SoundQueue _soundQueue;
        private readonly List<RaceEvent> _dueEvents;
        private readonly VehicleRadioController _localRadio;
        private readonly RadioVehiclePanel _radioPanel;
        private readonly VehiclePanelManager _panelManager;
        private long _eventSequence;
        private uint _nextMediaId;
        private RaceResultSummary? _pendingResultSummary;

        protected bool _manualTransmission;
        protected int _nrOfLaps;
        protected int _lap;
        protected float _elapsedTotal;
        protected int _raceTime;
        protected int _highscore;
        protected bool _started;
        protected bool _finished;
        protected bool _engineStarted;
        protected float _sayTimeLength;
        protected float _speakTime;
        protected float _nextRequestInfoAt;
        protected int _unkeyQueue;
        protected Track.Road _currentRoad;
        private TrackType _lastRoadTypeAtPosition;
        private bool _hasLastRoadTypeAtPosition;
        protected long _oldStopwatchMs;
        protected long _stopwatchDiffMs;
        private Vector3 _lastListenerPosition;
        private bool _listenerInitialized;

        protected AudioSourceHandle _soundStart;
        protected AudioSourceHandle[] _soundLaps;
        protected AudioSourceHandle _soundBestTime;
        protected AudioSourceHandle _soundNewTime;
        protected AudioSourceHandle _soundYourTime;
        protected AudioSourceHandle _soundMinute;
        protected AudioSourceHandle _soundMinutes;
        protected AudioSourceHandle _soundSecond;
        protected AudioSourceHandle _soundSeconds;
        protected AudioSourceHandle _soundPoint;
        protected AudioSourceHandle _soundPercent;
        protected AudioSourceHandle[] _soundUnkey;
        protected AudioSourceHandle? _soundTheme4;
        protected AudioSourceHandle? _soundPause;
        protected AudioSourceHandle? _soundUnpause;
        protected AudioSourceHandle? _soundTrackName;
        protected AudioSourceHandle? _soundTurnEndDing;

        protected bool ExitRequested { get; set; }
        protected bool PauseRequested { get; set; }
        private bool _exitWhenQueueIdle;
        private bool _requirePostFinishStopBeforeExit;
    }
}

