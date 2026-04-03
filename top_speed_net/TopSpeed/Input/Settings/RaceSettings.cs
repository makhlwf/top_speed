using Key = TopSpeed.Input.InputKey;
using System;
using System.Collections.Generic;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed class RaceSettings
    {
        public RaceSettings()
        {
            RestoreDefaults();
        }

        public string Language { get; set; } = "en";
        public AxisOrButton ControllerLeft { get; set; }
        public AxisOrButton ControllerRight { get; set; }
        public AxisOrButton ControllerThrottle { get; set; }
        public AxisOrButton ControllerBrake { get; set; }
        public AxisOrButton ControllerClutch { get; set; }
        public AxisOrButton ControllerGearUp { get; set; }
        public AxisOrButton ControllerGearDown { get; set; }
        public AxisOrButton ControllerHorn { get; set; }
        public AxisOrButton ControllerRequestInfo { get; set; }
        public AxisOrButton ControllerCurrentGear { get; set; }
        public AxisOrButton ControllerCurrentLapNr { get; set; }
        public AxisOrButton ControllerCurrentRacePerc { get; set; }
        public AxisOrButton ControllerCurrentLapPerc { get; set; }
        public AxisOrButton ControllerCurrentRaceTime { get; set; }
        public AxisOrButton ControllerStartEngine { get; set; }
        public AxisOrButton ControllerReportDistance { get; set; }
        public AxisOrButton ControllerReportSpeed { get; set; }
        public AxisOrButton ControllerTrackName { get; set; }
        public AxisOrButton ControllerPause { get; set; }
        public PedalInvertMode ControllerThrottleInvertMode { get; set; }
        public PedalInvertMode ControllerBrakeInvertMode { get; set; }
        public int ControllerSteeringDeadZone { get; set; }
        public State ControllerCenter { get; set; }

        public Key KeyLeft { get; set; }
        public Key KeyRight { get; set; }
        public Key KeyThrottle { get; set; }
        public Key KeyBrake { get; set; }
        public Key KeyClutch { get; set; }
        public Key KeyGearUp { get; set; }
        public Key KeyGearDown { get; set; }
        public Key KeyHorn { get; set; }
        public Key KeyRequestInfo { get; set; }
        public Key KeyCurrentGear { get; set; }
        public Key KeyCurrentLapNr { get; set; }
        public Key KeyCurrentRacePerc { get; set; }
        public Key KeyCurrentLapPerc { get; set; }
        public Key KeyCurrentRaceTime { get; set; }
        public Key KeyStartEngine { get; set; }
        public Key KeyReportDistance { get; set; }
        public Key KeyReportSpeed { get; set; }
        public Key KeyTrackName { get; set; }
        public Key KeyPause { get; set; }

        public bool ForceFeedback { get; set; }
        public KeyboardProgressiveRate KeyboardProgressiveRate { get; set; }
        public InputDeviceMode DeviceMode { get; set; }

        public AutomaticInfoMode AutomaticInfo { get; set; }
        public CopilotMode Copilot { get; set; }
        public CurveAnnouncementMode CurveAnnouncement { get; set; }
        public int NrOfLaps { get; set; }
        public int NrOfComputers { get; set; }
        public RaceDifficulty Difficulty { get; set; }
        public UnitSystem Units { get; set; }
        public float MusicVolume { get; set; }
        public AudioVolumeSettings AudioVolumes { get; set; } = new AudioVolumeSettings();
        public bool HrtfAudio { get; set; }
        public bool StereoWidening { get; set; }
        public bool AutoDetectAudioDeviceFormat { get; set; }
        public bool RandomCustomTracks { get; set; }
        public bool RandomCustomVehicles { get; set; }
        public bool SingleRaceCustomVehicles { get; set; }
        public string LastServerAddress { get; set; } = string.Empty;
        public int DefaultServerPort { get; set; }
        public float ScreenReaderRateMs { get; set; }
        public bool UsageHints { get; set; }
        public bool MenuAutoFocus { get; set; }
        public bool MenuWrapNavigation { get; set; }
        public string MenuSoundPreset { get; set; } = "1";
        public bool MenuNavigatePanning { get; set; }
        public bool AutoCheckUpdates { get; set; }
        public string RadioLastFolder { get; set; } = string.Empty;
        public bool RadioShuffle { get; set; }
        public Dictionary<string, Key> ShortcutKeyBindings { get; set; } = new Dictionary<string, Key>(StringComparer.Ordinal);
        public List<SavedServerEntry> SavedServers { get; set; } = new List<SavedServerEntry>();

        public bool UseController
        {
            get => DeviceMode != InputDeviceMode.Keyboard;
            set => DeviceMode = value ? InputDeviceMode.Controller : InputDeviceMode.Keyboard;
        }

        public void RestoreDefaults()
        {
            Language = "en";
            ControllerLeft = AxisOrButton.AxisXNeg;
            ControllerRight = AxisOrButton.AxisXPos;
            ControllerThrottle = AxisOrButton.AxisRzPos;
            ControllerBrake = AxisOrButton.AxisZPos;
            ControllerClutch = AxisOrButton.AxisSlider1Pos;
            ControllerGearUp = AxisOrButton.Button2;
            ControllerGearDown = AxisOrButton.Button1;
            ControllerHorn = AxisOrButton.Button3;
            ControllerRequestInfo = AxisOrButton.Button4;
            ControllerCurrentGear = AxisOrButton.Button5;
            ControllerCurrentLapNr = AxisOrButton.Button6;
            ControllerCurrentRacePerc = AxisOrButton.Button7;
            ControllerCurrentLapPerc = AxisOrButton.Button8;
            ControllerCurrentRaceTime = AxisOrButton.Button9;
            ControllerStartEngine = AxisOrButton.Button10;
            ControllerReportDistance = AxisOrButton.Button11;
            ControllerReportSpeed = AxisOrButton.Button12;
            ControllerTrackName = AxisOrButton.Button13;
            ControllerPause = AxisOrButton.Button14;
            ControllerThrottleInvertMode = PedalInvertMode.Auto;
            ControllerBrakeInvertMode = PedalInvertMode.Auto;
            ControllerSteeringDeadZone = 1;
            ControllerCenter = default;

            KeyLeft = Key.Left;
            KeyRight = Key.Right;
            KeyThrottle = Key.Up;
            KeyBrake = Key.Down;
            KeyClutch = Key.LeftShift;
            KeyGearUp = Key.A;
            KeyGearDown = Key.Z;
            KeyHorn = Key.Space;
            KeyRequestInfo = Key.Tab;
            KeyCurrentGear = Key.Q;
            KeyCurrentLapNr = Key.W;
            KeyCurrentRacePerc = Key.E;
            KeyCurrentLapPerc = Key.R;
            KeyCurrentRaceTime = Key.T;
            KeyStartEngine = Key.Return;
            KeyReportDistance = Key.C;
            KeyReportSpeed = Key.S;
            KeyTrackName = Key.F9;
            KeyPause = Key.P;

            ForceFeedback = false;
            KeyboardProgressiveRate = KeyboardProgressiveRate.Off;
            DeviceMode = InputDeviceMode.Keyboard;
            AutomaticInfo = AutomaticInfoMode.On;
            Copilot = CopilotMode.All;
            CurveAnnouncement = CurveAnnouncementMode.SpeedDependent;
            NrOfLaps = 3;
            NrOfComputers = 3;
            Difficulty = RaceDifficulty.Easy;
            Units = UnitSystem.Metric;
            MusicVolume = 0.6f;
            AudioVolumes = new AudioVolumeSettings();
            AudioVolumes.RestoreDefaults((int)Math.Round(MusicVolume * 100f));
            HrtfAudio = true;
            StereoWidening = false;
            AutoDetectAudioDeviceFormat = true;
            RandomCustomTracks = false;
            RandomCustomVehicles = false;
            SingleRaceCustomVehicles = false;
            LastServerAddress = string.Empty;
            DefaultServerPort = 28630;
            ScreenReaderRateMs = 0f;
            UsageHints = true;
            MenuAutoFocus = true;
            MenuWrapNavigation = true;
            MenuSoundPreset = "1";
            MenuNavigatePanning = false;
            AutoCheckUpdates = true;
            RadioLastFolder = string.Empty;
            RadioShuffle = false;
            ShortcutKeyBindings = new Dictionary<string, Key>(StringComparer.Ordinal);
            SavedServers = new List<SavedServerEntry>();
        }

        public void SyncMusicVolumeFromAudioCategories()
        {
            AudioVolumes ??= new AudioVolumeSettings();
            AudioVolumes.ClampAll();
            MusicVolume = AudioVolumeSettings.PercentToScalar(AudioVolumes.MusicPercent);
        }

        public void SyncAudioCategoriesFromMusicVolume()
        {
            AudioVolumes ??= new AudioVolumeSettings();
            AudioVolumes.MusicPercent = AudioVolumeSettings.ClampPercent((int)Math.Round(Math.Max(0f, Math.Min(1f, MusicVolume)) * 100f));
            AudioVolumes.ClampAll();
        }
    }
}


