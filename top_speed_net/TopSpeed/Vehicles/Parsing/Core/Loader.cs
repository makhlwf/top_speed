using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
    {
        private sealed class Entry
        {
            public Entry(string value, int line)
            {
                Value = value;
                Line = line;
            }

            public string Value { get; }
            public int Line { get; }
        }

        private sealed class Section
        {
            public Section(string name, int line)
            {
                Name = name;
                Line = line;
                Entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            }

            public string Name { get; }
            public int Line { get; }
            public Dictionary<string, Entry> Entries { get; }
        }

        private static readonly string[] s_requiredSections =
        {
            "meta", "sounds", "general", "engine", "torque", "torque_curve", "drivetrain", "gears", "steering", "tire_model", "dynamics", "dimensions", "tires"
        };

        private static readonly Dictionary<string, HashSet<string>> s_allowedKeys = BuildAllowedKeys();

        public static bool TryLoadFromFile(string path, out CustomVehicleTsvData data)
        {
            return TryLoadFromFile(path, out data, out _);
        }

        public static bool TryLoadFromFile(string path, out CustomVehicleTsvData data, out IReadOnlyList<VehicleTsvIssue> issues)
        {
            data = null!;
            var issueList = new List<VehicleTsvIssue>();
            issues = issueList;

            if (string.IsNullOrWhiteSpace(path))
            {
                issueList.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, 0, "Vehicle file path is empty."));
                return false;
            }

            if (!File.Exists(path))
            {
                issueList.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, 0, $"Vehicle file not found: {path}"));
                return false;
            }

            var fullPath = Path.GetFullPath(path);
            if (!string.Equals(Path.GetExtension(fullPath), ".tsv", StringComparison.OrdinalIgnoreCase))
            {
                issueList.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, 0, "Custom vehicle file must use .tsv extension."));
                return false;
            }

            if (!TryParseSections(fullPath, issueList, out var sections))
                return false;

            return TryBuild(fullPath, sections, issueList, out data);
        }

        private static bool TryParseSections(string fullPath, List<VehicleTsvIssue> issues, out Dictionary<string, Section> sections)
        {
            sections = new Dictionary<string, Section>(StringComparer.OrdinalIgnoreCase);
            string? currentSection = null;
            var lineNo = 0;

            foreach (var raw in File.ReadLines(fullPath))
            {
                lineNo++;
                var line = StripInlineComment(raw).Trim();
                if (line.Length == 0)
                    continue;

                if (TryParseSectionHeader(line, out var sectionName))
                {
                    if (!s_allowedKeys.ContainsKey(sectionName))
                    {
                        issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, lineNo, $"Unknown section [{sectionName}]."));
                        currentSection = null;
                        continue;
                    }

                    if (sections.ContainsKey(sectionName))
                    {
                        issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, lineNo, $"Duplicate section [{sectionName}] is not allowed."));
                        currentSection = null;
                        continue;
                    }

                    sections[sectionName] = new Section(sectionName, lineNo);
                    currentSection = sectionName;
                    continue;
                }

                if (!TryParseKeyValue(line, out var rawKey, out var rawValue))
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, lineNo, "Invalid line. Expected [section] or key=value."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(currentSection))
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, lineNo, $"Top-level key '{rawKey.Trim()}' is not supported."));
                    continue;
                }

                var key = NormalizeKey(rawKey);
                if (!IsAllowedKey(currentSection!, key))
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, lineNo, $"Unknown key '{key}' in section [{currentSection}]."));
                    continue;
                }

                var section = sections[currentSection!];
                if (section.Entries.ContainsKey(key))
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, lineNo, $"Duplicate key '{key}' in section [{currentSection}] is not allowed."));
                    continue;
                }

                section.Entries[key] = new Entry(rawValue.Trim(), lineNo);
            }

            for (var i = 0; i < s_requiredSections.Length; i++)
            {
                var required = s_requiredSections[i];
                if (!sections.ContainsKey(required))
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, 0, $"Missing required section [{required}]."));
            }

            return !HasErrors(issues);
        }

        private static Dictionary<string, HashSet<string>> BuildAllowedKeys()
        {
            return new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["meta"] = Set("name", "version", "description"),
                ["sounds"] = Set("engine", "start", "horn", "throttle", "crash", "brake", "backfire", "idle_freq", "top_freq", "shift_freq"),
                ["general"] = Set("surface_traction_factor", "deceleration", "max_speed", "has_wipers"),
                ["engine"] = Set(
                    "idle_rpm", "max_rpm", "rev_limiter", "auto_shift_rpm", "engine_braking", "mass_kg", "drivetrain_efficiency",
                    "drag_coefficient", "frontal_area", "rolling_resistance", "launch_rpm"),
                ["torque"] = Set(
                    "engine_braking_torque", "peak_torque", "peak_torque_rpm", "idle_torque", "redline_torque",
                    "power_factor", "engine_inertia_kgm2", "engine_friction_torque_nm", "driveline_coupling_rate"),
                ["torque_curve"] = Set("preset"),
                ["drivetrain"] = Set("final_drive", "reverse_max_speed", "reverse_power_factor", "reverse_gear_ratio", "brake_strength"),
                ["gears"] = Set("number_of_gears", "gear_ratios"),
                ["steering"] = Set("steering_response", "wheelbase", "max_steer_deg", "high_speed_stability", "high_speed_steer_gain", "high_speed_steer_start_kph", "high_speed_steer_full_kph"),
                ["tire_model"] = Set("tire_grip", "lateral_grip", "combined_grip_penalty", "slip_angle_peak_deg", "slip_angle_falloff", "turn_response", "mass_sensitivity", "downforce_grip_gain"),
                ["dynamics"] = Set("corner_stiffness_front", "corner_stiffness_rear", "yaw_inertia_scale", "steering_curve", "transient_damping"),
                ["dimensions"] = Set("vehicle_width", "vehicle_length"),
                ["tires"] = Set("tire_circumference", "tire_width", "tire_aspect", "tire_rim"),
                ["policy"] = Set(
                    "top_speed_gear",
                    "allow_overdrive_above_game_top_speed",
                    "base_auto_shift_cooldown",
                    "upshift_delay_default",
                    "auto_upshift_rpm_fraction",
                    "auto_upshift_rpm",
                    "auto_downshift_rpm_fraction",
                    "auto_downshift_rpm",
                    "top_speed_pursuit_speed_fraction",
                    "upshift_hysteresis",
                    "min_upshift_net_accel_mps2",
                    "prefer_intended_top_speed_gear_near_limit")
            };
        }

        private static HashSet<string> Set(params string[] values) => new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);

        private static bool IsAllowedKey(string section, string key)
        {
            if (s_allowedKeys.TryGetValue(section, out var keys) && keys.Contains(key))
                return true;

            if (!string.Equals(section, "policy", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(section, "torque_curve", StringComparison.OrdinalIgnoreCase))
                    return false;

                return key.EndsWith("rpm", StringComparison.OrdinalIgnoreCase);
            }

            if (key.StartsWith("upshift_delay_", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}
