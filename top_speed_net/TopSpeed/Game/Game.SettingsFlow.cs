using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Menu;
using TopSpeed.Core.Settings;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void SaveSettings()
        {
            _settingsManager.Save(_settings);
        }

        private void RestoreDefaults()
        {
            _settings.RestoreDefaults();
            _raceInput.SetDevice(_settings.DeviceMode);
            _input.SetDeviceMode(_settings.DeviceMode);
            _speech.ScreenReaderRateMs = _settings.ScreenReaderRateMs;
            _needsCalibration = _settings.ScreenReaderRateMs <= 0f;
            _menu.SetWrapNavigation(_settings.MenuWrapNavigation);
            _menu.SetMenuSoundPreset(_settings.MenuSoundPreset);
            _menu.SetMenuNavigatePanning(_settings.MenuNavigatePanning);
            ApplyAudioSettings();
            SaveSettings();
            _speech.Speak("Defaults restored.");
        }

        private void SetDevice(InputDeviceMode mode)
        {
            _settings.DeviceMode = mode;
            _raceInput.SetDevice(mode);
            _input.SetDeviceMode(mode);
            SaveSettings();
        }

        private void UpdateSetting(Action update)
        {
            update();
            SaveSettings();
        }

        private bool ShowSettingsIssuesDialog(Action? onClose = null)
        {
            if (_settingsIssues == null || _settingsIssues.Count == 0)
                return false;

            var items = new List<DialogItem>();
            for (var i = 0; i < _settingsIssues.Count; i++)
            {
                var issue = _settingsIssues[i];
                if (issue == null || string.IsNullOrWhiteSpace(issue.Message))
                    continue;
                if (ShouldSkipSettingsIssue(issue))
                    continue;
                var message = issue.Message.Trim();
                var key = string.IsNullOrWhiteSpace(issue.Field) ? "unknown" : issue.Field;
                var line = message.StartsWith("The key ", StringComparison.OrdinalIgnoreCase)
                    ? $"{IssueSeverityLabel(issue.Severity)} {message}"
                    : $"{IssueSeverityLabel(issue.Severity)} key '{key}': {message}";
                items.Add(new DialogItem(line));
            }

            if (items.Count == 0)
                return false;

            var hasWholeFileParseError = HasWholeFileParseError(_settingsIssues);
            var title = hasWholeFileParseError ? "Settings file parse error" : "Settings notice";
            var caption = hasWholeFileParseError
                ? "The entire settings file could not be parsed. Defaults were loaded. Review this error before continuing."
                : "Some settings were missing or invalid. Review these details.";

            var dialog = new Dialog(
                title,
                caption,
                QuestionId.Ok,
                items,
                onResult: _ => onClose?.Invoke(),
                new DialogButton(QuestionId.Ok, "OK"));
            _dialogs.Show(dialog);
            return true;
        }

        private static bool ShouldSkipSettingsIssue(SettingsIssue issue)
        {
            if (issue == null)
                return true;

            if (issue.Severity != SettingsIssueSeverity.Info)
                return false;

            if (!string.Equals(issue.Field, "settings", StringComparison.OrdinalIgnoreCase))
                return false;

            return issue.Message.IndexOf("was not found", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasWholeFileParseError(IReadOnlyList<SettingsIssue> issues)
        {
            if (issues == null || issues.Count == 0)
                return false;

            for (var i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                if (issue == null)
                    continue;
                if (issue.Severity != SettingsIssueSeverity.Error)
                    continue;
                if (!string.Equals(issue.Field, "settings", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (issue.Message.IndexOf("could not be read as valid JSON", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static string IssueSeverityLabel(SettingsIssueSeverity severity)
        {
            switch (severity)
            {
                case SettingsIssueSeverity.Error:
                    return "Error:";
                case SettingsIssueSeverity.Warning:
                    return "Warning:";
                default:
                    return "Info:";
            }
        }

        private void SaveMusicVolume(float volume)
        {
            _settings.MusicVolume = volume;
            _settings.SyncAudioCategoriesFromMusicVolume();
            ApplyAudioSettings();
            SaveSettings();
        }

        private void ApplyAudioSettings()
        {
            _settings.AudioVolumes ??= new AudioVolumeSettings();
            _settings.SyncMusicVolumeFromAudioCategories();
            _audio.SetMasterVolume(_settings.GetCategoryScalar(AudioVolumeCategory.Master));
            _menu.SetMenuMusicVolume(_settings.MusicVolume);
        }
    }
}
