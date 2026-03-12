using System;
using System.Collections.Generic;
using System.Globalization;
using TopSpeed.Vehicles;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
    {
        private static bool ValidatePolicy(
            Section? policy,
            int gears,
            float idleRpm,
            float revLimiter,
            List<VehicleTsvIssue> issues)
        {
            if (policy == null)
                return true;

            if (policy.Entries.TryGetValue("top_speed_gear", out var topGear))
            {
                if (!int.TryParse(topGear.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var g) || g < 1 || g > gears)
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, topGear.Line, $"top_speed_gear must be within 1..{gears}."));
            }

            ValidateOptionalPolicyFloat(policy, "base_auto_shift_cooldown", 0f, 2f, issues);
            ValidateOptionalPolicyFloat(policy, "upshift_delay_default", 0f, 2f, issues);
            ValidateOptionalPolicyFloat(policy, "auto_upshift_rpm_fraction", 0.05f, 1.0f, issues);
            ValidateOptionalPolicyFloat(policy, "auto_downshift_rpm_fraction", 0.05f, 0.95f, issues);
            ValidateOptionalPolicyFloat(policy, "top_speed_pursuit_speed_fraction", 0.50f, 1.20f, issues);
            ValidateOptionalPolicyFloat(policy, "upshift_hysteresis", 0f, 2f, issues);
            ValidateOptionalPolicyFloat(policy, "min_upshift_net_accel_mps2", -20f, 20f, issues);

            if (policy.Entries.TryGetValue("auto_upshift_rpm", out var upAbs))
            {
                if (!TryParseFloat(upAbs.Value, out var rpm) || rpm < 0f || (rpm > 0f && (rpm < idleRpm || rpm > revLimiter)))
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, upAbs.Line, "auto_upshift_rpm must be 0 or between idle_rpm and rev_limiter."));
            }

            if (policy.Entries.TryGetValue("auto_downshift_rpm", out var downAbs))
            {
                if (!TryParseFloat(downAbs.Value, out var rpm) || rpm < 0f || (rpm > 0f && (rpm < idleRpm || rpm > revLimiter)))
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, downAbs.Line, "auto_downshift_rpm must be 0 or between idle_rpm and rev_limiter."));
            }

            foreach (var kvp in policy.Entries)
            {
                if (!kvp.Key.StartsWith("upshift_delay_", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.Equals(kvp.Key, "upshift_delay_default", StringComparison.OrdinalIgnoreCase))
                {
                    if (kvp.Key.StartsWith("upshift_delay_g", StringComparison.OrdinalIgnoreCase))
                    {
                        var rawGear = kvp.Key.Substring("upshift_delay_g".Length);
                        if (!int.TryParse(rawGear, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sourceGear) ||
                            sourceGear < 1 || sourceGear > gears)
                        {
                            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, kvp.Value.Line, $"Invalid key '{kvp.Key}'. Source gear must be within 1..{gears}."));
                        }
                    }
                    else
                    {
                        var suffix = kvp.Key.Substring("upshift_delay_".Length);
                        var parts = suffix.Split('_');
                        if (parts.Length != 2 ||
                            !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var g1) ||
                            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var g2) ||
                            g1 < 1 || g1 > gears || g2 != g1 + 1 || g2 > gears)
                        {
                            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, kvp.Value.Line, $"Invalid key '{kvp.Key}'. Use upshift_delay_X_Y for adjacent gears."));
                        }
                    }
                }

                if (!TryParseFloat(kvp.Value.Value, out var delay) || delay < 0f || delay > 2f)
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, kvp.Value.Line, $"{kvp.Key} must be a float between 0 and 2 seconds."));
            }

            return !HasErrors(issues);
        }

        private static void ValidateOptionalPolicyFloat(Section policy, string key, float min, float max, List<VehicleTsvIssue> issues)
        {
            if (!policy.Entries.TryGetValue(key, out var entry))
                return;
            if (!TryParseFloat(entry.Value, out var value) || value < min || value > max)
            {
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, entry.Line,
                    $"{key} must be between {min.ToString(CultureInfo.InvariantCulture)} and {max.ToString(CultureInfo.InvariantCulture)}."));
            }
        }

        private static TransmissionPolicy BuildTransmissionPolicy(Section? policy, int gears, float idleRpm, float revLimiter, float autoShiftRpm)
        {
            if (policy == null)
                return TransmissionPolicy.Default;

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in policy.Entries)
                values[$"policy.{kvp.Key}"] = kvp.Value.Value;

            var resolvedGears = Math.Max(1, gears);
            var intendedTopSpeedGear = ReadInt(values, "policy.top_speed_gear", resolvedGears);
            var allowOverdrive = ReadBool(values, "policy.allow_overdrive_above_game_top_speed", false);

            var baseCooldown = ReadFloat(values, "policy.base_auto_shift_cooldown", 0.15f);
            var fallbackUpshiftDelay = ReadFloat(values, "policy.upshift_delay_default", baseCooldown);
            var perGearUpshiftDelays = new float[resolvedGears];
            for (var gear = 1; gear <= resolvedGears; gear++)
            {
                perGearUpshiftDelays[gear - 1] = fallbackUpshiftDelay;
                if (gear >= resolvedGears)
                    continue;
                var transitionKey = $"policy.upshift_delay_{gear}_{gear + 1}";
                var sourceGearKey = $"policy.upshift_delay_g{gear}";
                var overrideDelay = ReadFloat(values, transitionKey, float.NaN);
                if (!float.IsNaN(overrideDelay))
                {
                    perGearUpshiftDelays[gear - 1] = overrideDelay;
                    continue;
                }
                overrideDelay = ReadFloat(values, sourceGearKey, float.NaN);
                if (!float.IsNaN(overrideDelay))
                    perGearUpshiftDelays[gear - 1] = overrideDelay;
            }

            var defaultUpshiftFraction = 0.92f;
            if (revLimiter > idleRpm && autoShiftRpm > 0f)
                defaultUpshiftFraction = Math.Max(0.05f, Math.Min(1.0f, (autoShiftRpm - idleRpm) / (revLimiter - idleRpm)));

            var upshiftRpmFraction = ReadFloat(values, "policy.auto_upshift_rpm_fraction", defaultUpshiftFraction);
            var upshiftRpmAbsolute = ReadFloat(values, "policy.auto_upshift_rpm", 0f);
            if (upshiftRpmAbsolute > 0f && revLimiter > idleRpm)
                upshiftRpmFraction = (upshiftRpmAbsolute - idleRpm) / (revLimiter - idleRpm);

            var downshiftRpmFraction = ReadFloat(values, "policy.auto_downshift_rpm_fraction", 0.35f);
            var downshiftRpmAbsolute = ReadFloat(values, "policy.auto_downshift_rpm", 0f);
            if (downshiftRpmAbsolute > 0f && revLimiter > idleRpm)
                downshiftRpmFraction = (downshiftRpmAbsolute - idleRpm) / (revLimiter - idleRpm);

            return new TransmissionPolicy(
                intendedTopSpeedGear: intendedTopSpeedGear,
                allowOverdriveAboveGameTopSpeed: allowOverdrive,
                upshiftRpmFraction: upshiftRpmFraction,
                downshiftRpmFraction: downshiftRpmFraction,
                upshiftHysteresis: ReadFloat(values, "policy.upshift_hysteresis", 0.05f),
                baseAutoShiftCooldownSeconds: baseCooldown,
                minUpshiftNetAccelerationMps2: ReadFloat(values, "policy.min_upshift_net_accel_mps2", -0.05f),
                topSpeedPursuitSpeedFraction: ReadFloat(values, "policy.top_speed_pursuit_speed_fraction", 0.97f),
                preferIntendedTopSpeedGearNearLimit: ReadBool(values, "policy.prefer_intended_top_speed_gear_near_limit", true),
                upshiftCooldownBySourceGear: perGearUpshiftDelays);
        }
    }
}
