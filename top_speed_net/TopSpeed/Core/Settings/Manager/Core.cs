using System;
using System.Collections.Generic;
using System.IO;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static readonly KeyValuePair<string, string>[] BooleanKeys =
        {
            new KeyValuePair<string, string>("hrtfAudio", "audio.hrtfAudio"),
            new KeyValuePair<string, string>("stereoWidening", "audio.stereoWidening"),
            new KeyValuePair<string, string>("autoDetectAudioDeviceFormat", "audio.autoDetectAudioDeviceFormat"),
            new KeyValuePair<string, string>("forceFeedback", "input.forceFeedback"),
            new KeyValuePair<string, string>("randomCustomTracks", "race.randomCustomTracks"),
            new KeyValuePair<string, string>("randomCustomVehicles", "race.randomCustomVehicles"),
            new KeyValuePair<string, string>("singleRaceCustomVehicles", "race.singleRaceCustomVehicles"),
            new KeyValuePair<string, string>("usageHints", "ui.usageHints"),
            new KeyValuePair<string, string>("menuWrapNavigation", "ui.menuWrapNavigation"),
            new KeyValuePair<string, string>("menuNavigatePanning", "ui.menuNavigatePanning"),
            new KeyValuePair<string, string>("shuffleEnabled", "radio.shuffleEnabled")
        };

        private const string SettingsFileName = "settings.json";
        private const int CurrentSchemaVersion = 1;
        private readonly string _settingsPath = string.Empty;

        public SettingsManager(string? settingsPath = null)
        {
            _settingsPath = string.IsNullOrWhiteSpace(settingsPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), SettingsFileName)
                : settingsPath!;
        }
    }
}

