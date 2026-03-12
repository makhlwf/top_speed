using System.Collections.Generic;
using TopSpeed.Input;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static void ApplyAccessibility(RaceSettings settings, SettingsAccessibilityDocument accessibility, List<SettingsIssue> issues)
        {
            if (!accessibility.ScreenReaderRateMs.HasValue)
                return;

            var value = (float)accessibility.ScreenReaderRateMs.Value;
            if (!float.IsNaN(value) && !float.IsInfinity(value))
            {
                settings.ScreenReaderRateMs = ClampFloat(value, 0f, float.MaxValue, "accessibility.screenReaderRateMs", issues);
            }
            else
            {
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, "accessibility.screenReaderRateMs", "Screen reader rate is not a valid number and was reset to default."));
            }
        }

        private static void ApplyRadio(RaceSettings settings, SettingsRadioDocument radio, List<SettingsIssue> issues)
        {
            if (radio.LastFolder != null)
                settings.RadioLastFolder = radio.LastFolder.Trim();

            if (radio.ShuffleEnabled.HasValue)
                settings.RadioShuffle = radio.ShuffleEnabled.Value;
        }
    }
}
