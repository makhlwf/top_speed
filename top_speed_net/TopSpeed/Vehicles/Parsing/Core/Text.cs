using System;
using System.Collections.Generic;
using System.Globalization;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
    {
        private static bool TryParseSectionHeader(string line, out string name)
        {
            name = string.Empty;
            if (!line.StartsWith("[", StringComparison.Ordinal) ||
                !line.EndsWith("]", StringComparison.Ordinal) ||
                line.Length < 3)
            {
                return false;
            }

            name = NormalizeKey(line.Substring(1, line.Length - 2));
            return name.Length > 0;
        }

        private static bool TryParseKeyValue(string line, out string key, out string value)
        {
            key = string.Empty;
            value = string.Empty;
            var sep = line.IndexOf('=');
            if (sep <= 0)
                return false;
            key = line.Substring(0, sep).Trim();
            value = line.Substring(sep + 1).Trim();
            return key.Length > 0;
        }

        private static string NormalizeKey(string raw)
        {
            var normalized = raw.Trim().ToLowerInvariant();
            if (normalized == "back_fire")
                return "backfire";
            return normalized;
        }

        private static string StripInlineComment(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            var hash = line.IndexOf('#');
            var semi = line.IndexOf(';');
            var cut = -1;
            if (hash >= 0 && semi >= 0) cut = Math.Min(hash, semi);
            else if (hash >= 0) cut = hash;
            else if (semi >= 0) cut = semi;
            return cut >= 0 ? line.Substring(0, cut) : line;
        }

        private static bool TryParseBool(string raw, out bool value)
        {
            switch (raw.Trim().ToLowerInvariant())
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                    value = true;
                    return true;
                case "0":
                case "false":
                case "no":
                case "off":
                    value = false;
                    return true;
                default:
                    value = false;
                    return false;
            }
        }

        private static bool TryParseFloat(string raw, out float value)
        {
            return float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool HasErrors(List<VehicleTsvIssue> issues)
        {
            for (var i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == VehicleTsvIssueSeverity.Error)
                    return true;
            }
            return false;
        }

        private static List<string> ParseCsvStrings(string raw)
        {
            var result = new List<string>();
            var tokens = raw.Split(',');
            for (var i = 0; i < tokens.Length; i++)
            {
                var value = tokens[i].Trim();
                if (value.Length > 0)
                    result.Add(value);
            }
            return result;
        }

        private static float CalculateTireCircumferenceM(int widthMm, int aspectPercent, int rimInches)
        {
            var sidewallMm = widthMm * (aspectPercent / 100f);
            var diameterMm = (rimInches * 25.4f) + (2f * sidewallMm);
            return (float)(Math.PI * (diameterMm / 1000f));
        }
    }
}
