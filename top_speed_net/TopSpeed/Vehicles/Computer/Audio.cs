using System;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Input;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
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

        private void RefreshCategoryVolumes(bool force = false)
        {
            var enginePercent = _settings.AudioVolumes?.OtherVehicleEnginePercent ?? 80;
            var eventsPercent = _settings.AudioVolumes?.OtherVehicleEventsPercent ?? 100;
            var radioPercent = _settings.AudioVolumes?.RadioPercent ?? 100;
            if (!force &&
                enginePercent == _lastOtherEngineVolumePercent &&
                eventsPercent == _lastOtherEventsVolumePercent &&
                radioPercent == _lastRadioVolumePercent)
                return;

            _lastOtherEngineVolumePercent = enginePercent;
            _lastOtherEventsVolumePercent = eventsPercent;
            _lastRadioVolumePercent = radioPercent;

            SetOtherEngineVolumePercent(_soundEngine, 80);
            SetOtherEngineVolumePercent(_soundStart, 100);
            SetOtherEventVolumePercent(_soundHorn, 100);
            SetOtherEventVolumePercent(_soundCrash, 100);
            SetOtherEventVolumePercent(_soundBrake, 100);
            SetOtherEventVolumePercent(_soundMiniCrash, 100);
            SetOtherEventVolumePercent(_soundBump, 100);
            SetOtherEventVolumePercent(_soundBackfire, 100);
            _radio.SetVolumePercent(radioPercent);
            _liveRadio.SetVolumePercent(radioPercent);
        }

        private void SetOtherEngineVolumePercent(AudioSourceHandle? sound, int percent)
        {
            sound.SetVolumePercent(_settings, AudioVolumeCategory.OtherVehicleEngine, percent);
        }

        private void SetOtherEventVolumePercent(AudioSourceHandle? sound, int percent)
        {
            sound.SetVolumePercent(_settings, AudioVolumeCategory.OtherVehicleEvents, percent);
        }

        private AudioSourceHandle CreateRequiredSound(string? path, string label, bool looped = false, bool allowHrtf = true)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException($"Sound path not provided for {label}.");
            var resolved = path!.Trim();
            if (!File.Exists(resolved))
                throw new FileNotFoundException("Sound file not found.", resolved);
            return looped
                ? _audio.CreateLoopingSpatialSource(resolved, allowHrtf: allowHrtf)
                : _audio.CreateSpatialSource(resolved, streamFromDisk: true, allowHrtf: allowHrtf);
        }

        private AudioSourceHandle? TryCreateSound(string? path, bool looped = false, bool allowHrtf = true)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            var resolved = path!.Trim();
            if (!File.Exists(resolved))
                return null;
            return looped
                ? _audio.CreateLoopingSpatialSource(resolved, allowHrtf: allowHrtf)
                : _audio.CreateSpatialSource(resolved, streamFromDisk: true, allowHrtf: allowHrtf);
        }

        private float NormalizeSpeedByTopSpeed(float speedKph, float maxRatio = 1f)
        {
            var referenceTopSpeed = Math.Max(1f, _topSpeed);
            var ratio = speedKph / referenceTopSpeed;
            if (ratio <= 0f)
                return 0f;
            if (ratio >= maxRatio)
                return maxRatio;
            return ratio;
        }
    }
}
