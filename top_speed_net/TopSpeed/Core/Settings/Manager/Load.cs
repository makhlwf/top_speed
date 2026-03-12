using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using TopSpeed.Input;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
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
    }
}
