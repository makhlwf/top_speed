using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using TopSpeed.Input;

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
            new KeyValuePair<string, string>("menuNavigatePanning", "ui.menuNavigatePanning")
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

        public SettingsLoadResult Load()
        {
            var settings = new RaceSettings();
            var issues = new List<SettingsIssue>();

            if (!File.Exists(_settingsPath))
            {
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Info,
                    "settings",
                    $"Settings file '{Path.GetFileName(_settingsPath)}' was not found. Default settings were created."));
                Save(settings);
                return new SettingsLoadResult(settings, new ReadOnlyCollection<SettingsIssue>(issues));
            }

            SettingsFileDocument? document;
            try
            {
                document = ReadDocument(_settingsPath);
            }
            catch (IOException ex)
            {
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Error,
                    "settings",
                    BuildSettingsParseErrorMessage(_settingsPath, ex)));
                Save(settings);
                return new SettingsLoadResult(settings, new ReadOnlyCollection<SettingsIssue>(issues));
            }
            catch (UnauthorizedAccessException ex)
            {
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Error,
                    "settings",
                    BuildSettingsParseErrorMessage(_settingsPath, ex)));
                Save(settings);
                return new SettingsLoadResult(settings, new ReadOnlyCollection<SettingsIssue>(issues));
            }
            catch (SerializationException ex)
            {
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Error,
                    "settings",
                    BuildSettingsParseErrorMessage(_settingsPath, ex)));
                Save(settings);
                return new SettingsLoadResult(settings, new ReadOnlyCollection<SettingsIssue>(issues));
            }
            catch (InvalidDataException ex)
            {
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Error,
                    "settings",
                    BuildSettingsParseErrorMessage(_settingsPath, ex)));
                Save(settings);
                return new SettingsLoadResult(settings, new ReadOnlyCollection<SettingsIssue>(issues));
            }
            catch (FormatException ex)
            {
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Error,
                    "settings",
                    BuildSettingsParseErrorMessage(_settingsPath, ex)));
                Save(settings);
                return new SettingsLoadResult(settings, new ReadOnlyCollection<SettingsIssue>(issues));
            }

            if (document == null)
            {
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Error,
                    "settings",
                    $"Settings file '{Path.GetFileName(_settingsPath)}' is empty or invalid. Defaults were used."));
                Save(settings);
                return new SettingsLoadResult(settings, new ReadOnlyCollection<SettingsIssue>(issues));
            }

            if (document.SchemaVersion.GetValueOrDefault() != CurrentSchemaVersion)
            {
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Error,
                    "schemaVersion",
                    $"Unsupported settings schema version '{document.SchemaVersion?.ToString(CultureInfo.InvariantCulture) ?? "missing"}'. Expected {CurrentSchemaVersion}. Defaults were used."));
                Save(settings);
                return new SettingsLoadResult(settings, new ReadOnlyCollection<SettingsIssue>(issues));
            }

            ApplyDocument(settings, document, issues);
            settings.AudioVolumes ??= new AudioVolumeSettings();
            settings.AudioVolumes.ClampAll();
            settings.SyncMusicVolumeFromAudioCategories();
            Save(settings);
            return new SettingsLoadResult(settings, new ReadOnlyCollection<SettingsIssue>(issues));
        }

        public void Save(RaceSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            settings.AudioVolumes ??= new AudioVolumeSettings();
            settings.AudioVolumes.ClampAll();
            settings.SyncMusicVolumeFromAudioCategories();

            var document = BuildDocument(settings);
            try
            {
                WriteDocument(_settingsPath, document);
            }
            catch (IOException)
            {
                // Ignore settings write failures.
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore settings write failures.
            }
            catch (SerializationException)
            {
                // Ignore settings write failures.
            }
            catch (NotSupportedException)
            {
                // Ignore settings write failures.
            }
        }

        private static string BuildSettingsParseErrorMessage(string settingsPath, Exception ex)
        {
            var details = ex?.Message ?? "Unknown error.";
            if (TryFindInvalidBooleanToken(settingsPath, out var key, out var value))
            {
                details = $"The value '{value}' for the key '{key}' could not be parsed as Boolean. {details}";
            }

            return $"Settings file '{Path.GetFileName(settingsPath)}' could not be read as valid JSON. Defaults were used. Details: {details}";
        }

        private static bool TryFindInvalidBooleanToken(string settingsPath, out string key, out string value)
        {
            key = string.Empty;
            value = string.Empty;

            string json;
            try
            {
                json = File.ReadAllText(settingsPath);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }

            for (var i = 0; i < BooleanKeys.Length; i++)
            {
                var entry = BooleanKeys[i];
                var pattern = $"\\\"{Regex.Escape(entry.Key)}\\\"\\s*:\\s*(?<value>[^,\\r\\n\\}}\\]]+)";
                var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (!match.Success)
                    continue;

                var token = match.Groups["value"].Value.Trim();
                if (string.Equals(token, "true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(token, "false", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(token, "null", StringComparison.OrdinalIgnoreCase))
                    continue;

                key = entry.Value;
                value = token;
                return true;
            }

            return false;
        }
    }
}
