using System;
using System.Collections.Generic;
using TopSpeed.Core.Settings;
using TopSpeed.Menu;

using TopSpeed.Localization;
namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
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
                var key = string.IsNullOrWhiteSpace(issue.Field)
                    ? LocalizationService.Translate(LocalizationService.Mark("unknown"))
                    : issue.Field;
                var line = message.StartsWith("The key ", StringComparison.OrdinalIgnoreCase)
                    ? IssueSeverityLabel(issue.Severity) + " " + message
                    : LocalizationService.Format(
                        LocalizationService.Mark("{0} key '{1}': {2}"),
                        IssueSeverityLabel(issue.Severity),
                        key,
                        message);
                items.Add(new DialogItem(line));
            }

            if (items.Count == 0)
                return false;

            var hasWholeFileParseError = HasWholeFileParseError(_settingsIssues);
            var title = hasWholeFileParseError
                ? LocalizationService.Mark("Settings file parse error")
                : LocalizationService.Mark("Settings notice");
            var caption = hasWholeFileParseError
                ? LocalizationService.Mark("The entire settings file could not be parsed. Defaults were loaded. Review this error before continuing.")
                : LocalizationService.Mark("Some settings were missing or invalid. Review these details.");

            var dialog = new Dialog(
                title,
                caption,
                QuestionId.Ok,
                items,
                onResult: _ => onClose?.Invoke(),
                new DialogButton(QuestionId.Ok, LocalizationService.Mark("OK")));
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
                    return LocalizationService.Translate(LocalizationService.Mark("Error:"));
                case SettingsIssueSeverity.Warning:
                    return LocalizationService.Translate(LocalizationService.Mark("Warning:"));
                default:
                    return LocalizationService.Translate(LocalizationService.Mark("Info:"));
            }
        }
    }
}

