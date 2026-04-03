using System;
using System.Collections.Generic;
using System.Globalization;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Localization;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static int ClampPercent(int value, string field, List<SettingsIssue> issues)
        {
            var clamped = AudioVolumeSettings.ClampPercent(value);
            if (clamped != value)
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, BuildInvalidValueMessage(field, value, clamped)));
            return clamped;
        }

        private static int ReadDefaultServerPort(int? value, int fallback, string field, List<SettingsIssue> issues)
        {
            if (!value.HasValue)
                return fallback;

            var current = value.Value;
            if (current >= 1 && current <= 65535)
                return current;

            issues.Add(new SettingsIssue(
                SettingsIssueSeverity.Warning,
                field,
                BuildInvalidValueMessage(field, current, fallback)));
            return fallback;
        }

        private static int ClampInt(int? value, int fallback, int min, int max, string field, List<SettingsIssue> issues)
        {
            if (!value.HasValue)
                return fallback;

            var current = value.Value;
            if (current < min)
            {
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, BuildInvalidValueMessage(field, current, min)));
                return min;
            }

            if (current > max)
            {
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, BuildInvalidValueMessage(field, current, max)));
                return max;
            }

            return current;
        }

        private static float ClampFloat(float value, float min, float max, string field, List<SettingsIssue> issues)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, BuildInvalidValueMessage(
                    field,
                    value.ToString(CultureInfo.InvariantCulture),
                    min.ToString(CultureInfo.InvariantCulture))));
                return min;
            }

            if (value < min)
            {
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, BuildInvalidValueMessage(
                    field,
                    value.ToString(CultureInfo.InvariantCulture),
                    min.ToString(CultureInfo.InvariantCulture))));
                return min;
            }

            if (value > max)
            {
                issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, BuildInvalidValueMessage(
                    field,
                    value.ToString(CultureInfo.InvariantCulture),
                    max.ToString(CultureInfo.InvariantCulture))));
                return max;
            }

            return value;
        }

        private static Key ReadKey(int? value, Key fallback, string field, List<SettingsIssue> issues)
        {
            if (!value.HasValue)
                return fallback;

            if (Enum.IsDefined(typeof(Key), value.Value) && value.Value >= 0)
                return (Key)value.Value;

            issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, BuildInvalidValueMessage(field, value.Value, (int)fallback)));
            return fallback;
        }

        private static AxisOrButton ReadController(int? value, AxisOrButton fallback, string field, List<SettingsIssue> issues)
        {
            if (!value.HasValue)
                return fallback;

            if (Enum.IsDefined(typeof(AxisOrButton), value.Value) && value.Value >= 0)
                return (AxisOrButton)value.Value;

            issues.Add(new SettingsIssue(SettingsIssueSeverity.Warning, field, BuildInvalidValueMessage(field, value.Value, (int)fallback)));
            return fallback;
        }

        private static TEnum ReadEnum<TEnum>(int? value, TEnum fallback, string field, List<SettingsIssue> issues)
            where TEnum : struct, Enum
        {
            if (!value.HasValue)
                return fallback;

            var enumType = typeof(TEnum);
            object? converted = null;
            try
            {
                var underlyingType = Enum.GetUnderlyingType(enumType);
                converted = Convert.ChangeType(value.Value, underlyingType, CultureInfo.InvariantCulture);
            }
            catch (OverflowException)
            {
            }
            catch (InvalidCastException)
            {
            }

            if (converted != null && Enum.IsDefined(enumType, converted))
                return (TEnum)Enum.ToObject(enumType, converted);

            issues.Add(new SettingsIssue(
                SettingsIssueSeverity.Warning,
                field,
                BuildInvalidValueMessage(field, value.Value, Enum.Format(enumType, fallback, "D"))));
            return fallback;
        }

        private static string BuildInvalidValueMessage(string field, object invalidValue, object replacementValue)
        {
            return LocalizationService.Format(
                LocalizationService.Mark("The key {0} has invalid value {1}, which was replaced with {2}."),
                field,
                invalidValue,
                replacementValue);
        }
    }
}


