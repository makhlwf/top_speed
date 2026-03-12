using System.Collections.Generic;
using TopSpeed.Input;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static void ApplyDocument(RaceSettings settings, SettingsFileDocument document, List<SettingsIssue> issues)
        {
            settings.Language = string.IsNullOrWhiteSpace(document.Language)
                ? settings.Language
                : document.Language!;

            if (document.Audio == null)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "audio", "The audio section is missing. Defaults were used for audio settings."));
            else
                ApplyAudio(settings, document.Audio, issues);

            if (document.Input == null)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "input", "The input section is missing. Defaults were used for input settings."));
            else
                ApplyInput(settings, document.Input, issues);

            if (document.Race == null)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "race", "The race section is missing. Defaults were used for race settings."));
            else
                ApplyRace(settings, document.Race, issues);

            if (document.Ui == null)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "ui", "The ui section is missing. Defaults were used for menu settings."));
            else
                ApplyUi(settings, document.Ui, issues);

            if (document.Network == null)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "network", "The network section is missing. Defaults were used for network settings."));
            else
                ApplyNetwork(settings, document.Network, issues);

            if (document.Accessibility == null)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "accessibility", "The accessibility section is missing. Defaults were used for accessibility settings."));
            else
                ApplyAccessibility(settings, document.Accessibility, issues);

            if (document.Radio != null)
                ApplyRadio(settings, document.Radio, issues);
        }
    }
}
