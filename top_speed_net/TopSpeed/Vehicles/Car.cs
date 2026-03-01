using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Bots;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Protocol;
using TopSpeed.Tracks;
using TopSpeed.Vehicles.Audio;
using TopSpeed.Vehicles.Control;
using TopSpeed.Vehicles.Core;
using TopSpeed.Vehicles.Events;
using TopSpeed.Vehicles.Physics;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal partial class Car : CarBase, ICar
    {
        private const int MaxSurfaceFreq = 100000;
        private const float BaseLateralSpeed = 7.0f;
        private const float StabilitySpeedRef = 45.0f;
        private const float CrashVibrationSeconds = 1.5f;
        private const float BumpVibrationSeconds = 0.2f;
        private const int ReverseGear = 0;
        private const int FirstForwardGear = 1;
        private const float ReverseShiftMaxSpeedKmh = 15.0f;
        private bool _stickReleased = true;

        private readonly AudioManager _audio;
        private readonly Track _track;
        private readonly RaceSettings _settings;
        private readonly Func<float> _currentTime;
        private readonly Func<bool> _started;
        private readonly string _legacyRoot;
        private readonly string _effectsRoot;
        private readonly EventQueue _events;
        private readonly Processor _eventProcessor;
        private readonly IFlow _audioFlow;
        private readonly CarRuntimeContext _runtimeContext;
        private IModel _physicsModel;

        private CarState _state;
        private TrackSurface _surface;
        private int _gear;
        private float _speed;
        private float _positionX;
        private float _positionY;
        private bool _manualTransmission;
        private bool _backfirePlayed;
        private bool _backfirePlayedAuto;
        private int _hasWipers;
        private int _switchingGear;
        private float _autoShiftCooldown;
        private CarType _carType;
        private ICarListener? _listener;
        private string? _customFile;
        private bool _userDefined;

        private float _surfaceTractionFactor;
        private float _deceleration;
        private float _topSpeed;
        private float _massKg;
        private float _drivetrainEfficiency;
        private float _engineBrakingTorqueNm;
        private float _tireGripCoefficient;
        private float _brakeStrength;
        private float _wheelRadiusM;
        private float _engineBraking;
        private float _idleRpm;
        private float _revLimiter;
        private float _finalDriveRatio;
        private float _reverseMaxSpeedKph;
        private float _reversePowerFactor;
        private float _reverseGearRatio;
        private float _powerFactor;
        private float _peakTorqueNm;
        private float _peakTorqueRpm;
        private float _idleTorqueNm;
        private float _redlineTorqueNm;
        private float _dragCoefficient;
        private float _frontalAreaM2;
        private float _rollingResistanceCoefficient;
        private float _launchRpm;
        private float _lastDriveRpm;
        private float _lateralGripCoefficient;
        private float _highSpeedStability;
        private float _wheelbaseM;
        private float _maxSteerDeg;
        private float _widthM;
        private float _lengthM;
        private int _idleFreq;
        private int _topFreq;
        private int _shiftFreq;
        private int _gears;
        private float _steering;
        private float _thrust;
        private int _prevFrequency;
        private int _frequency;
        private int _prevBrakeFrequency;
        private int _brakeFrequency;
        private int _prevSurfaceFrequency;
        private int _surfaceFrequency;
        private float _laneWidth;
        private float _relPos;
        private int _panPos;
        private int _currentSteering;
        private int _currentThrottle;
        private int _currentBrake;
        private float _currentSurfaceTractionFactor;
        private float _currentDeceleration;
        private float _speedDiff;
        private int _factor1;
        private int _frame;
        private float _prevThrottleVolume;
        private float _throttleVolume;
        private float _lastAudioX;
        private float _lastAudioY;
        private bool _audioInitialized;
        private float _lastAudioElapsed;

        private AudioSourceHandle _soundEngine = default!;
        private AudioSourceHandle? _soundThrottle;
        private AudioSourceHandle _soundHorn = default!;
        private AudioSourceHandle _soundStart = default!;
        private AudioSourceHandle _soundBrake = default!;
        private AudioSourceHandle _soundCrash = default!;
        private AudioSourceHandle[] _soundCrashVariants = Array.Empty<AudioSourceHandle>();
        private AudioSourceHandle _soundMiniCrash = default!;
        private AudioSourceHandle _soundAsphalt = default!;
        private AudioSourceHandle _soundGravel = default!;
        private AudioSourceHandle _soundWater = default!;
        private AudioSourceHandle _soundSand = default!;
        private AudioSourceHandle _soundSnow = default!;
        private AudioSourceHandle? _soundWipers;
        private AudioSourceHandle _soundBump = default!;
        private AudioSourceHandle _soundBadSwitch = default!;
        private AudioSourceHandle? _soundBackfire;
        private AudioSourceHandle[] _soundBackfireVariants = Array.Empty<AudioSourceHandle>();
        private int _lastPlayerEngineVolumePercent = -1;
        private int _lastPlayerEventsVolumePercent = -1;
        private int _lastSurfaceLoopVolumePercent = -1;

        private readonly IVibrationDevice? _vibration;

        private EngineModel _engine;
        private TransmissionPolicy _transmissionPolicy;

        public Car(
            AudioManager audio,
            Track track,
            RaceInput input,
            RaceSettings settings,
            int vehicleIndex,
            string? vehicleFile,
            Func<float> currentTime,
            Func<bool> started,
            IVibrationDevice? vibrationDevice = null)
            : base(new RaceInputCarController(input))
        {
            _audio = audio;
            _track = track;
            _settings = settings;
            _currentTime = currentTime;
            _started = started;
            _legacyRoot = Path.Combine(AssetPaths.SoundsRoot, "Legacy");
            _effectsRoot = Path.Combine(AssetPaths.Root, "Effects");
            _events = new EventQueue();
            _runtimeContext = new CarRuntimeContext();
            _physicsModel = new Default();
            _audioFlow = new Flow();
            _eventProcessor = new Processor(
                HandleEventCarStart,
                HandleEventCarRestart,
                HandleEventCrashComplete,
                HandleEventInGear,
                HandleEventStopVibration,
                HandleEventStopBumpVibration);

            _surface = track.InitialSurface;
            _gear = 1;
            SetState(CarState.Stopped);
            _manualTransmission = false;
            _hasWipers = 0;
            _switchingGear = 0;
            _speed = 0;
            _frame = 1;
            _throttleVolume = 0.0f;
            _prevThrottleVolume = 0.0f;
            _prevFrequency = 0;
            _prevBrakeFrequency = 0;
            _brakeFrequency = 0;
            _prevSurfaceFrequency = 0;
            _surfaceFrequency = 0;
            _laneWidth = track.LaneWidth * 2;
            _relPos = 0f;
            _panPos = 0;
            _currentSteering = 0;
            _currentThrottle = 0;
            _currentBrake = 0;
            _currentSurfaceTractionFactor = 0;
            _currentDeceleration = 0;
            _speedDiff = 0;
            _factor1 = 100;

            VehicleDefinition definition;
            if (string.IsNullOrWhiteSpace(vehicleFile))
            {
                definition = VehicleLoader.LoadOfficial(vehicleIndex, track.Weather);
                _carType = definition.CarType;
            }
            else
            {
                definition = VehicleLoader.LoadCustom(vehicleFile!, track.Weather);
                _carType = definition.CarType;
                _customFile = definition.CustomFile;
                _userDefined = true;
            }

            VehicleName = definition.Name;
            _surfaceTractionFactor = Math.Max(0.01f, SanitizeFinite(definition.SurfaceTractionFactor, 0.01f));
            _deceleration = Math.Max(0.01f, SanitizeFinite(definition.Deceleration, 0.01f));
            _topSpeed = Math.Max(1f, SanitizeFinite(definition.TopSpeed, 1f));
            _massKg = Math.Max(1f, SanitizeFinite(definition.MassKg, 1f));
            _drivetrainEfficiency = Math.Max(0.1f, Math.Min(1.0f, SanitizeFinite(definition.DrivetrainEfficiency, 0.85f)));
            _engineBrakingTorqueNm = Math.Max(0f, SanitizeFinite(definition.EngineBrakingTorqueNm, 0f));
            _tireGripCoefficient = Math.Max(0.1f, SanitizeFinite(definition.TireGripCoefficient, 0.1f));
            _brakeStrength = Math.Max(0.1f, SanitizeFinite(definition.BrakeStrength, 0.1f));
            _wheelRadiusM = Math.Max(0.01f, SanitizeFinite(definition.TireCircumferenceM, 0f) / (2.0f * (float)Math.PI));
            _engineBraking = Math.Max(0.05f, Math.Min(1.0f, SanitizeFinite(definition.EngineBraking, 0.3f)));
            _idleRpm = Math.Max(0f, SanitizeFinite(definition.IdleRpm, 0f));
            _revLimiter = Math.Max(_idleRpm, SanitizeFinite(definition.RevLimiter, _idleRpm));
            _finalDriveRatio = Math.Max(0.1f, SanitizeFinite(definition.FinalDriveRatio, 0.1f));
            _reverseMaxSpeedKph = Math.Max(5f, SanitizeFinite(definition.ReverseMaxSpeedKph, 35f));
            _reversePowerFactor = Math.Max(0.1f, SanitizeFinite(definition.ReversePowerFactor, 0.55f));
            _reverseGearRatio = Math.Max(0.1f, SanitizeFinite(definition.ReverseGearRatio, 3.2f));
            _powerFactor = Math.Max(0.1f, SanitizeFinite(definition.PowerFactor, 0.1f));
            _peakTorqueNm = Math.Max(0f, SanitizeFinite(definition.PeakTorqueNm, 0f));
            _peakTorqueRpm = Math.Max(_idleRpm + 100f, SanitizeFinite(definition.PeakTorqueRpm, _idleRpm + 100f));
            _idleTorqueNm = Math.Max(0f, SanitizeFinite(definition.IdleTorqueNm, 0f));
            _redlineTorqueNm = Math.Max(0f, SanitizeFinite(definition.RedlineTorqueNm, 0f));
            _dragCoefficient = Math.Max(0.01f, SanitizeFinite(definition.DragCoefficient, 0.01f));
            _frontalAreaM2 = Math.Max(0.1f, SanitizeFinite(definition.FrontalAreaM2, 0.1f));
            _rollingResistanceCoefficient = Math.Max(0.001f, SanitizeFinite(definition.RollingResistanceCoefficient, 0.001f));
            _launchRpm = Math.Max(_idleRpm, Math.Min(_revLimiter, SanitizeFinite(definition.LaunchRpm, _idleRpm)));
            _lateralGripCoefficient = Math.Max(0.1f, SanitizeFinite(definition.LateralGripCoefficient, 0.1f));
            _highSpeedStability = Math.Max(0f, Math.Min(1.0f, SanitizeFinite(definition.HighSpeedStability, 0f)));
            _wheelbaseM = Math.Max(0.5f, SanitizeFinite(definition.WheelbaseM, 0.5f));
            _maxSteerDeg = Math.Max(5f, Math.Min(60f, SanitizeFinite(definition.MaxSteerDeg, 35f)));
            _widthM = Math.Max(0.5f, SanitizeFinite(definition.WidthM, 0.5f));
            _lengthM = Math.Max(0.5f, SanitizeFinite(definition.LengthM, 0.5f));
            _idleFreq = definition.IdleFreq;
            _topFreq = definition.TopFreq;
            _shiftFreq = definition.ShiftFreq;
            _gears = Math.Max(1, definition.Gears);
            _steering = SanitizeFinite(definition.Steering, 0.1f);
            _frequency = _idleFreq;

            // Initialize engine model
            _engine = new EngineModel(
                definition.IdleRpm,
                definition.MaxRpm,
                definition.RevLimiter,
                definition.AutoShiftRpm,
                definition.EngineBraking,
                definition.TopSpeed,
                definition.FinalDriveRatio,
                definition.TireCircumferenceM,
                definition.Gears,
                definition.GearRatios);
            _transmissionPolicy = definition.TransmissionPolicy ?? TransmissionPolicy.Default;
            InitializeAudioAssets(definition);
            _vibration = InitializeVibration(vibrationDevice);
            ConfigureInitialAudioState();
        }

        public CarState State => _state;
        public float PositionX => _positionX;
        public float PositionY => _positionY;
        public float Speed => _speed;
        public int Frequency => _frequency;
        public int Gear => _gear;
        public bool InReverseGear => _gear == ReverseGear;
        public bool ManualTransmission
        {
            get => _manualTransmission;
            set => _manualTransmission = value;
        }
        public CarType CarType => _carType;
        public ICarListener? Listener
        {
            get => _listener;
            set => _listener = value;
        }
        public bool EngineRunning => _soundEngine.IsPlaying;
        public bool Braking => _soundBrake.IsPlaying;
        public bool Horning => _soundHorn.IsPlaying;
        public bool UserDefined => _userDefined;
        public string? CustomFile => _customFile;
        public string VehicleName { get; private set; } = "Vehicle";
        public float WidthM => _widthM;
        public float LengthM => _lengthM;

        // Engine simulation properties for reporting
        public float SpeedKmh => _engine.SpeedKmh;
        public float EngineRpm => _engine.Rpm;
        public float DistanceMeters => _engine.DistanceMeters;

        public void SetPhysicsModel(IModel model)
        {
            _physicsModel = model ?? throw new ArgumentNullException(nameof(model));
        }

        private CarControlContext BuildControlContext(float elapsed)
        {
            return new CarControlContext(
                _state,
                _started(),
                _manualTransmission,
                _gear,
                _speed,
                _positionX,
                _positionY,
                elapsed);
        }

        private void UpdateRuntimeContext(float elapsed)
        {
            _runtimeContext.State = _state;
            _runtimeContext.Started = _started();
            _runtimeContext.ManualTransmission = _manualTransmission;
            _runtimeContext.Gear = _gear;
            _runtimeContext.Speed = _speed;
            _runtimeContext.PositionX = _positionX;
            _runtimeContext.PositionY = _positionY;
            _runtimeContext.Elapsed = elapsed;
        }

        private void SetState(CarState nextState)
        {
            var previousState = _state;
            _state = nextState;
            NotifyStateChanged(previousState, nextState);
        }

        public virtual void Run(float elapsed)
        {
            RefreshCategoryVolumes();
            _lastAudioElapsed = elapsed;
            var controlContext = BuildControlContext(elapsed);
            var controlIntent = ResolveControlIntent(controlContext);
            OnBeforeRun(elapsed, controlContext, controlIntent);
            var horning = controlIntent.Horn;

            _physicsModel.Step(this, elapsed, controlIntent);

            _audioFlow.UpdateHorn(_soundHorn, _state, horning);
            _eventProcessor.ProcessDue(_events, _currentTime());

            UpdateRuntimeContext(elapsed);
            OnAfterRun(elapsed, BuildControlContext(elapsed), controlIntent);
        }
    }
}
