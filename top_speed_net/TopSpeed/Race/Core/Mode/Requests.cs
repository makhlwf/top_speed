using System;
using TopSpeed.Common;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Race.Events;
using TopSpeed.Vehicles;

namespace TopSpeed.Race
{
    internal abstract partial class RaceMode
    {
        protected void HandleEngineStartRequest()
        {
            if (_input.GetStartEngine() && _started && !_finished)
            {
                var restartFromStall = !_car.EngineRunning && _car.State == CarState.Running;
                var canStart = !_engineStarted || _car.State == CarState.Crashed || restartFromStall;
                if (canStart)
                {
                    _engineStarted = true;
                    if (_car.State == CarState.Crashed)
                        _car.RestartAfterCrash();
                    else if (restartFromStall)
                        _car.RestartFromStall();
                    else
                        _car.Start();
                }
            }
        }

        protected void HandleCurrentGearRequest()
        {
            if (_input.GetCurrentGear() && _started && _lap <= _nrOfLaps)
            {
                var gear = _car.Gear;
                string code;
                if (_car.InReverseGear)
                {
                    code = "R";
                }
                else if (gear == 0)
                {
                    code = "N";
                }
                else if (_car.ManualTransmission)
                {
                    code = gear.ToString();
                }
                else
                {
                    code = "D";
                }

                SpeakText(LocalizationService.Format(LocalizationService.Mark("Gear {0}"), code));
            }
        }

        protected void HandleCurrentLapNumberRequest()
        {
            if (_input.GetCurrentLapNr() && _started && _lap <= _nrOfLaps)
            {
                SpeakText(LocalizationService.Format(LocalizationService.Mark("Lap {0}"), _lap));
            }
        }

        protected void HandleCurrentRacePercentageRequest()
        {
            if (_input.GetCurrentRacePerc() && _started && _lap <= _nrOfLaps)
            {
                var perc = (_car.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f;
                var units = Math.Max(0, Math.Min(100, (int)perc));
                SpeakText(FormatRacePercentageText(units));
            }
        }

        protected void HandleCurrentLapPercentageRequest()
        {
            if (_input.GetCurrentLapPerc() && _started && _lap <= _nrOfLaps)
            {
                var perc = ((_car.PositionY - (_track.Length * (_lap - 1))) / _track.Length) * 100.0f;
                var units = Math.Max(0, Math.Min(100, (int)perc));
                SpeakText(FormatLapPercentageText(units));
            }
        }

        protected void HandleCurrentRaceTimeRequestActiveOnly()
        {
            if (_input.GetCurrentRaceTime() && _started && _lap <= _nrOfLaps)
            {
                var text = FormatTimeText((int)(_stopwatch.ElapsedMilliseconds - _stopwatchDiffMs), detailed: false);
                SpeakText(LocalizationService.Format(LocalizationService.Mark("Race time {0}"), text));
            }
        }

        protected void HandleCurrentRaceTimeRequestWithFinish()
        {
            if (_input.GetCurrentRaceTime() && _started)
            {
                var timeMs = _lap <= _nrOfLaps
                    ? (int)(_stopwatch.ElapsedMilliseconds - _stopwatchDiffMs)
                    : _raceTime;
                var text = FormatTimeText(timeMs, detailed: false);
                SpeakText(LocalizationService.Format(LocalizationService.Mark("Race time {0}"), text));
            }
        }

        protected void HandleTrackNameRequest()
        {
            if (_input.GetTrackName())
            {
                SpeakText(FormatTrackName(_track.TrackName));
            }
        }

        protected void HandleSpeedReportRequest()
        {
            if (_input.GetSpeedReport() && _started && _lap <= _nrOfLaps)
            {
                var speedKmh = _car.SpeedKmh;
                var rpm = _car.EngineRpm;
                var horsepower = _car.EngineNetHorsepower;
                if (_settings.Units == UnitSystem.Imperial)
                {
                    var speedMph = speedKmh * KmToMiles;
                    SpeakText(LocalizationService.Format(
                        LocalizationService.Mark("{0:F0} miles per hour, {1:F0} RPM, {2:F0} horsepower"),
                        speedMph,
                        rpm,
                        horsepower));
                }
                else
                {
                    SpeakText(LocalizationService.Format(
                        LocalizationService.Mark("{0:F0} kilometers per hour, {1:F0} RPM, {2:F0} horsepower"),
                        speedKmh,
                        rpm,
                        horsepower));
                }
            }
        }

        protected void HandleDistanceReportRequest()
        {
            if (_input.GetDistanceReport() && _started && _lap <= _nrOfLaps)
            {
                var distanceM = _car.DistanceMeters;
                if (_settings.Units == UnitSystem.Imperial)
                {
                    var distanceMiles = distanceM / MetersPerMile;
                    if (distanceMiles >= 1f)
                        SpeakText(LocalizationService.Format(LocalizationService.Mark("{0:F1} miles traveled"), distanceMiles));
                    else
                        SpeakText(LocalizationService.Format(LocalizationService.Mark("{0:F0} feet traveled"), distanceM * MetersToFeet));
                }
                else
                {
                    var distanceKm = distanceM / 1000f;
                    if (distanceKm >= 1f)
                        SpeakText(LocalizationService.Format(LocalizationService.Mark("{0:F1} kilometers traveled"), distanceKm));
                    else
                        SpeakText(LocalizationService.Format(LocalizationService.Mark("{0:F0} meters traveled"), distanceM));
                }
            }
        }

        protected void HandlePauseRequest(ref bool pauseKeyReleased)
        {
            if (!_input.GetPause() && !pauseKeyReleased)
            {
                pauseKeyReleased = true;
            }
            else if (_input.GetPause() && pauseKeyReleased && _started && _lap <= _nrOfLaps && _car.State == CarState.Running)
            {
                pauseKeyReleased = false;
                PauseRequested = true;
            }
        }
    }
}

