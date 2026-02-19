using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Protocol;
using TopSpeed.Tracks;
using TopSpeed.Tracks.Geometry;
using TopSpeed.Tracks.Map;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal sealed class ComputerPlayer : IDisposable
    {
        private const float CallLength = 30.0f;
        private const float BaseLateralSpeed = 7.0f;
        private const float StabilitySpeedRef = 45.0f;
        private const float AutoShiftHysteresis = 0.05f;
        private const float AutoShiftCooldownSeconds = 0.15f;
        private const float YieldSpeedKph = 10.0f;
        private const float AiTurnApproachMeters = 80.0f;
        private const float AiCruiseMinSpeedKph = 35.0f;
        private enum SurfaceKind
        {
            Asphalt,
            Gravel,
            Water,
            Sand,
            Snow,
            Other
        }

        private readonly AudioManager _audio;
        private readonly MapTrack _track;
        private readonly RaceSettings _settings;
        private readonly Func<float> _currentTime;
        private readonly Func<bool> _started;
        private readonly Action<string>? _debugSpeak;
        private readonly int _playerNumber;
        private readonly int _vehicleIndex;

        private readonly List<BotEvent> _events;
        private readonly string _legacyRoot;

        private ComputerState _state;
        private string _materialId;
        private SurfaceKind _surfaceKind;
        private int _gear;
        private float _speed;
        private float _positionX;
        private float _positionY;
        private MapMovementState _mapState;
        private float _networkDistanceMeters;
        private bool _networkControlled;
        private int _switchingGear;
        private float _autoShiftCooldown;
        private float _trackLength;

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
        private float _powerFactor;
        private VehiclePowertrainParameters _powertrainParams;
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
        private VehicleDynamicsModel _dynamicsModel;
        private VehicleDynamicsState _dynamicsState;
        private VehicleDynamicsParameters _dynamicsParams;
        private BicycleDynamicsParameters _bicycleParams;
        private float _widthM;
        private float _lengthM;
        private float _vehicleHeightM;
        private float _hornHeightM;
        private float _engineHeightM;
        private int _idleFreq;
        private int _topFreq;
        private int _shiftFreq;
        private int _gears;

        private int _prevFrequency;
        private int _frequency;
        private int _prevBrakeFrequency;
        private int _brakeFrequency;
        private float _laneWidth;
        private float _relPos;
        private float _nextRelPos;
        private float _diffX;
        private float _diffY;
        private int _currentSteering;
        private int _currentThrottle;
        private int _currentBrake;
        private float _currentSurfaceTractionFactor;
        private float _currentDeceleration;
        private float _speedDiff;
        private float _thrust;
        private int _difficulty;
        private bool _finished;
        private bool _horning;
        private bool _backfirePlayedAuto;
        private bool _networkBackfireActive;
        private float _aiMistakeCooldown;
        private float _aiMistakeRemaining;
        private float _aiMistakeSteerBias;
        private float _aiMistakeThrottleScale;
        private int _frame;
        private Vector3 _lastAudioPosition;
        private Vector3 _worldPosition;
        private Vector3 _worldForward;
        private Vector3 _worldUp;
        private Vector3 _worldVelocity;
        private bool _audioInitialized;
        private float _lastAudioUpdateTime;

        private AudioSourceHandle _soundEngine;
        private AudioSourceHandle _soundHorn;
        private AudioSourceHandle _soundStart;
        private AudioSourceHandle _soundCrash;
        private AudioSourceHandle _soundBrake;
        private AudioSourceHandle _soundMiniCrash;
        private AudioSourceHandle _soundBump;
        private AudioSourceHandle? _soundBackfire;

        private EngineModel _engine;

        public ComputerPlayer(
            AudioManager audio,
            MapTrack track,
            RaceSettings settings,
            int vehicleIndex,
            int playerNumber,
            Func<float> currentTime,
            Func<bool> started,
            Action<string>? debugSpeak = null)
        {
            _audio = audio;
            _track = track;
            _settings = settings;
            _playerNumber = playerNumber;
            _vehicleIndex = vehicleIndex;
            _currentTime = currentTime;
            _started = started;
            _debugSpeak = debugSpeak;
            _events = new List<BotEvent>();
            _legacyRoot = Path.Combine(AssetPaths.SoundsRoot, "Legacy");

            _materialId = NormalizeMaterialId("asphalt");
            _surfaceKind = ResolveSurfaceKind(_materialId);
            _gear = 1;
            _state = ComputerState.Stopped;
            _switchingGear = 0;
            _horning = false;
            _difficulty = (int)settings.Difficulty;
            _prevFrequency = 0;
            _prevBrakeFrequency = 0;
            _brakeFrequency = 0;
            _laneWidth = 0;
            _relPos = 0f;
            _nextRelPos = 0f;
            _diffX = 0;
            _diffY = 0;
            _currentSteering = 0;
            _currentThrottle = 0;
            _currentBrake = 0;
            _currentSurfaceTractionFactor = 0;
            _currentDeceleration = 0;
            _speedDiff = 0;
            _thrust = 0;
            _speed = 0;
            _frame = 1;
            _finished = false;
            _networkBackfireActive = false;
            _aiMistakeCooldown = 1.0f;
            _aiMistakeRemaining = 0f;
            _aiMistakeSteerBias = 0f;
            _aiMistakeThrottleScale = 1f;

            var definition = VehicleLoader.LoadOfficial(vehicleIndex, track.Weather);
            _dynamicsModel = definition.DynamicsModel;
            _surfaceTractionFactor = definition.SurfaceTractionFactor;
            _deceleration = definition.Deceleration;
            _topSpeed = definition.TopSpeed;
            _massKg = Math.Max(1f, definition.MassKg);
            _drivetrainEfficiency = Math.Max(0.1f, Math.Min(1.0f, definition.DrivetrainEfficiency));
            _engineBrakingTorqueNm = Math.Max(0f, definition.EngineBrakingTorqueNm);
            _tireGripCoefficient = Math.Max(0.1f, definition.TireGripCoefficient);
            _brakeStrength = Math.Max(0.1f, definition.BrakeStrength);
            _wheelRadiusM = Math.Max(0.01f, definition.TireCircumferenceM / (2.0f * (float)Math.PI));
            _engineBraking = Math.Max(0.05f, Math.Min(1.0f, definition.EngineBraking));
            _idleRpm = definition.IdleRpm;
            _revLimiter = definition.RevLimiter;
            _finalDriveRatio = definition.FinalDriveRatio;
            _powerFactor = Math.Max(0.1f, definition.PowerFactor);
            _peakTorqueNm = Math.Max(0f, definition.PeakTorqueNm);
            _peakTorqueRpm = Math.Max(_idleRpm + 100f, definition.PeakTorqueRpm);
            _idleTorqueNm = Math.Max(0f, definition.IdleTorqueNm);
            _redlineTorqueNm = Math.Max(0f, definition.RedlineTorqueNm);
            _dragCoefficient = Math.Max(0.01f, definition.DragCoefficient);
            _frontalAreaM2 = Math.Max(0.1f, definition.FrontalAreaM2);
            _rollingResistanceCoefficient = Math.Max(0.001f, definition.RollingResistanceCoefficient);
            _launchRpm = Math.Max(_idleRpm, Math.Min(_revLimiter, definition.LaunchRpm));
            _lateralGripCoefficient = Math.Max(0.1f, definition.LateralGripCoefficient);
            _highSpeedStability = Math.Max(0f, Math.Min(1.0f, definition.HighSpeedStability));
            _wheelbaseM = Math.Max(0.5f, definition.WheelbaseM);
            _maxSteerDeg = Math.Max(5f, Math.Min(60f, definition.MaxSteerDeg));
            _widthM = Math.Max(0.5f, definition.WidthM);
            _lengthM = Math.Max(0.5f, definition.LengthM);
            _vehicleHeightM = VehicleAudioHeights.ResolveVehicleHeight(definition);
            _hornHeightM = VehicleAudioHeights.ResolveHornHeight(definition, _vehicleHeightM);
            _engineHeightM = VehicleAudioHeights.ResolveEngineHeight(definition);
            var dynamicsSetup = VehicleDynamicsSetupBuilder.Build(
                definition,
                _massKg,
                _tireGripCoefficient,
                _lateralGripCoefficient,
                _dragCoefficient,
                _frontalAreaM2,
                _rollingResistanceCoefficient,
                _topSpeed,
                _wheelbaseM,
                _maxSteerDeg,
                _widthM,
                _lengthM);
            _dynamicsParams = dynamicsSetup.FourWheel;
            _bicycleParams = dynamicsSetup.Bicycle;
            _powertrainParams = new VehiclePowertrainParameters
            {
                MassKg = _massKg,
                WheelRadiusM = _wheelRadiusM,
                FinalDriveRatio = _finalDriveRatio,
                DrivetrainEfficiency = _drivetrainEfficiency,
                EngineBrakingTorqueNm = _engineBrakingTorqueNm,
                EngineBraking = _engineBraking,
                IdleRpm = _idleRpm,
                RevLimiter = _revLimiter,
                LaunchRpm = _launchRpm,
                PowerFactor = _powerFactor,
                PeakTorqueNm = _peakTorqueNm,
                PeakTorqueRpm = _peakTorqueRpm,
                IdleTorqueNm = _idleTorqueNm,
                RedlineTorqueNm = _redlineTorqueNm,
                TireGripCoefficient = _tireGripCoefficient,
                BrakeStrength = _brakeStrength,
                DragCoefficient = _dragCoefficient,
                FrontalAreaM2 = _frontalAreaM2,
                RollingResistanceCoefficient = _rollingResistanceCoefficient
            };
            _idleFreq = definition.IdleFreq;
            _topFreq = definition.TopFreq;
            _shiftFreq = definition.ShiftFreq;
            _gears = definition.Gears;
            _frequency = _idleFreq;

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

            _soundEngine = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Engine), "engine", looped: true);
            _soundStart = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Start), "start");
            _soundHorn = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Horn), "horn", looped: true);
            _soundCrash = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Crash), "crash");
            _soundBrake = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Brake), "brake", looped: true);
            _soundEngine.SetDopplerFactor(1f);
            _soundHorn.SetDopplerFactor(0f);
            _soundBrake.SetDopplerFactor(0f);
            _soundMiniCrash = CreateRequiredSound(Path.Combine(_legacyRoot, "crashshort.wav"), "mini crash");
            _soundBump = CreateRequiredSound(Path.Combine(_legacyRoot, "bump.wav"), "bump");
            _soundCrash.SetDopplerFactor(0f);
            _soundMiniCrash.SetDopplerFactor(0f);
            _soundBump.SetDopplerFactor(0f);
            _soundBackfire = TryCreateSound(definition.GetSoundPath(VehicleAction.Backfire));
        }

        public ComputerState State => _state;
        public float PositionX => _positionX;
        public float PositionY => _positionY;
        public float DistanceMeters => _networkControlled ? _networkDistanceMeters : _mapState.DistanceMeters;
        public MapMovementState MapState => _mapState;
        public float Speed => _speed;
        public Vector3 WorldPosition => _worldPosition;
        public int PlayerNumber => _playerNumber;
        public int VehicleIndex => _vehicleIndex;
        public bool Finished => _finished;
        public void SetFinished(bool value) => _finished = value;
        public float WidthM => _widthM;
        public float LengthM => _lengthM;

        public void Initialize(float positionX, float positionY, float trackLength)
        {
            if (Math.Abs(positionX) > 0.001f || Math.Abs(positionY) > 0.001f)
                _mapState = _track.CreateStateFromWorld(new Vector3(positionX, 0f, positionY), _track.Map.StartHeadingDegrees);
            else
                _mapState = _track.CreateStartState();
            _positionX = 0f;
            _positionY = _mapState.DistanceMeters;
            _networkControlled = false;
            _networkDistanceMeters = _mapState.DistanceMeters;
            _trackLength = trackLength;
            _laneWidth = _track.LaneWidth;
            _audioInitialized = false;
            var pose = _track.GetPose(_mapState);
            _dynamicsState = new VehicleDynamicsState
            {
                VelLong = 0f,
                VelLat = 0f,
                Yaw = pose.HeadingRadians,
                YawRate = 0f,
                SteerInput = 0f,
                SteerWheelAngleRad = 0f,
                SteerWheelAngleDeg = 0f
            };
            _worldPosition = pose.Position;
            _worldForward = pose.Tangent;
            _worldUp = Vector3.UnitY;
            _worldVelocity = Vector3.Zero;
            _lastAudioPosition = _worldPosition;
            _lastAudioUpdateTime = 0f;
        }

        public void FinalizePlayer()
        {
            _soundEngine.Stop();
        }

        public void PendingStart(float baseDelay)
        {
            float difficultyDelay;
            var randomValue = Algorithm.RandomInt(100) / 100f;

            switch (_difficulty)
            {
                case 2: // Hard
                    difficultyDelay = 0.1f + (randomValue * 0.4f);
                    break;
                case 1: // Normal
                    difficultyDelay = 1.0f + (randomValue * 1.5f);
                    break;
                case 0: // Easy
                default:
                    difficultyDelay = 2.5f + (randomValue * 2.5f);
                    break;
            }

            var startTime = baseDelay + difficultyDelay;
            PushEvent(BotEventType.CarComputerStart, startTime);
        }

        public void Start()
        {
            var delay = Math.Max(0f, _soundStart.GetLengthSeconds() - 0.1f);
            PushEvent(BotEventType.CarStart, delay);
            _soundStart.Play(loop: false);
            _speed = 0;
            _dynamicsState = new VehicleDynamicsState
            {
                VelLong = 0f,
                VelLat = 0f,
                Yaw = _track.GetPose(_mapState).HeadingRadians,
                YawRate = 0f,
                SteerInput = 0f,
                SteerWheelAngleRad = 0f,
                SteerWheelAngleDeg = 0f
            };
            _prevFrequency = _idleFreq;
            _frequency = _idleFreq;
            _prevBrakeFrequency = 0;
            _brakeFrequency = 0;
            _switchingGear = 0;
            _state = ComputerState.Starting;
        }

        public void Crash(float newPosition)
        {
            _speed = 0;
            _soundCrash.Play(loop: false);
            _soundEngine.Stop();
            _soundEngine.SeekToStart();
            _soundEngine.SetPanPercent(0);
            _soundBrake.Stop();
            _soundBrake.SeekToStart();
            _soundHorn.Stop();
            _gear = 1;
            _positionX = newPosition;
            _state = ComputerState.Crashing;
            PushEvent(BotEventType.CarRestart, _soundCrash.GetLengthSeconds() + 1.25f);
        }

        public void MiniCrash(float newPosition)
        {
            _speed /= 4;
            _positionX = newPosition;
            _soundMiniCrash.Play(loop: false);
        }

        public void Bump(float bumpX, float bumpY, float bumpSpeed)
        {
            if (bumpY != 0)
            {
                _speed -= bumpSpeed;
                _positionY += bumpY;
            }

            if (bumpX > 0)
            {
                _positionX += 2 * bumpX;
                _speed -= _speed / 5;
            }
            else if (bumpX < 0)
            {
                _positionX += 2 * bumpX;
                _speed -= _speed / 5;
            }

            if (_speed < 0)
                _speed = 0;
            _soundBump.Play(loop: false);
            Horn();
        }

        public void Stop()
        {
            _state = ComputerState.Stopping;
        }

        public void Quiet()
        {
            _soundBrake.Stop();
            _soundHorn.Stop();
            _soundEngine.SetVolumePercent(80);
            if (_soundBackfire != null)
                _soundBackfire.SetVolumePercent(80);
        }

        public void Run(float elapsed, float playerX, float playerY)
        {
            _diffX = _worldPosition.X - playerX;
            _diffY = _worldPosition.Z - playerY;


            if (!_horning && _diffY < -100.0f)
            {
                if (Algorithm.RandomInt(2500) == 1)
                {
                    var duration = Algorithm.RandomInt(80);
                    _horning = true;
                    PushEvent(BotEventType.StopHorn, 0.2f + (duration / 80.0f));
                }
            }

            UpdateSpatialAudio(playerX, playerY, _trackLength, elapsed);

            if (_state == ComputerState.Running && _started())
            {
                AI(elapsed);

                _currentSurfaceTractionFactor = _surfaceTractionFactor;
                _currentDeceleration = _deceleration;
                _speedDiff = 0;
                switch (_surfaceKind)
                {
                    case SurfaceKind.Gravel:
                        _currentSurfaceTractionFactor = (_currentSurfaceTractionFactor * 2) / 3;
                        _currentDeceleration = (_currentDeceleration * 2) / 3;
                        break;
                    case SurfaceKind.Water:
                        _currentSurfaceTractionFactor = (_currentSurfaceTractionFactor * 3) / 5;
                        _currentDeceleration = (_currentDeceleration * 3) / 5;
                        break;
                    case SurfaceKind.Sand:
                        _currentSurfaceTractionFactor = _currentSurfaceTractionFactor / 2;
                        _currentDeceleration = (_currentDeceleration * 3) / 2;
                        break;
                    case SurfaceKind.Snow:
                        _currentDeceleration = _currentDeceleration / 2;
                        break;
                }

                if (_currentThrottle == 0)
                {
                    _thrust = _currentBrake;
                    if (_currentBrake != 0)
                    {
                        if (_surfaceKind == SurfaceKind.Asphalt && !_soundBrake.IsPlaying)
                            _soundBrake.Play(loop: true);
                        else if (_surfaceKind != SurfaceKind.Asphalt)
                            _soundBrake.Stop();
                    }
                }
                else if (_currentBrake == 0)
                {
                    _thrust = _currentThrottle;
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                }
                else if (-_currentBrake > _currentThrottle)
                {
                    _thrust = _currentBrake;
                }

                var throttle = Math.Max(0f, Math.Min(100f, _currentThrottle)) / 100f;
                var brakeInput = Math.Max(0f, Math.Min(100f, -_currentBrake)) / 100f;
                var surfaceTractionMod = _surfaceTractionFactor > 0f
                    ? _currentSurfaceTractionFactor / _surfaceTractionFactor
                    : 1.0f;

                var speedForwardMps = Math.Abs(_dynamicsState.VelLong);
                var driveForce = 0f;
                if (_thrust > 10)
                {
                    var driveRpm = VehiclePowertrainMath.CalculateDriveRpm(_engine, _gear, speedForwardMps, throttle, _powertrainParams);
                    var engineTorque = VehiclePowertrainMath.CalculateEngineTorqueNm(driveRpm, _powertrainParams) * throttle * _powerFactor;
                    var gearRatio = _engine.GetGearRatio(_gear);
                    var wheelTorque = engineTorque * gearRatio * _finalDriveRatio * _drivetrainEfficiency;
                    driveForce = wheelTorque / _wheelRadiusM;
                    _lastDriveRpm = VehiclePowertrainMath.CalculateDriveRpm(_engine, _gear, speedForwardMps, throttle, _powertrainParams);
                }
                else
                {
                    _lastDriveRpm = 0f;
                }

                var surfaceDecelMod = _deceleration > 0f ? _currentDeceleration / _deceleration : 1.0f;
                var brakeForce = brakeInput > 0f
                    ? _massKg * (VehiclePowertrainMath.CalculateBrakeDecel(brakeInput, surfaceDecelMod, _powertrainParams) / 3.6f)
                    : 0f;
                var engineBrakeForce = throttle > 0.05f
                    ? 0f
                    : _massKg * (VehiclePowertrainMath.CalculateEngineBrakingDecel(_engine, _gear, surfaceDecelMod, _powertrainParams) / 3.6f);

                var inputs = new VehicleDynamicsInputs
                {
                    Elapsed = elapsed,
                    SteeringCommand = _currentSteering,
                    DriveForce = driveForce,
                    BrakeForce = brakeForce,
                    EngineBrakeForce = engineBrakeForce,
                    SurfaceTractionMod = surfaceTractionMod,
                    TireGripCoefficient = _tireGripCoefficient,
                    LateralGripCoefficient = _lateralGripCoefficient
                };
                var dynamics = _dynamicsModel == VehicleDynamicsModel.Bicycle
                    ? BicycleDynamics.Step(ref _dynamicsState, _bicycleParams, inputs)
                    : VehicleDynamics.Step(ref _dynamicsState, _dynamicsParams, inputs);
                _speed = dynamics.SpeedKph;
                _speedDiff = dynamics.SpeedDiffKph;
                var longitudinalGripFactor = dynamics.LongitudinalGripFactor;
                if (_speed < 0f)
                    _speed = 0f;

                var speedForGearKph = Math.Abs(_dynamicsState.VelLong) * 3.6f;
                UpdateAutomaticGear(elapsed, speedForGearKph / 3.6f, throttle, surfaceTractionMod, longitudinalGripFactor);
                _engine.SyncFromSpeed(speedForGearKph, _gear, elapsed, _currentThrottle);
                if (_lastDriveRpm > 0f && _lastDriveRpm > _engine.Rpm)
                    _engine.OverrideRpm(_lastDriveRpm);
                if (_thrust < -50 && _speed > 0)
                    _currentSteering = _currentSteering * 2 / 3;
                var headingDegrees = MapMovement.HeadingFromYaw(_dynamicsState.Yaw);
                var distanceMeters = (_speed / 3.6f) * elapsed;
                var previousPosition = _worldPosition;
                _track.TryMove(ref _mapState, distanceMeters, headingDegrees, out _, out var boundaryHit);
                if (boundaryHit)
                {
                    _speed = 0f;
                    _dynamicsState.VelLong = 0f;
                    _dynamicsState.VelLat = 0f;
                }
                else
                {
                    ApplySectorSpeedRules(_mapState.WorldPosition, headingDegrees);
                }
                _worldPosition = _mapState.WorldPosition;
                _positionY = _mapState.DistanceMeters;
                var worldVelocity = elapsed > 0f ? (_worldPosition - previousPosition) / elapsed : Vector3.Zero;
                if (_track.TryGetSurfaceOrientation(_worldPosition, headingDegrees, out var surfaceForward, out var surfaceUp))
                {
                    _worldForward = surfaceForward;
                    _worldUp = surfaceUp;
                }
                else
                {
                    _worldForward = MapMovement.HeadingVector(headingDegrees);
                    _worldUp = Vector3.UnitY;
                }
                _worldVelocity = worldVelocity;

                var forwardAxis = _worldForward.LengthSquared() > 0.0001f ? Vector3.Normalize(_worldForward) : Vector3.UnitZ;
                var upAxis = _worldUp.LengthSquared() > 0.0001f ? Vector3.Normalize(_worldUp) : Vector3.UnitY;
                var rightAxis = Vector3.Cross(upAxis, forwardAxis);
                if (rightAxis.LengthSquared() > 0.0001f)
                    rightAxis = Vector3.Normalize(rightAxis);
                else
                    rightAxis = new Vector3(forwardAxis.Z, 0f, -forwardAxis.X);

                var lateralDelta = Vector3.Dot(_worldPosition - previousPosition, rightAxis);
                if (!float.IsNaN(lateralDelta) && !float.IsInfinity(lateralDelta))
                    _positionX += lateralDelta;

                if (_frame % 4 == 0)
                {
                    _frame = 0;
                    _brakeFrequency = (int)(11025 + 22050 * _speed / _topSpeed);
                    if (_brakeFrequency != _prevBrakeFrequency)
                    {
                        _soundBrake.SetFrequency(_brakeFrequency);
                        _prevBrakeFrequency = _brakeFrequency;
                    }
                    UpdateEngineFreq();
                }

                var road = _track.RoadAt(_mapState);
                if (!_finished)
                    Evaluate(road);
            }
            else if (_state == ComputerState.Stopping)
            {
                _speed -= (elapsed * 100 * _deceleration);
                if (_speed < 0)
                    _speed = 0;
                if (_frame % 4 == 0)
                {
                    _frame = 0;
                    UpdateEngineFreq();
                }
                _frame++;
            }

            if (_horning && _state == ComputerState.Running)
            {
                if (!_soundHorn.IsPlaying)
                    _soundHorn.Play(loop: true);
            }
            else
            {
                if (_soundHorn.IsPlaying)
                    _soundHorn.Stop();
            }

            for (var i = _events.Count - 1; i >= 0; i--)
            {
                var e = _events[i];
                if (e.Time < _currentTime())
                {
                    _events.RemoveAt(i);
                    switch (e.Type)
                    {
                        case BotEventType.CarStart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarStart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} engine start.");
                            _soundEngine.SetFrequency(_idleFreq);
                            _soundEngine.Play(loop: true);
                            _state = ComputerState.Running;
                            break;
                        case BotEventType.CarComputerStart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarComputerStart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} start trigger.");
                            Start();
                            break;
                        case BotEventType.CarRestart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarRestart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} restart trigger.");
                            Start();
                            break;
                        case BotEventType.InGear:
                            _switchingGear = 0;
                            break;
                        case BotEventType.StopHorn:
                            _horning = false;
                            break;
                        case BotEventType.StartHorn:
                            _horning = true;
                            break;
                    }
                }
            }
        }

        public void ApplyNetworkState(
            float positionX,
            float positionY,
            float speed,
            int frequency,
            bool engineRunning,
            bool braking,
            bool horning,
            bool backfiring,
            float playerX,
            float playerY,
            float trackLength)
        {
            _positionX = 0f;
            _speed = speed;
            _trackLength = trackLength;
            _state = ComputerState.Running;
            _networkControlled = true;

            var worldPosition = new Vector3(positionX, 0f, positionY);
            _worldPosition = worldPosition;
            _mapState = _track.CreateStateFromWorld(worldPosition, _mapState.HeadingDegrees);
            _mapState.WorldPosition = worldPosition;

            var now = _currentTime();
            var elapsed = 0f;
            if (_audioInitialized)
            {
                elapsed = now - _lastAudioUpdateTime;
                if (elapsed < 0f)
                    elapsed = 0f;
            }
            _lastAudioUpdateTime = now;

            var speedMps = Math.Max(0f, speed / 3.6f);
            _networkDistanceMeters += speedMps * elapsed;
            _mapState.DistanceMeters = _networkDistanceMeters;
            _positionY = _networkDistanceMeters;

            _diffX = _worldPosition.X - playerX;
            _diffY = _worldPosition.Z - playerY;

            UpdateSpatialAudio(playerX, playerY, _trackLength, elapsed);

            if (engineRunning)
            {
                if (!_soundEngine.IsPlaying)
                    _soundEngine.Play(loop: true);
                var targetFrequency = frequency > 0 ? frequency : _idleFreq;
                if (_prevFrequency != targetFrequency)
                {
                    _soundEngine.SetFrequency(targetFrequency);
                    _prevFrequency = targetFrequency;
                }
            }
            else if (_soundEngine.IsPlaying)
            {
                _soundEngine.Stop();
            }

            if (braking)
            {
                if (!_soundBrake.IsPlaying)
                    _soundBrake.Play(loop: true);
                var targetBrakeFrequency = (int)(11025 + 22050 * _speed / _topSpeed);
                if (_prevBrakeFrequency != targetBrakeFrequency)
                {
                    _soundBrake.SetFrequency(targetBrakeFrequency);
                    _prevBrakeFrequency = targetBrakeFrequency;
                }
            }
            else if (_soundBrake.IsPlaying)
            {
                _soundBrake.Stop();
            }

            if (horning)
            {
                if (!_soundHorn.IsPlaying)
                    _soundHorn.Play(loop: true);
            }
            else if (_soundHorn.IsPlaying)
            {
                _soundHorn.Stop();
            }

            if (backfiring && !_networkBackfireActive && _soundBackfire != null)
            {
                _soundBackfire.Stop();
                _soundBackfire.SeekToStart();
                _soundBackfire.Play(loop: false);
            }
            _networkBackfireActive = backfiring;
        }

        public void Evaluate(TrackRoad road)
        {
            if (_state == ComputerState.Running && _started())
            {
                if (_frame % 4 == 0)
                {
                    _relPos = (_positionX - road.Left) / (_laneWidth * 2.0f);
                }
            }

            _materialId = NormalizeMaterialId(road.MaterialId);
            _surfaceKind = ResolveSurfaceKind(_materialId);
            _frame++;
        }

        private void ApplySectorSpeedRules(Vector3 worldPosition, float headingDegrees)
        {
            if (!_track.TryGetSectorRules(worldPosition, headingDegrees, out _, out var rules, out _, out _))
                return;

            var speedCap = rules.MaxSpeedKph;
            if (rules.RequiresYield)
            {
                var yieldCap = YieldSpeedKph;
                speedCap = speedCap.HasValue ? Math.Min(speedCap.Value, yieldCap) : yieldCap;
            }

            if (rules.RequiresStop)
                speedCap = 0f;

            if (!speedCap.HasValue)
                return;
            if (_speed <= speedCap.Value)
                return;

            var cap = Math.Max(0f, speedCap.Value);
            var factor = _speed > 0f ? cap / _speed : 0f;
            _speed = cap;
            _dynamicsState.VelLong *= factor;
            _dynamicsState.VelLat *= factor;
        }

        public void Pause()
        {
            if (_state == ComputerState.Starting)
                _soundStart.Stop();
            else if (_state == ComputerState.Running || _state == ComputerState.Stopping)
                _soundEngine.Stop();
            if (_soundBrake.IsPlaying)
                _soundBrake.Stop();
            if (_soundHorn.IsPlaying)
                _soundHorn.Stop();
            if (_soundBackfire != null && _soundBackfire.IsPlaying)
            {
                _soundBackfire.Stop();
                _soundBackfire.SeekToStart();
            }
            if (_soundCrash.IsPlaying)
            {
                _soundCrash.Stop();
                _soundCrash.SeekToStart();
            }
        }

        public void Unpause()
        {
            if (_state == ComputerState.Starting)
                _soundStart.Play(loop: false);
            else if (_state == ComputerState.Running || _state == ComputerState.Stopping)
                _soundEngine.Play(loop: true);
        }

        public void Dispose()
        {
            _soundEngine.Dispose();
            _soundHorn.Dispose();
            _soundStart.Dispose();
            _soundCrash.Dispose();
            _soundBrake.Dispose();
            _soundMiniCrash.Dispose();
            _soundBump.Dispose();
            _soundBackfire?.Dispose();
        }

        private void AI(float elapsed)
        {
            var road = _track.RoadAt(_mapState);
            var roadWidth = Math.Max(1f, road.Right - road.Left);
            _laneWidth = roadWidth * 0.5f;
            _relPos = (_positionX - road.Left) / roadWidth;
            _nextRelPos = _relPos;

            _currentSteering = 0;
            _currentThrottle = 100;
            _currentBrake = 0;

            var currentHeading = NormalizeDegrees(_dynamicsState.Yaw * 180.0f / (float)Math.PI);
            var targetCruiseSpeed = Math.Max(AiCruiseMinSpeedKph, _topSpeed * ResolveDifficultyCruiseFactor());
            var desiredSpeed = targetCruiseSpeed;
            var inTurnWindow = false;
            var guidanceActive = false;

            if (_track.TryGetTurnGuidance(_worldPosition, currentHeading, out var guidance) && !guidance.Passed)
            {
                var distanceToTurn = Math.Max(0f, guidance.DistanceMeters);
                inTurnWindow = guidance.InTurnWindow;
                var activationRange = AiTurnApproachMeters;
                if (guidance.GuidanceRangeMeters > 0f)
                    activationRange = Math.Min(activationRange, guidance.GuidanceRangeMeters);
                guidanceActive = inTurnWindow || distanceToTurn <= activationRange;

                if (guidanceActive)
                {
                    var headingDelta = SignedDeltaDegrees(currentHeading, guidance.TurnHeadingDegrees);
                    var absHeadingDelta = Math.Abs(headingDelta);
                    var turnSeverity = Clamp(absHeadingDelta / 95f, 0f, 1f);

                    if (inTurnWindow)
                    {
                        var inWindowCap = Lerp(65f, 28f, turnSeverity);
                        desiredSpeed = Math.Min(desiredSpeed, inWindowCap);
                    }
                    else if (distanceToTurn <= AiTurnApproachMeters)
                    {
                        var approach = 1f - Clamp(distanceToTurn / AiTurnApproachMeters, 0f, 1f);
                        var turnCap = Lerp(130f, 32f, turnSeverity);
                        desiredSpeed = Math.Min(desiredSpeed, Lerp(targetCruiseSpeed, turnCap, approach));
                    }

                    var steerGain = inTurnWindow ? 1.25f : 0.9f;
                    var steerCommand = (headingDelta / 45f) * 100f * steerGain;
                    if (!inTurnWindow && distanceToTurn > 20f)
                    {
                        var laneCenterBias = Clamp((0.5f - _relPos) * 120f, -35f, 35f);
                        steerCommand += laneCenterBias * 0.25f;
                    }

                    var steerLimit = _speed < 25f ? 45f : 100f;
                    _currentSteering = (int)Math.Round(Clamp(steerCommand, -steerLimit, steerLimit));
                }
            }

            if (!guidanceActive)
            {
                var laneCenterError = 0.5f - _relPos;
                var laneSteer = Clamp(laneCenterError * 200f, -100f, 100f);
                _currentSteering = (int)Math.Round(laneSteer);
            }

            // Keep spacing from player to reduce bot-player collisions.
            var toPlayer = new Vector2(-_diffX, -_diffY);
            var forward2 = new Vector2(_worldForward.X, _worldForward.Z);
            if (forward2.LengthSquared() < 0.0001f)
            {
                var headingRad = _dynamicsState.Yaw;
                forward2 = new Vector2((float)Math.Sin(headingRad), (float)Math.Cos(headingRad));
            }
            if (forward2.LengthSquared() > 0.0001f)
            {
                forward2 = Vector2.Normalize(forward2);
                var right2 = new Vector2(forward2.Y, -forward2.X);
                var along = Vector2.Dot(toPlayer, forward2);
                var side = Vector2.Dot(toPlayer, right2);
                if (along > -2f && along < 14f && Math.Abs(side) < 4.5f)
                {
                    desiredSpeed = Math.Min(desiredSpeed, Math.Max(8f, along * 1.5f));
                    var avoidSteer = side >= 0f ? -20f : 20f;
                    _currentSteering = (int)Math.Round(Clamp(_currentSteering + avoidSteer, -100f, 100f));
                }
            }

            UpdateAiMistake(elapsed);
            if (_aiMistakeRemaining > 0f)
            {
                var steer = _currentSteering + _aiMistakeSteerBias;
                _currentSteering = (int)Math.Round(Clamp(steer, -100f, 100f));
                desiredSpeed *= _aiMistakeThrottleScale;
            }

            desiredSpeed = Clamp(desiredSpeed, AiCruiseMinSpeedKph, _topSpeed);
            var speedError = desiredSpeed - _speed;
            if (speedError >= 6f)
            {
                _currentBrake = 0;
                _currentThrottle = (int)Math.Round(Clamp(40f + (speedError * 2.5f), 45f, 100f));
            }
            else if (speedError <= -2f)
            {
                _currentThrottle = 0;
                var brakeBase = Clamp((-speedError * 3f) + (inTurnWindow ? 18f : 0f), 18f, 100f);
                _currentBrake = -(int)Math.Round(brakeBase);
            }
            else
            {
                _currentBrake = 0;
                _currentThrottle = (int)Math.Round(Clamp(30f + (speedError * 2f), 20f, 55f));
            }
        }

        private void UpdateAiMistake(float elapsed)
        {
            _aiMistakeCooldown = Math.Max(0f, _aiMistakeCooldown - elapsed);
            if (_aiMistakeRemaining > 0f)
            {
                _aiMistakeRemaining = Math.Max(0f, _aiMistakeRemaining - elapsed);
                if (_aiMistakeRemaining <= 0f)
                {
                    _aiMistakeSteerBias = 0f;
                    _aiMistakeThrottleScale = 1f;
                }
                return;
            }

            if (_aiMistakeCooldown > 0f)
                return;

            var triggerChance = _difficulty switch
            {
                0 => 45,
                1 => 25,
                _ => 10
            };

            if (Algorithm.RandomInt(100) < triggerChance)
            {
                var steerBiasBase = _difficulty switch
                {
                    0 => 30f,
                    1 => 22f,
                    _ => 14f
                };
                var steerBiasRange = _difficulty switch
                {
                    0 => 25f,
                    1 => 18f,
                    _ => 12f
                };
                var direction = Algorithm.RandomInt(2) == 0 ? -1f : 1f;
                _aiMistakeSteerBias = direction * (steerBiasBase + ((Algorithm.RandomInt(100) / 100f) * steerBiasRange));
                _aiMistakeThrottleScale = _difficulty switch
                {
                    0 => 0.6f + (Algorithm.RandomInt(50) / 100f),
                    1 => 0.75f + (Algorithm.RandomInt(40) / 100f),
                    _ => 0.85f + (Algorithm.RandomInt(25) / 100f)
                };
                _aiMistakeRemaining = 0.35f + (Algorithm.RandomInt(85) / 100f);
            }

            _aiMistakeCooldown = _difficulty switch
            {
                0 => 2.0f + (Algorithm.RandomInt(80) / 100f),
                1 => 3.0f + (Algorithm.RandomInt(110) / 100f),
                _ => 4.0f + (Algorithm.RandomInt(140) / 100f)
            };
        }

        private float ResolveDifficultyCruiseFactor()
        {
            return _difficulty switch
            {
                0 => 0.70f,
                1 => 0.80f,
                _ => 0.90f
            };
        }

        private void Horn()
        {
            var duration = Algorithm.RandomInt(80);
            PushEvent(BotEventType.StartHorn, 0.3f);
            PushEvent(BotEventType.StopHorn, 0.5f + duration / 80.0f);
        }

        private void PushEvent(BotEventType type, float time)
        {
            _events.Add(new BotEvent { Type = type, Time = _currentTime() + time });
        }

        private void UpdateEngineFreq()
        {
            var gearForSound = _engine.GetGearForSpeedKmh(_speed);
            var gearRange = _engine.GetGearRangeKmh(gearForSound);
            var gearMin = _engine.GetGearMinSpeedKmh(gearForSound);

            if (gearForSound == 1)
            {
                var gearSpeed = gearRange <= 0f ? 0f : Math.Min(1.0f, _speed / gearRange);
                _frequency = (int)(gearSpeed * (_topFreq - _idleFreq)) + _idleFreq;
            }
            else
            {
                var gearSpeed = (_speed - gearMin) / (float)gearRange;
                if (gearSpeed < 0.07f)
                {
                    _frequency = (int)(((0.07f - gearSpeed) / 0.07f) * (_topFreq - _shiftFreq) + _shiftFreq);
                    if (_soundBackfire != null)
                    {
                        if (!_backfirePlayedAuto)
                        {
                            if (Algorithm.RandomInt(5) == 1 && !_soundBackfire.IsPlaying)
                                _soundBackfire.Play(loop: false);
                        }
                        _backfirePlayedAuto = true;
                    }
                }
                else
                {
                    _frequency = (int)(gearSpeed * (_topFreq - _shiftFreq) + _shiftFreq);
                    if (_soundBackfire != null && _backfirePlayedAuto)
                        _backfirePlayedAuto = false;
                }
            }

            if (_switchingGear != 0)
                _frequency = (_frequency + _prevFrequency * 2) / 3;

            if (_frequency != _prevFrequency)
            {
                _soundEngine.SetFrequency(_frequency);
                _prevFrequency = _frequency;
            }
        }

        private void UpdateAutomaticGear(float elapsed, float speedMps, float throttle, float surfaceTractionMod, float longitudinalGripFactor)
        {
            if (_gears <= 1)
                return;

            if (_autoShiftCooldown > 0f)
            {
                _autoShiftCooldown -= elapsed;
                return;
            }

            var decision = VehiclePowertrainMath.SelectAutomaticGear(
                _engine,
                _gear,
                _gears,
                speedMps,
                throttle,
                surfaceTractionMod,
                longitudinalGripFactor,
                _powertrainParams);
            if (decision.ShouldShift)
                ShiftAutomaticGear(decision.TargetGear);
        }

        private void ShiftAutomaticGear(int newGear)
        {
            if (newGear == _gear)
                return;
            _switchingGear = newGear > _gear ? 1 : -1;
            _gear = newGear;
            PushEvent(BotEventType.InGear, 0.2f);
            _autoShiftCooldown = AutoShiftCooldownSeconds;
        }

        private void UpdateSpatialAudio(float listenerX, float listenerY, float trackLength, float elapsed)
        {
            var worldPos = _worldPosition;
            var vehiclePos = worldPos + (Vector3.UnitY * _vehicleHeightM);
            var enginePos = worldPos + (Vector3.UnitY * _engineHeightM);
            var hornPos = worldPos + (Vector3.UnitY * _hornHeightM);

            var velocity = Vector3.Zero;
            if (_audioInitialized && elapsed > 0f)
            {
                var velUnits = new Vector3((worldPos.X - _lastAudioPosition.X) / elapsed, 0f, (worldPos.Z - _lastAudioPosition.Z) / elapsed);
                velocity = AudioWorld.ToMeters(velUnits);
            }
            _lastAudioPosition = worldPos;
            _audioInitialized = true;

            SetSpatial(_soundEngine, AudioWorld.ToMeters(enginePos), velocity);
            SetSpatial(_soundStart, AudioWorld.ToMeters(enginePos), velocity);
            SetSpatial(_soundHorn, AudioWorld.ToMeters(hornPos), velocity);
            SetSpatial(_soundCrash, AudioWorld.ToMeters(vehiclePos), velocity);
            SetSpatial(_soundBrake, AudioWorld.ToMeters(vehiclePos), velocity);
            SetSpatial(_soundBackfire, AudioWorld.ToMeters(enginePos), velocity);
            SetSpatial(_soundBump, AudioWorld.ToMeters(vehiclePos), velocity);
            SetSpatial(_soundMiniCrash, AudioWorld.ToMeters(vehiclePos), velocity);
        }

        private static string NormalizeMaterialId(string? materialId)
        {
            if (materialId == null)
                return "asphalt";
            var trimmed = materialId.Trim();
            if (trimmed.Length == 0)
                return "asphalt";
            return trimmed;
        }

        private static SurfaceKind ResolveSurfaceKind(string materialId)
        {
            if (string.IsNullOrWhiteSpace(materialId))
                return SurfaceKind.Asphalt;
            switch (materialId.Trim().ToLowerInvariant())
            {
                case "gravel":
                    return SurfaceKind.Gravel;
                case "water":
                    return SurfaceKind.Water;
                case "sand":
                    return SurfaceKind.Sand;
                case "snow":
                    return SurfaceKind.Snow;
                case "asphalt":
                    return SurfaceKind.Asphalt;
                default:
                    return SurfaceKind.Other;
            }
        }

        private static void SetSpatial(AudioSourceHandle? sound, Vector3 position, Vector3 velocity)
        {
            if (sound == null)
                return;
            sound.SetPosition(position);
            sound.SetVelocity(velocity);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + ((b - a) * Clamp(t, 0f, 1f));
        }

        private static float NormalizeDegrees(float degrees)
        {
            degrees %= 360f;
            if (degrees < 0f)
                degrees += 360f;
            return degrees;
        }

        private static float SignedDeltaDegrees(float current, float target)
        {
            var delta = NormalizeDegrees(target - current);
            if (delta > 180f)
                delta -= 360f;
            return delta;
        }

        private AudioSourceHandle CreateRequiredSound(string? path, string label, bool looped = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException($"Sound path not provided for {label}.");
            var resolved = path!.Trim();
            if (!File.Exists(resolved))
                throw new FileNotFoundException("Sound file not found.", resolved);
            return looped
                ? _audio.CreateLoopingSource(resolved, useHrtf: true)
                : _audio.CreateSource(resolved, streamFromDisk: true, useHrtf: true);
        }

        private AudioSourceHandle? TryCreateSound(string? path, bool looped = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            var resolved = path!.Trim();
            if (!File.Exists(resolved))
                return null;
            return looped
                ? _audio.CreateLoopingSource(resolved, useHrtf: true)
                : _audio.CreateSource(resolved, streamFromDisk: true, useHrtf: true);
        }

        private sealed class BotEvent
        {
            public float Time { get; set; }
            public BotEventType Type { get; set; }
        }

        private enum BotEventType
        {
            CarStart,
            CarComputerStart,
            CarRestart,
            InGear,
            StopHorn,
            StartHorn
        }

        internal enum ComputerState
        {
            Stopped,
            Starting,
            Running,
            Crashing,
            Stopping
        }
    }
}

