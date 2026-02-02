using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Tracks;

namespace TopSpeed.Vehicles
{
    internal static class VehicleLoader
    {
        private const string BuiltinPrefix = "builtin";
        private const string DefaultVehicleFolder = "default";

        public static VehicleDefinition LoadOfficial(int vehicleIndex, TrackWeather weather)
        {
            if (vehicleIndex < 0 || vehicleIndex >= VehicleCatalog.VehicleCount)
                vehicleIndex = 0;

            var parameters = VehicleCatalog.Vehicles[vehicleIndex];
            var vehiclesRoot = Path.Combine(AssetPaths.SoundsRoot, "Vehicles");
            var currentVehicleFolder = $"Vehicle{vehicleIndex + 1}";

            var def = new VehicleDefinition
            {
                CarType = (CarType)vehicleIndex,
                Name = parameters.Name,
                UserDefined = false,
                SurfaceTractionFactor = parameters.SurfaceTractionFactor,
                Deceleration = parameters.Deceleration,
                TopSpeed = parameters.TopSpeed,
                IdleFreq = parameters.IdleFreq,
                TopFreq = parameters.TopFreq,
                ShiftFreq = parameters.ShiftFreq,
                Gears = parameters.Gears,
                Steering = parameters.Steering,
                SteeringFactor = parameters.SteeringFactor,
                HasWipers = parameters.HasWipers == 1 && weather == TrackWeather.Rain ? 1 : 0,
                IdleRpm = parameters.IdleRpm,
                MaxRpm = parameters.MaxRpm,
                RevLimiter = parameters.RevLimiter,
                AutoShiftRpm = parameters.AutoShiftRpm > 0f ? parameters.AutoShiftRpm : parameters.RevLimiter * 0.92f,
                EngineBraking = parameters.EngineBraking,
                MassKg = parameters.MassKg,
                DrivetrainEfficiency = parameters.DrivetrainEfficiency,
                EngineBrakingTorqueNm = parameters.EngineBrakingTorqueNm,
                TireGripCoefficient = parameters.TireGripCoefficient,
                PeakTorqueNm = parameters.PeakTorqueNm,
                PeakTorqueRpm = parameters.PeakTorqueRpm,
                IdleTorqueNm = parameters.IdleTorqueNm,
                RedlineTorqueNm = parameters.RedlineTorqueNm,
                DragCoefficient = parameters.DragCoefficient,
                FrontalAreaM2 = parameters.FrontalAreaM2,
                RollingResistanceCoefficient = parameters.RollingResistanceCoefficient,
                LaunchRpm = parameters.LaunchRpm,
                FinalDriveRatio = parameters.FinalDriveRatio,
                TireCircumferenceM = parameters.TireCircumferenceM,
                LateralGripCoefficient = parameters.LateralGripCoefficient,
                HighSpeedStability = parameters.HighSpeedStability,
                WheelbaseM = parameters.WheelbaseM,
                MaxSteerDeg = parameters.MaxSteerDeg,
                TrackWidthM = parameters.TrackWidthM,
                WidthM = parameters.WidthM,
                LengthM = parameters.LengthM,
                VehicleHeightM = parameters.VehicleHeightM,
                HornHeightM = parameters.HornHeightM,
                EngineHeightM = parameters.EngineHeightM,
                DynamicsModel = parameters.DynamicsModel,
                PowerFactor = parameters.PowerFactor,
                GearRatios = parameters.GearRatios,
                BrakeStrength = parameters.BrakeStrength,
                SteerInputRate = parameters.SteerInputRate,
                SteerReturnRate = parameters.SteerReturnRate,
                SteerGamma = parameters.SteerGamma,
                MaxSteerLowDeg = parameters.MaxSteerLowDeg,
                MaxSteerHighDeg = parameters.MaxSteerHighDeg,
                SteerSpeedKph = parameters.SteerSpeedKph,
                SteerSpeedExponent = parameters.SteerSpeedExponent,
                CorneringStiffnessFront = parameters.CorneringStiffnessFront,
                CorneringStiffnessRear = parameters.CorneringStiffnessRear,
                YawInertiaKgM2 = parameters.YawInertiaKgM2,
                CgToFrontAxleM = parameters.CgToFrontAxleM,
                CgToRearAxleM = parameters.CgToRearAxleM,
                CgHeightM = parameters.CgHeightM,
                WeightDistributionFront = parameters.WeightDistributionFront,
                BrakeBiasFront = parameters.BrakeBiasFront,
                DriveBiasFront = parameters.DriveBiasFront,
                RollStiffnessFrontFraction = parameters.RollStiffnessFrontFraction,
                TireLoadSensitivity = parameters.TireLoadSensitivity,
                DownforceCoefficient = parameters.DownforceCoefficient,
                DownforceFrontBias = parameters.DownforceFrontBias,
                LongitudinalStiffnessFront = parameters.LongitudinalStiffnessFront,
                LongitudinalStiffnessRear = parameters.LongitudinalStiffnessRear
            };

            foreach (VehicleAction action in Enum.GetValues(typeof(VehicleAction)))
            {
                var overridePath = parameters.GetSoundPath(action);
                if (!string.IsNullOrWhiteSpace(overridePath))
                {
                    def.SetSoundPath(action, Path.Combine(vehiclesRoot, overridePath!));
                }
                else
                {
                    def.SetSoundPath(action, ResolveOfficialFallback(vehiclesRoot, currentVehicleFolder, action));
                }
            }

            return def;
        }

        public static VehicleDefinition LoadCustom(string vehicleFile, TrackWeather weather)
        {
            var filePath = Path.IsPathRooted(vehicleFile)
                ? vehicleFile
                : Path.Combine(AssetPaths.Root, vehicleFile);
            var settings = ReadVehicleFile(filePath);
            var builtinRoot = Path.Combine(AssetPaths.SoundsRoot, "Vehicles");
            var customVehiclesRoot = Path.Combine(AssetPaths.Root, "Vehicles");

            var surfaceTractionFactor = ReadInt(settings, "surface_traction_factor", 10) / 100.0f;
            var deceleration = ReadInt(settings, "deceleration", 40) / 100.0f;
            var topSpeed = ReadInt(settings, "max_speed", 15000) / 100.0f;
            var idleFreq = ReadInt(settings, "idle_freq", 11000);
            var topFreq = ReadInt(settings, "top_freq", 50000);
            var shiftFreq = ReadInt(settings, "shift_freq", 40000);
            var gears = ReadInt(settings, "number_of_gears", 5);
            var steering = ReadInt(settings, "steering", 100) / 100.0f;
            var steeringFactor = ReadInt(settings, "steering_factor", 40);

            var hasWipers = 0;
            if (weather == TrackWeather.Rain)
                hasWipers = ReadInt(settings, "has_wipers", 1);

            // Engine simulation parameters
            var idleRpm = ReadFloat(settings, "idle_rpm", 800f);
            var maxRpm = ReadFloat(settings, "max_rpm", 7000f);
            var revLimiter = ReadFloat(settings, "rev_limiter", 6500f);
            var autoShiftRpm = ReadFloat(settings, "auto_shift_rpm", 0f);
            var engineBraking = ReadFloat(settings, "engine_braking", 0.3f);
            var massKg = ReadFloat(settings, "mass_kg", 1500f);
            var drivetrainEfficiency = ReadFloat(settings, "drivetrain_efficiency", 0.85f);
            var engineBrakingTorque = ReadFloat(settings, "engine_braking_torque", 150f);
            var tireGrip = ReadFloat(settings, "tire_grip", 0.9f);
            var peakTorqueNm = ReadFloat(settings, "peak_torque", 200f);
            var peakTorqueRpm = ReadFloat(settings, "peak_torque_rpm", 4000f);
            var idleTorqueNm = ReadFloat(settings, "idle_torque", peakTorqueNm * 0.3f);
            var redlineTorqueNm = ReadFloat(settings, "redline_torque", peakTorqueNm * 0.6f);
            var dragCoefficient = ReadFloat(settings, "drag_coefficient", 0.30f);
            var frontalArea = ReadFloat(settings, "frontal_area", 2.2f);
            var rollingResistance = ReadFloat(settings, "rolling_resistance", 0.015f);
            var launchRpm = ReadFloat(settings, "launch_rpm", 1800f);
            var finalDriveRatio = ReadFloat(settings, "final_drive", 3.5f);      
            var powerFactor = ReadFloat(settings, "power_factor", 0.5f);
            var gearRatios = ReadFloatArray(settings, "gear_ratios");
            var brakeStrength = ReadFloat(settings, "brake_strength", 1.0f);     
            var lateralGrip = ReadFloat(settings, "lateral_grip", 1.0f);
            var highSpeedStability = ReadFloat(settings, "high_speed_stability", 0.0f);
            var wheelbase = ReadFloat(settings, "wheelbase", 2.7f);
            var maxSteerDeg = ReadFloat(settings, "max_steer_deg", 35f);
            var trackWidthM = ReadFloat(settings, "track_width", 0f);
            var widthM = ReadFloat(settings, "vehicle_width", 1.8f);
            var lengthM = ReadFloat(settings, "vehicle_length", 4.5f);
            var vehicleHeightM = ReadFloat(settings, "vehicle_height", 0f);
            var hornHeightM = ReadFloat(settings, "horn_height", 0f);
            var engineHeightM = ReadFloat(settings, "engine_height", 0f);
            var steerInputRate = ReadFloat(settings, "steer_input_rate", 0f);
            var steerReturnRate = ReadFloat(settings, "steer_return_rate", 0f);
            var steerGamma = ReadFloat(settings, "steer_gamma", 0f);
            var maxSteerLowDeg = ReadFloat(settings, "max_steer_low_deg", 0f);
            var maxSteerHighDeg = ReadFloat(settings, "max_steer_high_deg", 0f);
            var steerSpeedKph = ReadFloat(settings, "steer_speed_kph", 0f);
            var steerSpeedExponent = ReadFloat(settings, "steer_speed_exp", 0f);
            var corneringStiffnessFront = ReadFloat(settings, "cornering_stiffness_front", 0f);
            var corneringStiffnessRear = ReadFloat(settings, "cornering_stiffness_rear", 0f);
            var yawInertiaKgM2 = ReadFloat(settings, "yaw_inertia_kg_m2", 0f);
            var cgToFrontAxleM = ReadFloat(settings, "cg_to_front_axle", 0f);
            var cgToRearAxleM = ReadFloat(settings, "cg_to_rear_axle", 0f);
            var cgHeightM = ReadFloat(settings, "cg_height", 0f);
            var weightDistributionFront = ReadFloat(settings, "weight_distribution_front", 0f);
            var brakeBiasFront = ReadFloat(settings, "brake_bias_front", 0f);
            var driveBiasFront = ReadFloat(settings, "drive_bias_front", 0f);
            var modelRaw = ReadString(settings, "dynamics_model", ReadString(settings, "vehicle_model", "fourwheel"));
            var dynamicsModel = ParseDynamicsModel(modelRaw);
            var rollStiffnessFront = ReadFloat(settings, "roll_stiffness_front", 0f);
            var tireLoadSensitivity = ReadFloat(settings, "tire_load_sensitivity", 0f);
            var downforceCoefficient = ReadFloat(settings, "downforce_coefficient", 0f);
            var downforceFrontBias = ReadFloat(settings, "downforce_front_bias", 0f);
            var longStiffnessFront = ReadFloat(settings, "longitudinal_stiffness_front", 0f);
            var longStiffnessRear = ReadFloat(settings, "longitudinal_stiffness_rear", 0f);

            var tireCircumferenceM = ReadFloat(settings, "tire_circumference", 0f);
            if (tireCircumferenceM <= 0f)
            {
                var tireWidth = ReadInt(settings, "tire_width", 0);
                var tireAspect = ReadInt(settings, "tire_aspect", 0);
                var tireRim = ReadInt(settings, "tire_rim", 0);
                if (tireWidth > 0 && tireAspect > 0 && tireRim > 0)
                    tireCircumferenceM = CalculateTireCircumferenceM(tireWidth, tireAspect, tireRim);
            }
            if (tireCircumferenceM <= 0f)
                tireCircumferenceM = 2.0f;

            var def = new VehicleDefinition
            {
                CarType = CarType.Vehicle1,
                Name = ReadString(settings, "name", Path.GetFileNameWithoutExtension(filePath)),
                UserDefined = true,
                CustomFile = Path.GetFileNameWithoutExtension(filePath),
                SurfaceTractionFactor = surfaceTractionFactor,
                Deceleration = deceleration,
                TopSpeed = topSpeed,
                IdleFreq = idleFreq,
                TopFreq = topFreq,
                ShiftFreq = shiftFreq,
                Gears = gears,
                Steering = steering,
                SteeringFactor = steeringFactor,
                HasWipers = hasWipers,
                IdleRpm = idleRpm,
                MaxRpm = maxRpm,
                RevLimiter = revLimiter,
                AutoShiftRpm = autoShiftRpm > 0f ? autoShiftRpm : revLimiter * 0.92f,
                EngineBraking = engineBraking,
                MassKg = massKg,
                DrivetrainEfficiency = drivetrainEfficiency,
                EngineBrakingTorqueNm = engineBrakingTorque,
                TireGripCoefficient = tireGrip,
                PeakTorqueNm = peakTorqueNm,
                PeakTorqueRpm = peakTorqueRpm,
                IdleTorqueNm = idleTorqueNm,
                RedlineTorqueNm = redlineTorqueNm,
                DragCoefficient = dragCoefficient,
                FrontalAreaM2 = frontalArea,
                RollingResistanceCoefficient = rollingResistance,
                LaunchRpm = launchRpm,
                FinalDriveRatio = finalDriveRatio,
                TireCircumferenceM = tireCircumferenceM,
                LateralGripCoefficient = lateralGrip,
                HighSpeedStability = highSpeedStability,
                WheelbaseM = wheelbase,
                MaxSteerDeg = maxSteerDeg,
                TrackWidthM = trackWidthM,
                WidthM = widthM,
                LengthM = lengthM,
                VehicleHeightM = vehicleHeightM,
                HornHeightM = hornHeightM,
                EngineHeightM = engineHeightM,
                DynamicsModel = dynamicsModel,
                PowerFactor = powerFactor,
                GearRatios = gearRatios,
                BrakeStrength = brakeStrength,
                SteerInputRate = steerInputRate,
                SteerReturnRate = steerReturnRate,
                SteerGamma = steerGamma,
                MaxSteerLowDeg = maxSteerLowDeg,
                MaxSteerHighDeg = maxSteerHighDeg,
                SteerSpeedKph = steerSpeedKph,
                SteerSpeedExponent = steerSpeedExponent,
                CorneringStiffnessFront = corneringStiffnessFront,
                CorneringStiffnessRear = corneringStiffnessRear,
                YawInertiaKgM2 = yawInertiaKgM2,
                CgToFrontAxleM = cgToFrontAxleM,
                CgToRearAxleM = cgToRearAxleM,
                CgHeightM = cgHeightM,
                WeightDistributionFront = weightDistributionFront,
                BrakeBiasFront = brakeBiasFront,
                DriveBiasFront = driveBiasFront,
                RollStiffnessFrontFraction = rollStiffnessFront,
                TireLoadSensitivity = tireLoadSensitivity,
                DownforceCoefficient = downforceCoefficient,
                DownforceFrontBias = downforceFrontBias,
                LongitudinalStiffnessFront = longStiffnessFront,
                LongitudinalStiffnessRear = longStiffnessRear
            };

            def.SetSoundPath(VehicleAction.Engine, ResolveSound(ReadString(settings, "engine_sound", "engine.wav"), builtinRoot, customVehiclesRoot, p => p.GetSoundPath(VehicleAction.Engine)));
            def.SetSoundPath(VehicleAction.Start, ResolveSound(ReadString(settings, "start_sound", "start.wav"), builtinRoot, customVehiclesRoot, p => p.GetSoundPath(VehicleAction.Start)));
            def.SetSoundPath(VehicleAction.Horn, ResolveSound(ReadString(settings, "horn_sound", "horn.wav"), builtinRoot, customVehiclesRoot, p => p.GetSoundPath(VehicleAction.Horn)));
            def.SetSoundPath(VehicleAction.Throttle, ResolveSound(ReadString(settings, "throttle_sound", "throttle.wav"), builtinRoot, customVehiclesRoot, p => p.GetSoundPath(VehicleAction.Throttle)));
            def.SetSoundPath(VehicleAction.Crash, ResolveSound(ReadString(settings, "crash_sound", "crash.wav"), builtinRoot, customVehiclesRoot, p => p.GetSoundPath(VehicleAction.Crash)));
            def.SetSoundPath(VehicleAction.CrashMono, ResolveSound(ReadString(settings, "mono_crash_sound", "crash_mono.wav"), builtinRoot, customVehiclesRoot, p => p.GetSoundPath(VehicleAction.CrashMono)));
            def.SetSoundPath(VehicleAction.Brake, ResolveSound(ReadString(settings, "brake_sound", "brake.wav"), builtinRoot, customVehiclesRoot, p => p.GetSoundPath(VehicleAction.Brake)));
            def.SetSoundPath(VehicleAction.Backfire, ResolveSound(ReadString(settings, "backfire_sound", "backfire.wav"), builtinRoot, customVehiclesRoot, p => p.GetSoundPath(VehicleAction.Backfire)));

            return def;
        }

        private static string? ResolveOfficialFallback(string root, string vehicleFolder, VehicleAction action)
        {
            var fileName = GetDefaultFileName(action);
            var primaryPath = Path.GetFullPath(Path.Combine(root, vehicleFolder, fileName));
            if (File.Exists(primaryPath))
                return primaryPath;

            // Only fallback to 'default' folder for non-optional sounds
            // Throttle and Backfire are vehicle-specific features
            if (action == VehicleAction.Backfire || action == VehicleAction.Throttle)
                return null;

            var fallbackPath = Path.GetFullPath(Path.Combine(root, DefaultVehicleFolder, fileName));
            if (File.Exists(fallbackPath))
                return fallbackPath;

            return null;
        }

        private static string GetDefaultFileName(VehicleAction action)
        {
            switch (action)
            {
                case VehicleAction.Engine: return "engine.wav";
                case VehicleAction.Start: return "start.wav";
                case VehicleAction.Horn: return "horn.wav";
                case VehicleAction.Throttle: return "throttle.wav";
                case VehicleAction.Crash: return "crash.wav";
                case VehicleAction.CrashMono: return "crash_mono.wav";
                case VehicleAction.Brake: return "brake.wav";
                case VehicleAction.Backfire: return "backfire.wav";
                default: throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        private static string? ResolveSound(string? value, string builtinRoot, string customVehiclesRoot, Func<VehicleParameters, string?> selector)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (value!.StartsWith(BuiltinPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(value.Substring(BuiltinPrefix.Length), out var index))
                    return null;
                index -= 1;
                if (index < 0 || index >= VehicleCatalog.VehicleCount)
                    return null;
                var parameters = VehicleCatalog.Vehicles[index];
                var file = selector(parameters);
                
                // If it's a builtin reference, we should still handle official fallbacks if the catalog doesn't provide a path
                if (string.IsNullOrWhiteSpace(file))
                {
                    return ResolveOfficialFallback(builtinRoot, $"Vehicle{index + 1}", GetActionFromSelector(selector));
                }

                return Path.Combine(builtinRoot, file!);
            }

            return Path.IsPathRooted(value) ? value : Path.Combine(customVehiclesRoot, value);
        }

        private static VehicleAction GetActionFromSelector(Func<VehicleParameters, string?> selector)
        {
            // Simple hack to detect the action from the selector if needed for builtin resolution
            // In a production environment, we'd pass the action explicitly.
            var testParams = new VehicleParameters(
                "Test", "e", "s", "h", "t", "c", "cm", "b", "f",
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                idleRpm: 0, maxRpm: 0, revLimiter: 0, autoShiftRpm: 0, engineBraking: 0,
                massKg: 0, drivetrainEfficiency: 0, engineBrakingTorqueNm: 0, tireGripCoefficient: 0,
                finalDriveRatio: 0, tireCircumferenceM: 0, powerFactor: 0, gearRatios: null, brakeStrength: 0);
            var result = selector(testParams);
            switch (result)
            {
                case "e": return VehicleAction.Engine;
                case "s": return VehicleAction.Start;
                case "h": return VehicleAction.Horn;
                case "t": return VehicleAction.Throttle;
                case "c": return VehicleAction.Crash;
                case "cm": return VehicleAction.CrashMono;
                case "b": return VehicleAction.Brake;
                case "f": return VehicleAction.Backfire;
                default: return VehicleAction.Engine;
            }
        }

        private static Dictionary<string, string> ReadVehicleFile(string filePath)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(filePath))
                return result;

            foreach (var line in File.ReadLines(filePath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                    continue;
                if (trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;
                var idx = trimmed.IndexOf('=');
                if (idx <= 0)
                    continue;
                var key = trimmed.Substring(0, idx).Trim();
                var value = trimmed.Substring(idx + 1).Trim();
                result[key] = value;
            }
            return result;
        }

        private static int ReadInt(Dictionary<string, string> values, string key, int defaultValue)
        {
            if (values.TryGetValue(key, out var raw) && int.TryParse(raw, out var value))
                return value;
            return defaultValue;
        }

        private static string ReadString(Dictionary<string, string> values, string key, string defaultValue)
        {
            if (values.TryGetValue(key, out var raw))
                return raw;
            return defaultValue;
        }

        private static float ReadFloat(Dictionary<string, string> values, string key, float defaultValue)
        {
            if (values.TryGetValue(key, out var raw) && float.TryParse(raw, out var value))
                return value;
            return defaultValue;
        }

        private static float CalculateTireCircumferenceM(int widthMm, int aspectPercent, int rimInches)
        {
            var sidewallMm = widthMm * (aspectPercent / 100f);
            var diameterMm = (rimInches * 25.4f) + (2f * sidewallMm);
            return (float)(Math.PI * (diameterMm / 1000f));
        }

        private static float[]? ReadFloatArray(Dictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
                return null;

            var parts = raw.Split(',');
            var result = new System.Collections.Generic.List<float>();
            foreach (var part in parts)
            {
                if (float.TryParse(part.Trim(), out var value))
                    result.Add(value);
            }
            return result.Count > 0 ? result.ToArray() : null;
        }

        private static VehicleDynamicsModel ParseDynamicsModel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return VehicleDynamicsModel.FourWheel;

            if (int.TryParse(value.Trim(), out var numeric))
                return numeric == 1 ? VehicleDynamicsModel.Bicycle : VehicleDynamicsModel.FourWheel;

            var key = value.Trim().ToLowerInvariant();
            switch (key)
            {
                case "bicycle":
                case "bike":
                case "motorcycle":
                case "motorbike":
                case "2wheel":
                case "2-wheel":
                case "two-wheel":
                case "twowheel":
                    return VehicleDynamicsModel.Bicycle;
                case "fourwheel":
                case "four-wheel":
                case "4wheel":
                case "4-wheel":
                case "car":
                default:
                    return VehicleDynamicsModel.FourWheel;
            }
        }
    }
}


