using System;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private void UpdateEngineFreq()
        {
            UpdateEngineFreqManual();
        }

        private void UpdateEngineFreqManual()
        {
            var driveGear = GetDriveGear();
            var gearRange = _engine.GetGearRangeKmh(driveGear);
            var gearMin = _engine.GetGearMinSpeedKmh(driveGear);

            if (driveGear == FirstForwardGear)
            {
                if (_speed < (4.0f / 3.0f) * gearRange)
                {
                    _frequency = _idleFreq + (int)((_speed * 3.0f / (2.0f * gearRange)) * (_topFreq - _idleFreq));
                }
                else
                {
                    _frequency = _idleFreq + 2 * (_topFreq - _idleFreq);
                }
            }
            else
            {
                var shiftPoint = gearMin + ((2.0f / 3.0f) * gearRange);
                if (shiftPoint > 0f)
                    _frequency = (int)((_speed / shiftPoint) * _topFreq);
                else
                    _frequency = _idleFreq;

                if (_frequency > 2 * _topFreq)
                    _frequency = 2 * _topFreq;
                if (_frequency < _idleFreq / 2)
                    _frequency = _idleFreq / 2;
            }

            if (_switchingGear != 0)
                _frequency = (2 * _prevFrequency + _frequency) / 3;

            if (_frequency == _prevFrequency)
                return;

            _soundEngine.SetFrequency(_frequency);
            if (_soundThrottle != null)
            {
                if ((int)_throttleVolume != (int)_prevThrottleVolume)
                {
                    SetPlayerEngineVolumePercent(_soundThrottle, (int)_throttleVolume);
                    _prevThrottleVolume = _throttleVolume;
                }
                _soundThrottle.SetFrequency(_frequency);
            }
            _prevFrequency = _frequency;
        }

        private void UpdateEngineFreqForGear(int gear)
        {
            var clampedGear = gear;
            if (clampedGear > _gears)
                clampedGear = _gears;
            if (clampedGear < 1)
                clampedGear = 1;

            var gearRange = _engine.GetGearRangeKmh(clampedGear);
            var gearMin = _engine.GetGearMinSpeedKmh(clampedGear);

            if (clampedGear == 1)
            {
                var gearSpeed = gearRange <= 0f ? 0f : Math.Min(1.0f, _speed / gearRange);
                _frequency = (int)(gearSpeed * (_topFreq - _idleFreq)) + _idleFreq;
            }
            else
            {
                var gearSpeed = (_speed - gearMin) / (float)gearRange;
                if (gearSpeed <= 0f)
                {
                    _frequency = _idleFreq;
                    if (_soundBackfireVariants.Length > 0 && _backfirePlayedAuto)
                        _backfirePlayedAuto = false;
                }
                else
                {
                    if (gearSpeed > 1.0f)
                        gearSpeed = 1.0f;
                    if (gearSpeed < 0.07f)
                    {
                        _frequency = (int)(((0.07f - gearSpeed) / 0.07f) * (_topFreq - _shiftFreq) + _shiftFreq);
                        if (_soundBackfireVariants.Length > 0)
                        {
                            if (!_backfirePlayedAuto)
                            {
                                if (Algorithm.RandomInt(5) == 1 && !AnyBackfirePlaying())
                                    PlayRandomBackfire();
                            }
                            _backfirePlayedAuto = true;
                        }
                    }
                    else
                    {
                        _frequency = (int)(gearSpeed * (_topFreq - _shiftFreq) + _shiftFreq);
                        if (_soundBackfireVariants.Length > 0 && _backfirePlayedAuto)
                            _backfirePlayedAuto = false;
                    }
                }
            }

            if (_switchingGear != 0)
                _frequency = (2 * _prevFrequency + _frequency) / 3;
            if (_frequency == _prevFrequency)
                return;

            _soundEngine.SetFrequency(_frequency);
            if (_soundThrottle != null)
            {
                if ((int)_throttleVolume != (int)_prevThrottleVolume)
                {
                    SetPlayerEngineVolumePercent(_soundThrottle, (int)_throttleVolume);
                    _prevThrottleVolume = _throttleVolume;
                }
                _soundThrottle.SetFrequency(_frequency);
            }
            _prevFrequency = _frequency;
        }

        private void UpdateSoundRoad()
        {
            _audioFlow.UpdateRoad(
                _surface,
                _speed,
                ref _surfaceFrequency,
                ref _prevSurfaceFrequency,
                _soundAsphalt,
                _soundGravel,
                _soundWater,
                _soundSand,
                _soundSnow);
        }

        private void SwitchSurfaceSound(TrackSurface surface)
        {
            switch (surface)
            {
                case TrackSurface.Gravel:
                    _soundGravel.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    _soundGravel.Play(loop: true);
                    break;
                case TrackSurface.Water:
                    _soundWater.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    _soundWater.Play(loop: true);
                    break;
                case TrackSurface.Sand:
                    _soundSand.SetFrequency((int)(_surfaceFrequency / 2.5f));
                    _soundSand.Play(loop: true);
                    break;
                case TrackSurface.Snow:
                    _soundSnow.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    _soundSnow.Play(loop: true);
                    break;
                case TrackSurface.Asphalt:
                    _soundAsphalt.SetFrequency(Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    _soundAsphalt.Play(loop: true);
                    break;
            }
        }

        private void ApplyPan(int pan)
        {
            _audioFlow.ApplyPan(
                _surface,
                pan,
                _soundHorn,
                _soundBrake,
                _soundBackfire,
                _soundWipers,
                _soundAsphalt,
                _soundGravel,
                _soundWater,
                _soundSand,
                _soundSnow);
        }

        private int CalculatePan(float relPos)
        {
            return _audioFlow.CalculatePan(relPos);
        }

        private void RefreshCategoryVolumes(bool force = false)
        {
            _audioFlow.RefreshVolumes(
                _settings,
                force,
                (int)Math.Round(_throttleVolume),
                _soundEngine,
                _soundStart,
                _soundThrottle,
                _soundHorn,
                _soundBrake,
                _soundMiniCrash,
                _soundBump,
                _soundBadSwitch,
                _soundWipers,
                _soundCrash,
                _soundBackfire,
                _soundCrashVariants,
                _soundBackfireVariants,
                _soundAsphalt,
                _soundGravel,
                _soundWater,
                _soundSand,
                _soundSnow,
                ref _lastPlayerEngineVolumePercent,
                ref _lastPlayerEventsVolumePercent,
                ref _lastSurfaceLoopVolumePercent);
        }

        private void SetPlayerEngineVolumePercent(AudioSourceHandle? sound, int percent)
        {
            sound.SetVolumePercent(_settings, AudioVolumeCategory.PlayerVehicleEngine, percent);
        }

        private void SetPlayerEventVolumePercent(AudioSourceHandle? sound, int percent)
        {
            sound.SetVolumePercent(_settings, AudioVolumeCategory.PlayerVehicleEvents, percent);
        }

        private void SetSurfaceLoopVolumePercent(AudioSourceHandle? sound, int percent)
        {
            sound.SetVolumePercent(_settings, AudioVolumeCategory.SurfaceLoops, percent);
        }
    }
}
