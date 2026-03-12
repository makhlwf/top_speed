using System;
using System.Collections.Generic;
using System.Diagnostics;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Race.Events;
using TopSpeed.Race.Panels;
using TopSpeed.Race.Runtime;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Core;
using TS.Audio;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Race
{
    internal abstract partial class RaceMode
    {
        protected RaceMode(
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
            : this(audio, speech, settings, input, track, automaticTransmission, nrOfLaps, vehicle, vehicleFile, vibrationDevice, null, userDefined: false)
        {
        }

        protected RaceMode(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            RaceInput input,
            string track,
            bool automaticTransmission,
            int nrOfLaps,
            int vehicle,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice,
            TrackData? trackData,
            bool userDefined)
        {
            _audio = audio;
            _speech = speech;
            _settings = settings;
            _input = input;
            _vibrationDevice = vibrationDevice;
            _events = new List<RaceEvent>();
            _stopwatch = new Stopwatch();
            _soundQueue = new SoundQueue();
            _dueEvents = new List<RaceEvent>();

            _manualTransmission = !automaticTransmission;
            _nrOfLaps = nrOfLaps;
            _lap = 0;
            _speakTime = 0.0f;
            _unkeyQueue = 0;
            _highscore = 0;
            _sayTimeLength = 0.0f;

            _track = trackData == null
                ? Track.Load(track, audio)
                : Track.LoadFromData(track, trackData, audio, userDefined);
            _car = CarFactory.CreateDefault(audio, _track, input, settings, vehicle, vehicleFile, () => _elapsedTotal, () => _started, _vibrationDevice);
            _localRadio = new VehicleRadioController(audio);
            _radioPanel = new RadioVehiclePanel(_input, _audio, _settings, _localRadio, NextLocalMediaId, SpeakText, HandleLocalRadioMediaLoaded, HandleLocalRadioPlaybackChanged);
            _panelManager = new VehiclePanelManager(new IVehicleRacePanel[]
            {
                new ControlVehiclePanel(),
                _radioPanel
            });
            ApplyActivePanelInputAccess();
            RefreshCategoryVolumes();

            if (!string.IsNullOrWhiteSpace(track) &&
                track.IndexOf("adv", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _nrOfLaps = 1;
            }

            _soundNumbers = new AudioSourceHandle[101];
            for (var i = 0; i <= 100; i++)
            {
                _soundNumbers[i] = LoadLanguageSound($"numbers\\{i}");
            }

            _soundStart = LoadLanguageSound("race\\start321");
            _soundBestTime = LoadLanguageSound("race\\time\\trackrecord");
            _soundNewTime = LoadLanguageSound("race\\time\\newrecord");
            _soundYourTime = LoadLanguageSound("race\\time\\yourtime");
            _soundMinute = LoadLanguageSound("race\\time\\minute");
            _soundMinutes = LoadLanguageSound("race\\time\\minutes");
            _soundSecond = LoadLanguageSound("race\\time\\second");
            _soundSeconds = LoadLanguageSound("race\\time\\seconds");
            _soundPoint = LoadLanguageSound("race\\time\\point");
            _soundPercent = LoadLanguageSound("race\\time\\percent");

            _soundUnkey = new AudioSourceHandle[MaxUnkeys];
            for (var i = 0; i < MaxUnkeys; i++)
            {
                var file = $"unkey{i + 1}.wav";
                _soundUnkey[i] = LoadLegacySound(file);
            }

            _randomSounds = new AudioSourceHandle?[RandomSoundGroups][];
            _totalRandomSounds = new int[RandomSoundGroups];
            for (var i = 0; i < RandomSoundGroups; i++)
                _randomSounds[i] = new AudioSourceHandle?[RandomSoundMax];

            LoadRandomSounds(RandomSound.EasyLeft, "race\\copilot\\easyleft");
            LoadRandomSounds(RandomSound.Left, "race\\copilot\\left");
            LoadRandomSounds(RandomSound.HardLeft, "race\\copilot\\hardleft");
            LoadRandomSounds(RandomSound.HairpinLeft, "race\\copilot\\hairpinleft");
            LoadRandomSounds(RandomSound.EasyRight, "race\\copilot\\easyright");
            LoadRandomSounds(RandomSound.Right, "race\\copilot\\right");
            LoadRandomSounds(RandomSound.HardRight, "race\\copilot\\hardright");
            LoadRandomSounds(RandomSound.HairpinRight, "race\\copilot\\hairpinright");
            LoadRandomSounds(RandomSound.Asphalt, "race\\copilot\\asphalt");
            LoadRandomSounds(RandomSound.Gravel, "race\\copilot\\gravel");
            LoadRandomSounds(RandomSound.Water, "race\\copilot\\water");
            LoadRandomSounds(RandomSound.Sand, "race\\copilot\\sand");
            LoadRandomSounds(RandomSound.Snow, "race\\copilot\\snow");
            LoadRandomSounds(RandomSound.Finish, "race\\info\\finish");

            _soundLaps = new AudioSourceHandle[MaxLaps - 1];
            for (var i = 0; i < MaxLaps - 1; i++)
            {
                _soundLaps[i] = LoadLanguageSound($"race\\info\\laps2go{i + 1}");
            }

            _soundTrackName = LoadTrackNameSound(_track.TrackName);
            _soundTurnEndDing = LoadLegacySound("ding.ogg");
        }
    }
}

