using System;
using System.Collections.Generic;
using System.Linq;
using TopSpeed.Common;
using TopSpeed.Audio;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Race.Events;
using TopSpeed.Race.TimeTrial;
using TopSpeed.Runtime;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Race
{
    internal sealed class TimeTrialMode : RaceMode
    {
        private readonly Store _scores;
        private readonly string _trackId;
        private readonly List<int> _lapTimes;
        private bool _pauseKeyReleased = true;
        private int _lastLapRaceTimeMs;

        public TimeTrialMode(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            RaceInput input,
            string track,
            string trackId,
            bool automaticTransmission,
            int nrOfLaps,
            int vehicle,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice,
            IFileDialogs fileDialogs)
            : base(audio, speech, settings, input, track, automaticTransmission, nrOfLaps, vehicle, vehicleFile, vibrationDevice, fileDialogs)
        {
            _scores = Store.CreateDefault();
            _trackId = trackId ?? throw new ArgumentNullException(nameof(trackId));
            _lapTimes = new List<int>();
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
            var previous = _scores.Read(_trackId, _nrOfLaps);
            var beatRecord = previous.RunBestMs <= 0 || _raceTime < previous.RunBestMs;
            var currentBestLap = _lapTimes.Count == 0 ? 0 : _lapTimes.Min();
            var snapshot = _scores.RecordRun(_trackId, _track.TrackName, _nrOfLaps, _raceTime, _lapTimes.ToArray());

            SetResultSummary(new RaceResultSummary
            {
                Mode = RaceResultMode.TimeTrial,
                IsMultiplayer = false,
                LocalPosition = 1,
                TimeTrialBeatRecord = beatRecord,
                TimeTrialLapCount = _nrOfLaps,
                TimeTrialCurrentRunMs = _raceTime,
                TimeTrialBestRunMs = snapshot.RunBestMs,
                TimeTrialAverageRunMs = snapshot.RunAverageMs,
                TimeTrialBestLapThisRunMs = currentBestLap,
                TimeTrialBestLapMs = snapshot.LapBestMs,
                TimeTrialAverageLapMs = snapshot.LapAverageMs,
                Entries = Array.Empty<RaceResultEntry>()
            });
            PushEvent(RaceEventType.RaceTimeFinalize, 0f);
        }

        protected override void OnRaceStartEvent()
        {
            base.OnRaceStartEvent();
            _lapTimes.Clear();
            _lastLapRaceTimeMs = 0;
        }

        protected override void OnPlayerLapCompleted(int lapNumber, int raceTimeMs)
        {
            if (lapNumber < 1 || lapNumber > _nrOfLaps)
                return;

            var lapTimeMs = raceTimeMs - _lastLapRaceTimeMs;
            if (lapTimeMs > 0)
                _lapTimes.Add(lapTimeMs);
            _lastLapRaceTimeMs = raceTimeMs;
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




