using System;
using System.IO;
using System.Text.RegularExpressions;
using TopSpeed.Localization;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static string BuildSettingsParseErrorMessage(string settingsPath, Exception ex)
        {
            var details = ex?.Message ?? LocalizationService.Mark("Unknown error.");
            if (TryFindInvalidBooleanToken(settingsPath, out var key, out var value))
            {
                details = LocalizationService.Format(
                    LocalizationService.Mark("The value '{0}' for the key '{1}' could not be parsed as Boolean. {2}"),
                    value,
                    key,
                    details);
            }

            return LocalizationService.Format(
                LocalizationService.Mark("Settings file '{0}' could not be read as valid JSON. Defaults were used. Details: {1}"),
                Path.GetFileName(settingsPath),
                details);
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

