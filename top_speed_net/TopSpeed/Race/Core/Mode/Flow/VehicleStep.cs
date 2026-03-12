using System;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Tracks;

namespace TopSpeed.Race
{
    internal abstract partial class RaceMode
    {
        protected void RunPlayerVehicleStep(float elapsed, Action? afterTrackUpdate = null)
        {
            var previousGear = _car.Gear;
            UpdateVehiclePanels(elapsed);
            _car.Run(elapsed);
            TryAnnounceManualGearShift(previousGear);
            _track.Run(_car.PositionY);
            afterTrackUpdate?.Invoke();
            var road = _track.RoadAtPosition(_car.PositionY);
            HandleTurnEndCue(road);
            _car.Evaluate(road);
            UpdateAudioListener(elapsed);
            if (_track.NextRoad(_car.PositionY, _car.Speed, (int)_settings.CurveAnnouncement, out var nextRoad))
                CallNextRoad(nextRoad);
        }

        private void TryAnnounceManualGearShift(int previousGear)
        {
            if (!_manualTransmission || !_started || _finished)
                return;

            var currentGear = _car.Gear;
            if (currentGear == previousGear)
                return;

            if (!_input.GetGearUp() && !_input.GetGearDown())
                return;

            SpeakText(currentGear <= 0 ? "Reverse" : currentGear.ToString());
        }

        private void HandleTurnEndCue(Track.Road road)
        {
            var currentType = road.Type;
            if (_hasLastRoadTypeAtPosition &&
                _lastRoadTypeAtPosition != TrackType.Straight &&
                currentType == TrackType.Straight &&
                _soundTurnEndDing != null)
            {
                _soundTurnEndDing.Stop();
                _soundTurnEndDing.SeekToStart();
                _soundTurnEndDing.Play(loop: false);
            }

            _lastRoadTypeAtPosition = currentType;
            _hasLastRoadTypeAtPosition = true;
        }

        protected bool HandlePlayerLapProgress(Action onPlayerFinished, bool announceLapsToGo = true)
        {
            var currentLap = _track.Lap(_car.PositionY);
            if (currentLap <= _lap)
                return false;

            _lap = currentLap;
            if (_lap > _nrOfLaps)
            {
                ApplyPlayerFinishState();
                onPlayerFinished?.Invoke();
                return true;
            }

            if (announceLapsToGo &&
                _settings.AutomaticInfo != AutomaticInfoMode.Off &&
                _lap > 1 &&
                _lap <= _nrOfLaps)
            {
                Speak(_soundLaps[_nrOfLaps - _lap], true);
            }

            return false;
        }
    }
}

