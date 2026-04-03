using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Bots;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Physics.Powertrain;
using TopSpeed.Protocol;
using TopSpeed.Tracks;
using TopSpeed.Vehicles.Live;

namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer : IDisposable
    {
        public ComputerPlayer(
            AudioManager audio,
            Track track,
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
            _radio = new VehicleRadioController(audio);
            _liveRadio = new LiveRadio(audio, settings);

            _surface = TrackSurface.Asphalt;
            _gear = 1;
            _state = ComputerState.Stopped;
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
            _speedDiff = 0;
            _lateralVelocityMps = 0f;
            _yawRateRad = 0f;
            _speed = 0;
            _frame = 1;
            _finished = false;
            _random = Algorithm.RandomInt(100);
            _networkBackfireActive = false;
            _remoteEngineStartPending = false;
            _remoteEngineStartRemaining = 0f;
            _remoteEnginePendingFrequency = _idleFreq;
            _crashLateralAnchored = false;
            _crashLateralFromCenter = 0f;
            _radioLoaded = false;
            _radioPlaying = false;
            _radioMediaId = 0;

            var definition = VehicleLoader.LoadOfficial(vehicleIndex, track.Weather);
            _surfaceTractionFactor = definition.SurfaceTractionFactor;
            _deceleration = definition.Deceleration;
            _topSpeed = definition.TopSpeed;
            var torqueCurve = PowertrainProfileBuilder.Build(definition);
            var build = PowertrainBuild.Create(
                new BuildInput(
                    _deceleration,
                    definition.MassKg,
                    definition.DrivetrainEfficiency,
                    definition.EngineBrakingTorqueNm,
                    definition.TireGripCoefficient,
                    definition.BrakeStrength,
                    definition.TireCircumferenceM / (2.0f * (float)Math.PI),
                    definition.EngineBraking,
                    definition.IdleRpm,
                    definition.RevLimiter,
                    definition.FinalDriveRatio,
                    definition.PowerFactor,
                    definition.PeakTorqueNm,
                    definition.PeakTorqueRpm,
                    definition.IdleTorqueNm,
                    definition.RedlineTorqueNm,
                    definition.DragCoefficient,
                    definition.FrontalAreaM2,
                    definition.RollingResistanceCoefficient,
                    definition.LaunchRpm,
                    definition.ReversePowerFactor,
                    definition.ReverseGearRatio,
                    definition.ReverseMaxSpeedKph,
                    definition.EngineInertiaKgm2,
                    definition.EngineFrictionTorqueNm,
                    definition.DrivelineCouplingRate,
                    definition.Gears,
                    torqueCurve,
                    definition.GearRatios,
                    definition.CoastDragBaseMps2,
                    definition.CoastDragLinearPerMps,
                    definition.EngineFrictionLinearNmPerKrpm,
                    definition.EngineFrictionQuadraticNmPerKrpm2,
                    definition.IdleControlWindowRpm,
                    definition.IdleControlGainNmPerRpm,
                    definition.MinCoupledRiseIdleRpmPerSecond,
                    definition.MinCoupledRiseFullRpmPerSecond,
                    definition.EngineOverrunIdleLossFraction,
                    definition.OverrunCurveExponent,
                    definition.EngineBrakeTransferEfficiency));

            _massKg = build.Powertrain.MassKg;
            _drivetrainEfficiency = build.Powertrain.DrivetrainEfficiency;
            _engineBrakingTorqueNm = build.Powertrain.EngineBrakingTorqueNm;
            _tireGripCoefficient = build.Powertrain.TireGripCoefficient;
            _brakeStrength = build.Powertrain.BrakeStrength;
            _wheelRadiusM = build.Powertrain.WheelRadiusM;
            _engineBraking = build.Powertrain.EngineBraking;
            _idleRpm = build.Powertrain.IdleRpm;
            _revLimiter = build.Powertrain.RevLimiter;
            _finalDriveRatio = build.Powertrain.FinalDriveRatio;
            _powerFactor = build.Powertrain.PowerFactor;
            _peakTorqueNm = build.Powertrain.PeakTorqueNm;
            _peakTorqueRpm = build.Powertrain.PeakTorqueRpm;
            _idleTorqueNm = build.Powertrain.IdleTorqueNm;
            _redlineTorqueNm = build.Powertrain.RedlineTorqueNm;
            _dragCoefficient = build.Powertrain.DragCoefficient;
            _frontalAreaM2 = build.Powertrain.FrontalAreaM2;
            _rollingResistanceCoefficient = build.Powertrain.RollingResistanceCoefficient;
            _launchRpm = build.Powertrain.LaunchRpm;
            _engineInertiaKgm2 = build.Powertrain.EngineInertiaKgm2;
            _engineFrictionTorqueNm = build.Powertrain.EngineFrictionTorqueNm;
            _drivelineCouplingRate = build.Powertrain.DrivelineCouplingRate;
            _activeTransmissionType = definition.PrimaryTransmissionType;
            _automaticTuning = definition.AutomaticTuning;
            _automaticCouplingFactor = 1f;
            _cvtRatio = _automaticTuning.Cvt.RatioMax;
            _effectiveDriveRatio = 0f;
            _lateralGripCoefficient = Math.Max(0.1f, definition.LateralGripCoefficient);
            _highSpeedStability = Math.Max(0f, Math.Min(1.0f, definition.HighSpeedStability));
            _wheelbaseM = Math.Max(0.5f, definition.WheelbaseM);
            _maxSteerDeg = Math.Max(5f, Math.Min(60f, definition.MaxSteerDeg));
            _widthM = Math.Max(0.5f, definition.WidthM);
            _lengthM = Math.Max(0.5f, definition.LengthM);
            _idleFreq = definition.IdleFreq;
            _topFreq = definition.TopFreq;
            _shiftFreq = definition.ShiftFreq;
            _pitchCurveExponent = VehicleDefinition.ClampPitchCurveExponent(definition.PitchCurveExponent);
            _gears = build.Powertrain.Gears;
            _steering = definition.Steering;
            _frequency = _idleFreq;

            _engine = new EngineModel(
                _idleRpm,
                definition.MaxRpm,
                _revLimiter,
                definition.AutoShiftRpm,
                _engineBraking,
                _topSpeed,
                _finalDriveRatio,
                definition.TireCircumferenceM,
                _gears,
                build.GearRatios,
                _peakTorqueNm,
                _peakTorqueRpm,
                _idleTorqueNm,
                _redlineTorqueNm,
                _engineBrakingTorqueNm,
                _powerFactor,
                _engineInertiaKgm2,
                _engineFrictionTorqueNm,
                _drivelineCouplingRate,
                torqueCurve,
                engineOverrunIdleLossFraction: build.EngineOverrunIdleLossFraction,
                engineFrictionLinearNmPerKrpm: build.FrictionLinearNmPerKrpm,
                engineFrictionQuadraticNmPerKrpm2: build.FrictionQuadraticNmPerKrpm2,
                idleControlWindowRpm: build.IdleControlWindowRpm,
                idleControlGainNmPerRpm: build.IdleControlGainNmPerRpm,
                minCoupledRiseIdleRpmPerSecond: build.MinCoupledRiseIdleRpmPerSecond,
                minCoupledRiseFullRpmPerSecond: build.MinCoupledRiseFullRpmPerSecond,
                overrunCurveExponent: build.OverrunCurveExponent);

            _physicsConfig = new BotPhysicsConfig(
                _surfaceTractionFactor,
                _deceleration,
                _topSpeed,
                _massKg,
                _drivetrainEfficiency,
                _engineBrakingTorqueNm,
                _tireGripCoefficient,
                _brakeStrength,
                _wheelRadiusM,
                _engineBraking,
                _idleRpm,
                _revLimiter,
                _finalDriveRatio,
                _powerFactor,
                _peakTorqueNm,
                _peakTorqueRpm,
                _idleTorqueNm,
                _redlineTorqueNm,
                _dragCoefficient,
                _frontalAreaM2,
                _rollingResistanceCoefficient,
                _launchRpm,
                definition.ReversePowerFactor,
                definition.ReverseGearRatio,
                _engineInertiaKgm2,
                _engineFrictionTorqueNm,
                _drivelineCouplingRate,
                _lateralGripCoefficient,
                _highSpeedStability,
                _wheelbaseM,
                _widthM,
                _lengthM,
                _maxSteerDeg,
                _steering,
                definition.HighSpeedSteerGain,
                definition.HighSpeedSteerStartKph,
                definition.HighSpeedSteerFullKph,
                definition.CombinedGripPenalty,
                definition.SlipAnglePeakDeg,
                definition.SlipAngleFalloff,
                definition.TurnResponse,
                definition.MassSensitivity,
                definition.DownforceGripGain,
                definition.CornerStiffnessFront,
                definition.CornerStiffnessRear,
                definition.YawInertiaScale,
                definition.SteeringCurve,
                definition.TransientDamping,
                _gears,
                torqueCurve,
                build.GearRatios,
                definition.TransmissionPolicy,
                definition.PrimaryTransmissionType,
                definition.AutomaticTuning,
                coastDragBaseMps2: build.CoastDragBaseMps2,
                coastDragLinearPerMps: build.CoastDragLinearPerMps,
                frictionLinearNmPerKrpm: build.FrictionLinearNmPerKrpm,
                frictionQuadraticNmPerKrpm2: build.FrictionQuadraticNmPerKrpm2,
                idleControlWindowRpm: build.IdleControlWindowRpm,
                idleControlGainNmPerRpm: build.IdleControlGainNmPerRpm,
                minCoupledRiseIdleRpmPerSecond: build.MinCoupledRiseIdleRpmPerSecond,
                minCoupledRiseFullRpmPerSecond: build.MinCoupledRiseFullRpmPerSecond,
                engineOverrunIdleLossFraction: build.EngineOverrunIdleLossFraction,
                overrunCurveExponent: build.OverrunCurveExponent,
                engineBrakeTransferEfficiency: build.EngineBrakeTransferEfficiency);

            _soundEngine = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Engine), "engine", looped: true);
            _soundStart = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Start), "start");
            _soundHorn = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Horn), "horn", looped: true);
            _soundCrash = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Crash), "crash");
            _soundBrake = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Brake), "brake", looped: true, allowHrtf: false);
            _soundEngine.SetDopplerFactor(1f);
            _soundHorn.SetDopplerFactor(0f);
            _soundBrake.SetDopplerFactor(0f);
            _soundMiniCrash = CreateRequiredSound(Path.Combine(_legacyRoot, "crashshort.wav"), "mini crash");
            _soundBump = CreateRequiredSound(Path.Combine(_legacyRoot, "bump.wav"), "bump", allowHrtf: false);
            _soundCrash.SetDopplerFactor(0f);
            _soundMiniCrash.SetDopplerFactor(0f);
            _soundBump.SetDopplerFactor(0f);
            _soundBackfire = TryCreateSound(definition.GetSoundPath(VehicleAction.Backfire));
            RefreshCategoryVolumes(force: true);
        }
    }
}

