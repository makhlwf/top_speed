using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Data;
using TS.Audio;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track : IDisposable
    {
        private const float LaneWidthMeters = 5.0f;
        private const float LegacyLaneWidthMeters = 50.0f;
        private const float CallLengthMeters = 30.0f;
        private const float MinPartLengthMeters = 50.0f;

        public struct Road
        {
            public float Left;
            public float Right;
            public TrackSurface Surface;
            public TrackType Type;
            public float Length;
        }

        private readonly AudioManager _audio;
        private readonly string _trackName;
        private readonly bool _userDefined;
        private readonly TrackDefinition[] _definition;
        private readonly int _segmentCount;
        private readonly TrackWeather _weather;
        private readonly TrackAmbience _ambience;
        private readonly IReadOnlyDictionary<string, TrackRoomDefinition> _roomProfiles;
        private readonly IReadOnlyDictionary<string, TrackSoundSourceDefinition> _soundDefinitions;
        private readonly Dictionary<string, int> _segmentIndexById;
        private readonly Dictionary<string, RuntimeTrackSound> _segmentTrackSounds;
        private readonly List<RuntimeTrackSound> _allTrackSounds;
        private readonly List<PendingHandleStop> _pendingHandleStops;
        private readonly Random _random;
        private readonly string _sourceDirectory;
        private readonly float[] _segmentStartDistances;
        private RoadModel? _roadModel;

        private float _laneWidth;
        private float _curveScale;
        private float _callLength;
        private float _lapDistance;
        private float _lapCenter;
        private int _currentRoad;
        private float _relPos;
        private float _prevRelPos;
        private int _lastCalled;
        private float _factor;
        private float _noiseLength;
        private float _noiseStartPos;
        private float _noiseEndPos;
        private bool _noisePlaying;
        private int _ambientVolumePercent;
        private float _ambientVolumeScale;
        private int _activeAudioSegmentIndex;
        private RoomAcoustics _activeRoomAcoustics;

        private AudioSourceHandle? _soundCrowd;
        private AudioSourceHandle? _soundOcean;
        private AudioSourceHandle? _soundRain;
        private AudioSourceHandle? _soundWind;
        private AudioSourceHandle? _soundStorm;
        private AudioSourceHandle? _soundDesert;
        private AudioSourceHandle? _soundAirport;
        private AudioSourceHandle? _soundAirplane;
        private AudioSourceHandle? _soundClock;
        private AudioSourceHandle? _soundJet;
        private AudioSourceHandle? _soundThunder;
        private AudioSourceHandle? _soundPile;
        private AudioSourceHandle? _soundConstruction;
        private AudioSourceHandle? _soundRiver;
        private AudioSourceHandle? _soundHelicopter;
        private AudioSourceHandle? _soundOwl;

        private Track(string trackName, TrackData data, AudioManager audio, bool userDefined)
        {
            _trackName = trackName.Length < 64 ? trackName : string.Empty;
            _userDefined = userDefined;
            _audio = audio;
            _laneWidth = LaneWidthMeters;
            UpdateCurveScale();
            _callLength = CallLengthMeters;
            _weather = data.Weather;
            _ambience = data.Ambience;
            _definition = data.Definitions;
            _segmentCount = _definition.Length;
            _roomProfiles = data.RoomProfiles;
            _soundDefinitions = data.SoundSources;
            _segmentIndexById = BuildSegmentIndex(_definition);
            _segmentTrackSounds = new Dictionary<string, RuntimeTrackSound>(StringComparer.OrdinalIgnoreCase);
            _allTrackSounds = new List<RuntimeTrackSound>();
            _pendingHandleStops = new List<PendingHandleStop>();
            _random = new Random();
            _sourceDirectory = ResolveSourceDirectory(data.SourcePath);
            _segmentStartDistances = new float[Math.Max(0, _segmentCount)];
            _ambientVolumePercent = 100;
            _ambientVolumeScale = 1f;
            _activeAudioSegmentIndex = -1;
            _activeRoomAcoustics = RoomAcoustics.Default;

            InitializeSounds();
            InitializeTrackSoundSources();
        }

        public string TrackName => _trackName;
        public float Length => _lapDistance;
        public int SegmentCount => _segmentCount;
        public float LapDistance => _lapDistance;
        public TrackWeather Weather => _weather;
        public TrackAmbience Ambience => _ambience;
        public bool UserDefined => _userDefined;
        public float LaneWidth => _laneWidth;
        public TrackSurface InitialSurface => _definition.Length > 0 ? _definition[0].Surface : TrackSurface.Asphalt;
    }
}

