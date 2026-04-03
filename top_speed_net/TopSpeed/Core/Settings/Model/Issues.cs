using System;
using System.Collections.Generic;
using TopSpeed.Input;

namespace TopSpeed.Core.Settings
{
    internal enum SettingsIssueSeverity
    {
        Info,
        Warning,
        Error
    }

    internal sealed class SettingsIssue
    {
        public SettingsIssue(SettingsIssueSeverity severity, string field, string message)
        {
            Severity = severity;
            Field = field ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public SettingsIssueSeverity Severity { get; }
        public string Field { get; }
        public string Message { get; }
    }

    internal sealed class SettingsLoadResult
    {
        public SettingsLoadResult(RaceSettings settings, IReadOnlyList<SettingsIssue> issues, bool settingsFileMissing = false)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Issues = issues ?? Array.Empty<SettingsIssue>();
            SettingsFileMissing = settingsFileMissing;
        }

        public RaceSettings Settings { get; }
        public IReadOnlyList<SettingsIssue> Issues { get; }
        public bool SettingsFileMissing { get; }
    }
}

