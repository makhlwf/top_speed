using System;
using System.Collections.Generic;
using System.Globalization;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
    {
        private static string RequireString(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, $"Missing required key '{key}' in section [{section.Name}]."));
                return string.Empty;
            }

            var value = entry.Value.Trim();
            if (value.Length == 0)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, $"Key '{key}' in section [{section.Name}] must not be empty."));
            return value;
        }

        private static string? OptionalString(Section section, string key)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
                return null;
            var value = entry.Value.Trim();
            return value.Length == 0 ? null : value;
        }

        private static IReadOnlyList<string> RequireCsvStrings(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, $"Missing required key '{key}' in section [{section.Name}]."));
                return Array.Empty<string>();
            }

            var values = ParseCsvStrings(entry.Value);
            if (values.Count == 0)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, $"Key '{key}' must contain at least one path."));
            return values;
        }

        private static IReadOnlyList<string> OptionalCsvStrings(Section section, string key)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
                return Array.Empty<string>();
            return ParseCsvStrings(entry.Value);
        }

        private static int RequireIntRange(Section section, string key, int min, int max, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, $"Missing required key '{key}' in section [{section.Name}]."));
                return min;
            }

            if (!int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, $"Key '{key}' must be an integer."));
                return min;
            }

            if (value < min || value > max)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, $"Key '{key}' value {value} is outside allowed range {min} to {max}."));
            return value;
        }

        private static int? OptionalInt(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
                return null;
            if (!int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, $"Key '{key}' must be an integer."));
                return null;
            }
            return value;
        }

        private static float RequireFloatRange(Section section, string key, float min, float max, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, $"Missing required key '{key}' in section [{section.Name}]."));
                return min;
            }

            if (!TryParseFloat(entry.Value, out var value))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, $"Key '{key}' must be a float."));
                return min;
            }

            if (value < min || value > max)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    entry.Line,
                    $"Key '{key}' value {value.ToString(CultureInfo.InvariantCulture)} is outside allowed range {min.ToString(CultureInfo.InvariantCulture)} to {max.ToString(CultureInfo.InvariantCulture)}."));
            }

            return value;
        }

        private static float? OptionalFloat(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
                return null;
            if (!TryParseFloat(entry.Value, out var value))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, $"Key '{key}' must be a float."));
                return null;
            }
            return value;
        }

        private static bool RequireBoolInt(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, $"Missing required key '{key}' in section [{section.Name}]."));
                return false;
            }

            if (TryParseBool(entry.Value, out var boolValue))
                return boolValue;
            if (int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                return intValue != 0;

            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, $"Key '{key}' must be a boolean or 0/1 integer."));
            return false;
        }

        private static List<float>? RequireFloatCsv(Section section, string key, List<VehicleTsvIssue> issues)
        {
            if (!section.Entries.TryGetValue(key, out var entry))
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, section.Line, $"Missing required key '{key}' in section [{section.Name}]."));
                return null;
            }

            var values = new List<float>();
            var tokens = entry.Value.Split(',');
            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i].Trim();
                if (token.Length == 0)
                    continue;
                if (!TryParseFloat(token, out var parsed))
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, $"Key '{key}' contains a non-float value '{token}'."));
                    return null;
                }
                values.Add(parsed);
            }

            if (values.Count == 0)
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line, $"Key '{key}' must contain at least one float value."));
                return null;
            }

            return values;
        }
    }
}
