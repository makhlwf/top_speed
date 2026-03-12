using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
    {
        private static bool TryBuild(string fullPath, Dictionary<string, Section> sections, List<VehicleTsvIssue> issues, out CustomVehicleTsvData data)
        {
            data = null!;

            var meta = sections["meta"];
            var sounds = sections["sounds"];
            var general = sections["general"];
            var engine = sections["engine"];
            var drivetrain = sections["drivetrain"];
            var gears = sections["gears"];
            var handling = sections["handling"];
            var dimensions = sections["dimensions"];
            var tires = sections["tires"];
            sections.TryGetValue("policy", out var policy);

            var metaName = RequireString(meta, "name", issues);
            var metaVersion = RequireString(meta, "version", issues);
            var metaDescription = RequireString(meta, "description", issues);

            var engineSound = RequireString(sounds, "engine", issues);
            var startSound = RequireString(sounds, "start", issues);
            var hornSound = RequireString(sounds, "horn", issues);
            var throttleSound = OptionalString(sounds, "throttle");
            var crashVariants = RequireCsvStrings(sounds, "crash", issues);
            var brakeSound = RequireString(sounds, "brake", issues);
            var backfireVariants = OptionalCsvStrings(sounds, "backfire");

            var idleFreq = RequireIntRange(sounds, "idle_freq", 100, 200000, issues);
            var topFreq = RequireIntRange(sounds, "top_freq", 100, 200000, issues);
            var shiftFreq = RequireIntRange(sounds, "shift_freq", 100, 200000, issues);

            var surfaceTractionFactor = RequireFloatRange(general, "surface_traction_factor", 0f, 5f, issues);
            var deceleration = RequireFloatRange(general, "deceleration", 0f, 5f, issues);
            var topSpeed = RequireFloatRange(general, "max_speed", 10f, 500f, issues);
            var hasWipers = RequireBoolInt(general, "has_wipers", issues);

            var gearCount = RequireIntRange(gears, "number_of_gears", 1, 10, issues);
            var gearRatios = RequireFloatCsv(gears, "gear_ratios", issues);

            var idleRpm = RequireFloatRange(engine, "idle_rpm", 300f, 3000f, issues);
            var maxRpm = RequireFloatRange(engine, "max_rpm", 1000f, 20000f, issues);
            var revLimiter = RequireFloatRange(engine, "rev_limiter", 800f, 18000f, issues);
            var autoShiftRpm = RequireFloatRange(engine, "auto_shift_rpm", 0f, 18000f, issues);
            var engineBraking = RequireFloatRange(engine, "engine_braking", 0f, 1.5f, issues);
            var massKg = RequireFloatRange(engine, "mass_kg", 20f, 10000f, issues);
            var drivetrainEfficiency = RequireFloatRange(engine, "drivetrain_efficiency", 0.1f, 1.0f, issues);
            var engineBrakingTorque = RequireFloatRange(engine, "engine_braking_torque", 0f, 3000f, issues);
            var peakTorque = RequireFloatRange(engine, "peak_torque", 10f, 3000f, issues);
            var peakTorqueRpm = RequireFloatRange(engine, "peak_torque_rpm", 500f, 18000f, issues);
            var idleTorque = RequireFloatRange(engine, "idle_torque", 0f, 3000f, issues);
            var redlineTorque = RequireFloatRange(engine, "redline_torque", 0f, 3000f, issues);
            var dragCoefficient = RequireFloatRange(engine, "drag_coefficient", 0.01f, 1.5f, issues);
            var frontalArea = RequireFloatRange(engine, "frontal_area", 0.05f, 10f, issues);
            var rollingResistance = RequireFloatRange(engine, "rolling_resistance", 0.001f, 0.1f, issues);
            var launchRpm = RequireFloatRange(engine, "launch_rpm", 0f, 18000f, issues);
            var powerFactor = RequireFloatRange(engine, "power_factor", 0.05f, 2f, issues);

            var finalDrive = RequireFloatRange(drivetrain, "final_drive", 0.5f, 8f, issues);
            var reverseMaxSpeed = RequireFloatRange(drivetrain, "reverse_max_speed", 1f, 100f, issues);
            var reversePowerFactor = RequireFloatRange(drivetrain, "reverse_power_factor", 0.05f, 2f, issues);
            var reverseGearRatio = RequireFloatRange(drivetrain, "reverse_gear_ratio", 0.5f, 8f, issues);
            var brakeStrength = RequireFloatRange(drivetrain, "brake_strength", 0.1f, 5f, issues);

            var steering = RequireFloatRange(handling, "steering", 0.1f, 5f, issues);
            var tireGrip = RequireFloatRange(handling, "tire_grip", 0.1f, 3f, issues);
            var lateralGrip = RequireFloatRange(handling, "lateral_grip", 0.1f, 3f, issues);
            var highSpeedStability = RequireFloatRange(handling, "high_speed_stability", 0f, 1f, issues);
            var wheelbase = RequireFloatRange(handling, "wheelbase", 0.3f, 8f, issues);
            var maxSteerDeg = RequireFloatRange(handling, "max_steer_deg", 5f, 60f, issues);

            var widthM = RequireFloatRange(dimensions, "vehicle_width", 0.2f, 5f, issues);
            var lengthM = RequireFloatRange(dimensions, "vehicle_length", 0.3f, 20f, issues);

            var tireCircumference = OptionalFloat(tires, "tire_circumference", issues);
            var tireWidth = OptionalInt(tires, "tire_width", issues);
            var tireAspect = OptionalInt(tires, "tire_aspect", issues);
            var tireRim = OptionalInt(tires, "tire_rim", issues);

            if (gearRatios != null)
            {
                if (gearRatios.Count != gearCount)
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, gears.Entries["gear_ratios"].Line,
                        $"gear_ratios count ({gearRatios.Count}) must match number_of_gears ({gearCount})."));
                }
                else
                {
                    for (var i = 0; i < gearRatios.Count; i++)
                    {
                        var value = gearRatios[i];
                        if (value < 0.20f || value > 8.00f)
                            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, gears.Entries["gear_ratios"].Line, $"gear_ratios[{i + 1}] is outside allowed range 0.20 to 8.00."));
                    }
                    for (var i = 1; i < gearRatios.Count; i++)
                    {
                        if (gearRatios[i] > gearRatios[i - 1] + 0.0001f)
                        {
                            issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, gears.Entries["gear_ratios"].Line, "gear_ratios must be non-increasing from gear 1 to last gear."));
                            break;
                        }
                    }
                }
            }

            if (maxRpm < idleRpm)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, engine.Entries["max_rpm"].Line, "max_rpm must be greater than or equal to idle_rpm."));
            if (revLimiter < idleRpm || revLimiter > maxRpm)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, engine.Entries["rev_limiter"].Line, "rev_limiter must be between idle_rpm and max_rpm."));
            if (autoShiftRpm > 0f && (autoShiftRpm < idleRpm || autoShiftRpm > revLimiter))
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, engine.Entries["auto_shift_rpm"].Line, "auto_shift_rpm must be 0 or between idle_rpm and rev_limiter."));
            if (peakTorqueRpm < idleRpm || peakTorqueRpm > revLimiter)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, engine.Entries["peak_torque_rpm"].Line, "peak_torque_rpm must be between idle_rpm and rev_limiter."));
            if (launchRpm > revLimiter)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, engine.Entries["launch_rpm"].Line, "launch_rpm must not exceed rev_limiter."));
            if (topFreq < idleFreq)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, sounds.Entries["top_freq"].Line, "top_freq must be greater than or equal to idle_freq."));
            if (shiftFreq < idleFreq || shiftFreq > topFreq)
                issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, sounds.Entries["shift_freq"].Line, "shift_freq must be between idle_freq and top_freq."));

            float tireCircumferenceResolved = 0f;
            if (tireCircumference.HasValue && tireCircumference.Value > 0f)
            {
                if (tireCircumference.Value < 0.2f || tireCircumference.Value > 5f)
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Entries["tire_circumference"].Line, "tire_circumference must be between 0.2 and 5.0 meters."));
                else
                    tireCircumferenceResolved = tireCircumference.Value;
            }
            else
            {
                if (!tireWidth.HasValue || !tireAspect.HasValue || !tireRim.HasValue)
                {
                    issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Line, "Provide tire_circumference or all of tire_width, tire_aspect, and tire_rim."));
                }
                else
                {
                    if (tireWidth.Value < 20 || tireWidth.Value > 450)
                        issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Entries["tire_width"].Line, "tire_width must be between 20 and 450 mm."));
                    if (tireAspect.Value < 5 || tireAspect.Value > 150)
                        issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Entries["tire_aspect"].Line, "tire_aspect must be between 5 and 150."));
                    if (tireRim.Value < 4 || tireRim.Value > 30)
                        issues.Add(new VehicleTsvIssue(VehicleTsvIssueSeverity.Error, tires.Entries["tire_rim"].Line, "tire_rim must be between 4 and 30 inches."));
                    if (!HasErrors(issues))
                        tireCircumferenceResolved = CalculateTireCircumferenceM(tireWidth.Value, tireAspect.Value, tireRim.Value);
                }
            }

            if (!ValidatePolicy(policy, gearCount, idleRpm, revLimiter, issues))
                return false;

            if (HasErrors(issues))
                return false;

            data = new CustomVehicleTsvData
            {
                SourcePath = fullPath,
                SourceDirectory = Path.GetDirectoryName(fullPath) ?? string.Empty,
                Meta = new CustomVehicleMeta(metaName, metaVersion, metaDescription),
                Sounds = new CustomVehicleSounds
                {
                    Engine = engineSound,
                    Start = startSound,
                    Horn = hornSound,
                    Throttle = throttleSound,
                    CrashVariants = crashVariants,
                    Brake = brakeSound,
                    BackfireVariants = backfireVariants
                },
                SurfaceTractionFactor = surfaceTractionFactor,
                Deceleration = deceleration,
                TopSpeed = topSpeed,
                HasWipers = hasWipers ? 1 : 0,
                IdleFreq = idleFreq,
                TopFreq = topFreq,
                ShiftFreq = shiftFreq,
                Gears = gearCount,
                GearRatios = gearRatios!.ToArray(),
                IdleRpm = idleRpm,
                MaxRpm = maxRpm,
                RevLimiter = revLimiter,
                AutoShiftRpm = autoShiftRpm,
                EngineBraking = engineBraking,
                MassKg = massKg,
                DrivetrainEfficiency = drivetrainEfficiency,
                EngineBrakingTorqueNm = engineBrakingTorque,
                PeakTorqueNm = peakTorque,
                PeakTorqueRpm = peakTorqueRpm,
                IdleTorqueNm = idleTorque,
                RedlineTorqueNm = redlineTorque,
                DragCoefficient = dragCoefficient,
                FrontalAreaM2 = frontalArea,
                RollingResistanceCoefficient = rollingResistance,
                LaunchRpm = launchRpm,
                PowerFactor = powerFactor,
                FinalDriveRatio = finalDrive,
                ReverseMaxSpeedKph = reverseMaxSpeed,
                ReversePowerFactor = reversePowerFactor,
                ReverseGearRatio = reverseGearRatio,
                BrakeStrength = brakeStrength,
                Steering = steering,
                TireGripCoefficient = tireGrip,
                LateralGripCoefficient = lateralGrip,
                HighSpeedStability = highSpeedStability,
                WheelbaseM = wheelbase,
                MaxSteerDeg = maxSteerDeg,
                WidthM = widthM,
                LengthM = lengthM,
                TireCircumferenceM = tireCircumferenceResolved,
                TransmissionPolicy = BuildTransmissionPolicy(policy, gearCount, idleRpm, revLimiter, autoShiftRpm)
            };

            return true;
        }

        private static float ReadFloat(Dictionary<string, string> values, string key, float defaultValue)
        {
            if (values.TryGetValue(key, out var raw) && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return value;
            return defaultValue;
        }

        private static int ReadInt(Dictionary<string, string> values, string key, int defaultValue)
        {
            if (values.TryGetValue(key, out var raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return value;
            return defaultValue;
        }

        private static bool ReadBool(Dictionary<string, string> values, string key, bool defaultValue)
        {
            if (!values.TryGetValue(key, out var raw))
                return defaultValue;
            if (bool.TryParse(raw, out var b))
                return b;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                return i != 0;
            return defaultValue;
        }
    }
}
