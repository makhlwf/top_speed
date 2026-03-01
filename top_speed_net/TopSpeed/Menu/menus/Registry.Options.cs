using System;
using System.Collections.Generic;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Game settings", MenuAction.None, nextMenuId: "options_game"),
                new MenuItem("Volume settings", MenuAction.None, nextMenuId: "options_volume",
                    onActivate: () =>
                    {
                        _settings.SyncAudioCategoriesFromMusicVolume();
                        _audio.ApplyAudioSettings();
                    }),
                new MenuItem("Controls", MenuAction.None, nextMenuId: "options_controls"),
                new MenuItem("Race settings", MenuAction.None, nextMenuId: "options_race"),
                new MenuItem("Server settings", MenuAction.None, nextMenuId: "options_server"),
                new MenuItem("Restore default settings", MenuAction.None, nextMenuId: "options_restore"),
                BackItem()
            };
            return _menu.CreateMenu("options_main", items);
        }

        private MenuScreen BuildOptionsGameSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new CheckBox(
                    "Include custom tracks in randomization",
                    () => _settings.RandomCustomTracks,
                    value => _settingsActions.UpdateSetting(() => _settings.RandomCustomTracks = value),
                    hint: "When checked, random track selection can include custom tracks. Press ENTER to toggle."),
                new CheckBox(
                    "Include custom vehicles in randomization",
                    () => _settings.RandomCustomVehicles,
                    value => _settingsActions.UpdateSetting(() => _settings.RandomCustomVehicles = value),
                    hint: "When checked, random vehicle selection can include custom vehicles. Press ENTER to toggle."),
                new CheckBox(
                    "Enable HRTF audio",
                    () => _settings.HrtfAudio,
                    value => _settingsActions.UpdateSetting(() => _settings.HrtfAudio = value),
                    hint: "When checked, Three-D audio uses HRTF spatialization for more realistic positioning. Press ENTER to toggle."),
                new CheckBox(
                    "Automatic audio device format",
                    () => _settings.AutoDetectAudioDeviceFormat,
                    value => _settingsActions.UpdateSetting(() => _settings.AutoDetectAudioDeviceFormat = value),
                    hint: "When checked, the game uses the device channel count and sample rate. Restart required. Press ENTER to toggle."),
                new Switch(
                    "Units",
                    "metric",
                    "imperial",
                    () => _settings.Units == UnitSystem.Metric,
                    value => _settingsActions.UpdateSetting(() => _settings.Units = value ? UnitSystem.Metric : UnitSystem.Imperial),
                    hint: "Switch between metric and imperial units. Press ENTER to change."),
                new CheckBox(
                    "Enable usage hints",
                    () => _settings.UsageHints,
                    value => _settingsActions.UpdateSetting(() => _settings.UsageHints = value),
                    hint: "When checked, menu items can speak usage hints after a short delay. Press ENTER to toggle."),
                new CheckBox(
                    "Enable menu wrapping",
                    () => _settings.MenuWrapNavigation,
                    value => _settingsActions.UpdateSetting(() => _settings.MenuWrapNavigation = value),
                    onChanged: value => _menu.SetWrapNavigation(value),
                    hint: "When checked, menu navigation wraps from the last item to the first. Press ENTER to toggle."),
                BuildMenuSoundPresetItem(),
                new CheckBox(
                    "Enable menu navigation panning",
                    () => _settings.MenuNavigatePanning,
                    value => _settingsActions.UpdateSetting(() => _settings.MenuNavigatePanning = value),
                    onChanged: value => _menu.SetMenuNavigatePanning(value),
                    hint: "When checked, menu navigation sounds pan left or right based on the item position. Press ENTER to toggle."),
                new MenuItem("Recalibrate screen reader rate", MenuAction.None, onActivate: _settingsActions.RecalibrateScreenReaderRate),
                BackItem()
            };
            return _menu.CreateMenu("options_game", items);
        }

        private MenuScreen BuildOptionsVolumeSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                BuildVolumeSlider(
                    "Master audio volume",
                    () => _settings.AudioVolumes.MasterPercent,
                    value => _settings.AudioVolumes.MasterPercent = value,
                    "Controls the overall audio volume for the game. Set lower to reduce every sound category."),
                BuildVolumeSlider(
                    "Vehicle engine sounds",
                    () => _settings.AudioVolumes.PlayerVehicleEnginePercent,
                    value => _settings.AudioVolumes.PlayerVehicleEnginePercent = value,
                    "Controls your own engine and throttle sounds, including engine start and stop."),
                BuildVolumeSlider(
                    "Vehicle event sounds",
                    () => _settings.AudioVolumes.PlayerVehicleEventsPercent,
                    value => _settings.AudioVolumes.PlayerVehicleEventsPercent = value,
                    "Controls events related to your own vehicle, such as horn, back-fire, and other vehicle events."),
                BuildVolumeSlider(
                    "Other vehicles engine sounds",
                    () => _settings.AudioVolumes.OtherVehicleEnginePercent,
                    value => _settings.AudioVolumes.OtherVehicleEnginePercent = value,
                    "Controls engine-related sounds for bots and other players, including engine start and stop."),
                BuildVolumeSlider(
                    "Other vehicles event sounds",
                    () => _settings.AudioVolumes.OtherVehicleEventsPercent,
                    value => _settings.AudioVolumes.OtherVehicleEventsPercent = value,
                    "Controls horns, crashes, bumps, brakes, and similar event sounds for bots and other players."),
                BuildVolumeSlider(
                    "Surface loop sounds",
                    () => _settings.AudioVolumes.SurfaceLoopsPercent,
                    value => _settings.AudioVolumes.SurfaceLoopsPercent = value,
                    "Controls road and surface loops like asphalt, gravel, etc."),
                BuildVolumeSlider(
                    "Music volume",
                    () => _settings.AudioVolumes.MusicPercent,
                    value =>
                    {
                        _settings.AudioVolumes.MusicPercent = value;
                        _settings.SyncMusicVolumeFromAudioCategories();
                    },
                    "Controls menu and race music volume. This stays synchronized with the menu music volume setting."),
                BuildVolumeSlider(
                    "Online server event sounds",
                    () => _settings.AudioVolumes.OnlineServerEventsPercent,
                    value => _settings.AudioVolumes.OnlineServerEventsPercent = value,
                    "Controls server and multiplayer event sounds such as connection and other events."),
                BackItem()
            };

            return _menu.CreateMenu("options_volume", items);
        }

        private MenuScreen BuildOptionsServerSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(() => $"Default server port: {FormatServerPort(_settings.DefaultServerPort)}", MenuAction.None, onActivate: _server.BeginServerPortEntry),
                BackItem()
            };
            return _menu.CreateMenu("options_server", items);
        }

        private MenuScreen BuildOptionsControlsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(() => $"Select device: {DeviceLabel(_settings.DeviceMode)}", MenuAction.None, nextMenuId: "options_controls_device"),
                new CheckBox(
                    "Force feedback",
                    () => _settings.ForceFeedback,
                    value => _settingsActions.UpdateSetting(() => _settings.ForceFeedback = value),
                    hint: "Enables force feedback or vibration if your controller supports it. Press ENTER to toggle."),
                new MenuItem("Map keyboard keys", MenuAction.None, nextMenuId: "options_controls_keyboard"),
                new MenuItem("Map joystick keys", MenuAction.None, nextMenuId: "options_controls_joystick"),
                BackItem()
            };
            return _menu.CreateMenu("options_controls", items);
        }

        private MenuScreen BuildOptionsControlsDeviceMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Keyboard", MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Keyboard)),
                new MenuItem("Joystick", MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Joystick)),
                new MenuItem("Both", MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Both)),
                BackItem()
            };
            return _menu.CreateMenu("options_controls_device", items, "Select input device");
        }

        private MenuScreen BuildOptionsControlsKeyboardMenu()
        {
            return _menu.CreateMenu("options_controls_keyboard", BuildMappingItems(InputMappingMode.Keyboard));
        }

        private MenuScreen BuildOptionsControlsJoystickMenu()
        {
            return _menu.CreateMenu("options_controls_joystick", BuildMappingItems(InputMappingMode.Joystick));
        }

        private List<MenuItem> BuildMappingItems(InputMappingMode mode)
        {
            var items = new List<MenuItem>();
            foreach (var action in _raceInput.KeyMap.Actions)
            {
                var definition = action;
                items.Add(new MenuItem(
                    () => $"{definition.Label}: {_mapping.FormatMappingValue(definition.Action, mode)}",
                    MenuAction.None,
                    onActivate: () => _mapping.BeginMapping(mode, definition.Action)));
            }
            items.Add(BackItem());
            return items;
        }

        private MenuScreen BuildOptionsRaceSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new RadioButton(
                    "Copilot",
                    new[] { "off", "curves only", "all" },
                    () => (int)_settings.Copilot,
                    value => _settingsActions.UpdateSetting(() => _settings.Copilot = (CopilotMode)value),
                    hint: "Choose what information the copilot reports during the race. Use LEFT or RIGHT to change."),
                new Switch(
                    "Curve announcements",
                    "speed dependent",
                    "fixed distance",
                    () => _settings.CurveAnnouncement == CurveAnnouncementMode.SpeedDependent,
                    value => _settingsActions.UpdateSetting(() => _settings.CurveAnnouncement = value ? CurveAnnouncementMode.SpeedDependent : CurveAnnouncementMode.FixedDistance),
                    hint: "Switch between fixed distance and speed dependent curve announcements. Press ENTER to change."),
                new RadioButton(
                    "Automatic race information",
                    new[] { "off", "laps only", "on" },
                    () => (int)_settings.AutomaticInfo,
                    value => _settingsActions.UpdateSetting(() => _settings.AutomaticInfo = (AutomaticInfoMode)value),
                    hint: "Choose how much automatic race information is spoken, such as lap numbers and player positions. Use LEFT or RIGHT to change."),
                new Slider(
                    "Number of laps",
                    "1-16",
                    () => _settings.NrOfLaps,
                    value => _settingsActions.UpdateSetting(() => _settings.NrOfLaps = value),
                    hint: "Sets how many laps the session will be for single race, time trial, and multiplayer. Use LEFT or RIGHT to change by 1, PAGE UP or PAGE DOWN to change by 10, HOME for maximum, END for minimum."),
                new Slider(
                    "Number of computer players",
                    "1-7",
                    () => _settings.NrOfComputers,
                    value => _settingsActions.UpdateSetting(() => _settings.NrOfComputers = value),
                    hint: "Sets how many computer-controlled cars will race against you. Use LEFT or RIGHT to change by 1, PAGE UP or PAGE DOWN to change by 10, HOME for maximum, END for minimum."),
                new RadioButton(
                    "Single race difficulty",
                    new[] { "easy", "normal", "hard" },
                    () => (int)_settings.Difficulty,
                    value => _settingsActions.UpdateSetting(() => _settings.Difficulty = (RaceDifficulty)value),
                    hint: "Choose the difficulty level for single races. Use LEFT or RIGHT to change."),
                BackItem()
            };
            return _menu.CreateMenu("options_race", items);
        }

        private MenuScreen BuildOptionsLapsMenu()
        {
            var items = new List<MenuItem>();
            for (var laps = 1; laps <= 16; laps++)
            {
                var value = laps;
                items.Add(new MenuItem(laps.ToString(), MenuAction.Back, onActivate: () => _settingsActions.UpdateSetting(() => _settings.NrOfLaps = value)));
            }
            items.Add(BackItem());
            return _menu.CreateMenu("options_race_laps", items, "How many labs should the session be. This applys to single race, time trial and multiPlayer modes.");
        }

        private MenuScreen BuildOptionsComputersMenu()
        {
            var items = new List<MenuItem>();
            for (var count = 1; count <= 7; count++)
            {
                var value = count;
                items.Add(new MenuItem(count.ToString(), MenuAction.Back, onActivate: () => _settingsActions.UpdateSetting(() => _settings.NrOfComputers = value)));
            }
            items.Add(BackItem());
            return _menu.CreateMenu("options_race_computers", items, "Number of computer players");
        }

        private MenuScreen BuildOptionsRestoreMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Yes", MenuAction.Back, onActivate: _settingsActions.RestoreDefaults),
                new MenuItem("No", MenuAction.Back),
                BackItem()
            };
            return _menu.CreateMenu("options_restore", items, "Are you sure you would like to restore all settings to their default values?");
        }

        private Slider BuildVolumeSlider(string label, Func<int> getter, Action<int> setter, string hint)
        {
            return new Slider(
                label,
                "0-100",
                getter,
                value => _settingsActions.UpdateSetting(() =>
                {
                    _settings.AudioVolumes ??= new AudioVolumeSettings();
                    setter(value);
                    _settings.AudioVolumes.ClampAll();
                    _settings.SyncMusicVolumeFromAudioCategories();
                }),
                onChanged: _ => _audio.ApplyAudioSettings(),
                hint: $"{hint} Use LEFT or RIGHT to change by 1, PAGE UP or PAGE DOWN to change by 10, HOME for maximum, END for minimum.");
        }

        private MenuItem BuildMenuSoundPresetItem()
        {
            if (_menuSoundPresets.Count < 2)
            {
                return new MenuItem(
                    () => $"Menu sounds: {(_menuSoundPresets.Count > 0 ? _menuSoundPresets[0] : "default")}",
                    MenuAction.None);
            }

            return new RadioButton(
                "Menu sounds",
                _menuSoundPresets,
                () => GetMenuSoundPresetIndex(),
                value => _settingsActions.UpdateSetting(() => _settings.MenuSoundPreset = _menuSoundPresets[value]),
                onChanged: _ => _menu.SetMenuSoundPreset(_settings.MenuSoundPreset),
                hint: "Select the menu sound preset. Use LEFT or RIGHT to change.");
        }

        private int GetMenuSoundPresetIndex()
        {
            if (_menuSoundPresets.Count == 0)
                return 0;
            for (var i = 0; i < _menuSoundPresets.Count; i++)
            {
                if (string.Equals(_menuSoundPresets[i], _settings.MenuSoundPreset, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return 0;
        }
    }
}
