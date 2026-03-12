using System;
using TopSpeed.Common;
using TopSpeed.Audio;
using TopSpeed.Input;
using TopSpeed.Race.Events;
using TopSpeed.Race.TimeTrial;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Race
{
    internal sealed class TimeTrialMode : RaceMode
    {
        private readonly ScoreStore _scores;
        private bool _pauseKeyReleased = true;

        public TimeTrialMode(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            RaceInput input,
            string track,
            bool automaticTransmission,
            int nrOfLaps,
            int vehicle,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice)
            : base(audio, speech, settings, input, track, automaticTransmission, nrOfLaps, vehicle, vehicleFile, vibrationDevice)
        {
            _scores = ScoreStore.CreateDefault();
        }

        public void Initialize()
        {
            InitializeMode();
            _soundTheme4 = LoadLanguageSound("music\\theme4", streamFromDisk: false);
            _soundPause = LoadLanguageSound("race\\pause");
            _soundUnpause = LoadLanguageSound("race\\unpause");
            _soundTheme4.SetVolumePercent((int)Math.Round(_settings.MusicVolume * 100f));
        }

        public void FinalizeTimeTrialMode()
        {
            FinalizeMode();
        }

        public void Run(float elapsed)
        {
            BeginFrame();
            RunPlayerVehicleStep(elapsed);
            HandlePlayerLapProgress(() => PushEvent(RaceEventType.RaceFinish, 2.0f));

            HandleCoreRaceMetricsRequests(includeFinishedRaceTime: false);

            if (_input.TryGetPlayerInfo(out var player) && player == 0)
            {
                SpeakText(GetVehicleName());
            }

            HandleGeneralInfoRequests(ref _pauseKeyReleased);

            if (CompleteFrame(elapsed))
                return;
        }

        protected override void OnRaceFinishEvent()
        {
            AppendDefaultRaceFinishAnnouncement();
            _highscore = _scores.Read(_track.TrackName, _nrOfLaps);
            if ((_raceTime < _highscore) || (_highscore == 0))
            {
                _scores.Write(_track.TrackName, _nrOfLaps, _raceTime);
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundNewTime);
                _sayTimeLength += _soundNewTime.GetLengthSeconds();
            }
            else
            {
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundBestTime);
                _sayTimeLength += _soundBestTime.GetLengthSeconds() + 0.5f;
                SayTime(_highscore);
            }

            PushEvent(RaceEventType.RaceTimeFinalize, _sayTimeLength);
        }

        protected override void OnRaceTimeFinalizeEvent()
        {
            base.OnRaceTimeFinalizeEvent();
            RequestExitWhenQueueIdle();
        }

        public void Pause()
        {
            PauseCore();
        }

        public void Unpause()
        {
            UnpauseCore();
        }

        private string GetVehicleName()
        {
            if (_car.UserDefined && !string.IsNullOrWhiteSpace(_car.CustomFile))
                return FormatVehicleName(_car.CustomFile);
            return _car.VehicleName;
        }
    }
}


