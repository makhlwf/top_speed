using System;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Vibration;
using TopSpeed.Network;
using TopSpeed.Race;
using TopSpeed.Runtime;
using TopSpeed.Speech;
using TopSpeed.Tracks;

namespace TopSpeed.Game
{
    internal interface IRaceModeFactory
    {
        TimeTrialMode CreateTimeTrial(
            string track,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice);

        SingleRaceMode CreateSingleRace(
            string track,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice);

        MultiplayerMode CreateMultiplayer(
            TrackData trackData,
            string trackName,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice,
            MultiplayerSession session,
            uint raceInstanceId,
            uint playerId,
            byte playerNumber,
            Func<byte, string> resolvePlayerName);
    }

    internal sealed class RaceModeFactory : IRaceModeFactory
    {
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly RaceSettings _settings;
        private readonly RaceInput _raceInput;
        private readonly IFileDialogs _fileDialogs;

        public RaceModeFactory(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            RaceInput raceInput,
            IFileDialogs fileDialogs)
        {
            _audio = audio;
            _speech = speech;
            _settings = settings;
            _raceInput = raceInput;
            _fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
        }

        public TimeTrialMode CreateTimeTrial(
            string track,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice)
        {
            return new TimeTrialMode(
                _audio,
                _speech,
                _settings,
                _raceInput,
                track,
                automaticTransmission,
                laps,
                vehicleIndex,
                vehicleFile,
                vibrationDevice,
                _fileDialogs);
        }

        public SingleRaceMode CreateSingleRace(
            string track,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice)
        {
            return new SingleRaceMode(
                _audio,
                _speech,
                _settings,
                _raceInput,
                track,
                automaticTransmission,
                laps,
                vehicleIndex,
                vehicleFile,
                vibrationDevice,
                _fileDialogs);
        }

        public MultiplayerMode CreateMultiplayer(
            TrackData trackData,
            string trackName,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice,
            MultiplayerSession session,
            uint raceInstanceId,
            uint playerId,
            byte playerNumber,
            Func<byte, string> resolvePlayerName)
        {
            return new MultiplayerMode(
                _audio,
                _speech,
                _settings,
                _raceInput,
                trackData,
                trackName,
                automaticTransmission,
                laps,
                vehicleIndex,
                vehicleFile,
                vibrationDevice,
                _fileDialogs,
                session,
                raceInstanceId,
                playerId,
                playerNumber,
                resolvePlayerName);
        }
    }
}


