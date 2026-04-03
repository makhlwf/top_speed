using System;
using System.IO;
using System.Linq;
using System.Text;
using TopSpeed.Vehicles.Parsing;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class VehicleParserStrictTests
    {
        [Fact]
        public void TryLoadFromFile_MissingTorqueCurveSection_ReturnsError()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(includeTorqueCurveSection: false));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("Missing required section [torque_curve]", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_GearRatioCountMismatch_ReturnsError()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(numberOfGears: 5, gearRatios: "3.6,2.1,1.4,1.0,0.84,0.72"));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("gear_ratios count", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_PrimaryTypeMustExistInSupportedTypes()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "atc",
                includeAtcSection: true));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("Primary transmission type", allText, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("supported transmission types", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_OnlyOneAutomaticFamilyAllowed()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                primaryType: "atc",
                supportedTypes: "atc,dct",
                includeAtcSection: true,
                includeDctSection: true));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("Only one automatic transmission family is allowed", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_MissingRequiredCvtSection_ReturnsError()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                primaryType: "cvt",
                supportedTypes: "cvt",
                includeAtcSection: false,
                includeCvtSection: false));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("Missing required section [transmission_cvt]", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_CvtRatioBounds_AreValidated()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                primaryType: "cvt",
                supportedTypes: "cvt",
                includeAtcSection: false,
                includeCvtSection: true,
                cvtRatioMin: 2.0f,
                cvtRatioMax: 1.2f));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("ratio_max must be greater than or equal to ratio_min", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_LegacyMovedKeys_ReturnError()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(includeLegacyMovedKeys: true));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);
                var allText = string.Join("\n", issues.Select(i => i.Message));

                Assert.False(ok);
                Assert.Contains("Unknown key 'drag_coefficient' in section [engine]", allText, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Unknown key 'engine_inertia_kgm2' in section [torque]", allText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static string WriteTempVehicle(string content)
        {
            var path = Path.Combine(Path.GetTempPath(), $"topspeed_vehicle_strict_{Guid.NewGuid():N}.tsv");
            File.WriteAllText(path, content);
            return path;
        }

        private static string BuildVehicleTsv(
            string primaryType = "atc",
            string supportedTypes = "atc",
            bool includeTorqueCurveSection = true,
            bool includeAtcSection = true,
            bool includeDctSection = false,
            bool includeCvtSection = false,
            int numberOfGears = 6,
            string gearRatios = "3.6,2.1,1.4,1.0,0.84,0.72",
            float cvtRatioMin = 0.45f,
            float cvtRatioMax = 3.40f,
            bool includeLegacyMovedKeys = false)
        {
            var sb = new StringBuilder();

            sb.AppendLine("[meta]");
            sb.AppendLine("name=Parser Strict Vehicle");
            sb.AppendLine("version=1");
            sb.AppendLine("description=Parser strict validation");
            sb.AppendLine();

            sb.AppendLine("[sounds]");
            sb.AppendLine("engine=builtin/engine.ogg");
            sb.AppendLine("start=builtin/start.ogg");
            sb.AppendLine("horn=builtin/horn.ogg");
            sb.AppendLine("crash=builtin/crash.ogg");
            sb.AppendLine("brake=builtin/brake.ogg");
            sb.AppendLine("idle_freq=400");
            sb.AppendLine("top_freq=2200");
            sb.AppendLine("shift_freq=1200");
            sb.AppendLine();

            sb.AppendLine("[general]");
            sb.AppendLine("surface_traction_factor=1");
            sb.AppendLine("deceleration=0.1");
            sb.AppendLine("max_speed=180");
            sb.AppendLine("has_wipers=0");
            sb.AppendLine();

            sb.AppendLine("[engine]");
            sb.AppendLine("idle_rpm=700");
            sb.AppendLine("max_rpm=7000");
            sb.AppendLine("rev_limiter=6500");
            sb.AppendLine("auto_shift_rpm=0");
            sb.AppendLine("engine_braking=0.3");
            sb.AppendLine("mass_kg=1500");
            sb.AppendLine("drivetrain_efficiency=0.85");
            sb.AppendLine("launch_rpm=1800");
            if (includeLegacyMovedKeys)
            {
                sb.AppendLine("drag_coefficient=0.30");
                sb.AppendLine("frontal_area=2.2");
                sb.AppendLine("rolling_resistance=0.015");
            }
            sb.AppendLine();

            sb.AppendLine("[torque]");
            sb.AppendLine("engine_braking_torque=150");
            sb.AppendLine("peak_torque=280");
            sb.AppendLine("peak_torque_rpm=3500");
            sb.AppendLine("idle_torque=120");
            sb.AppendLine("redline_torque=180");
            sb.AppendLine("power_factor=0.5");
            if (includeLegacyMovedKeys)
            {
                sb.AppendLine("engine_inertia_kgm2=0.24");
                sb.AppendLine("engine_friction_torque_nm=20");
                sb.AppendLine("driveline_coupling_rate=12");
            }
            sb.AppendLine();

            sb.AppendLine("[engine_rot]");
            sb.AppendLine("inertia_kgm2=0.24");
            sb.AppendLine("coupling_rate=12");
            sb.AppendLine("friction_base_nm=20");
            sb.AppendLine("friction_linear_nm_per_krpm=6");
            sb.AppendLine("friction_quadratic_nm_per_krpm2=0.4");
            sb.AppendLine("idle_control_window_rpm=150");
            sb.AppendLine("idle_control_gain_nm_per_rpm=0.08");
            sb.AppendLine("min_coupled_rise_idle_rpm_per_s=2200");
            sb.AppendLine("min_coupled_rise_full_rpm_per_s=6200");
            sb.AppendLine("overrun_idle_fraction=0.25");
            sb.AppendLine("overrun_curve_exponent=1.35");
            sb.AppendLine("brake_transfer_efficiency=0.64");
            sb.AppendLine();

            sb.AppendLine("[resistance]");
            sb.AppendLine("drag_coefficient=0.30");
            sb.AppendLine("frontal_area=2.2");
            sb.AppendLine("rolling_resistance=0.015");
            sb.AppendLine("coast_base_mps2=1.9");
            sb.AppendLine("coast_linear_per_mps=0.22");
            sb.AppendLine();

            if (includeTorqueCurveSection)
            {
                sb.AppendLine("[torque_curve]");
                sb.AppendLine("1000rpm=120");
                sb.AppendLine("3000rpm=280");
                sb.AppendLine("6000rpm=180");
                sb.AppendLine();
            }

            sb.AppendLine("[transmission]");
            sb.AppendLine($"primary_type={primaryType}");
            sb.AppendLine($"supported_types={supportedTypes}");
            sb.AppendLine("shift_on_demand=0");
            sb.AppendLine();

            if (includeAtcSection)
            {
                sb.AppendLine("[transmission_atc]");
                sb.AppendLine("creep_accel_kphps=0.7");
                sb.AppendLine("launch_coupling_min=0.2");
                sb.AppendLine("launch_coupling_max=0.9");
                sb.AppendLine("lock_speed_kph=30");
                sb.AppendLine("lock_throttle_min=0.2");
                sb.AppendLine("shift_release_coupling=0.5");
                sb.AppendLine("engage_rate=12");
                sb.AppendLine("disengage_rate=18");
                sb.AppendLine();
            }

            if (includeDctSection)
            {
                sb.AppendLine("[transmission_dct]");
                sb.AppendLine("launch_coupling_min=0.2");
                sb.AppendLine("launch_coupling_max=0.9");
                sb.AppendLine("lock_speed_kph=30");
                sb.AppendLine("lock_throttle_min=0.2");
                sb.AppendLine("shift_overlap_coupling=0.4");
                sb.AppendLine("engage_rate=12");
                sb.AppendLine("disengage_rate=18");
                sb.AppendLine();
            }

            if (includeCvtSection)
            {
                sb.AppendLine("[transmission_cvt]");
                sb.AppendLine($"ratio_min={cvtRatioMin.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                sb.AppendLine($"ratio_max={cvtRatioMax.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                sb.AppendLine("target_rpm_low=1700");
                sb.AppendLine("target_rpm_high=4200");
                sb.AppendLine("ratio_change_rate=4.5");
                sb.AppendLine("launch_coupling_min=0.24");
                sb.AppendLine("launch_coupling_max=0.85");
                sb.AppendLine("lock_speed_kph=24");
                sb.AppendLine("lock_throttle_min=0.12");
                sb.AppendLine("creep_accel_kphps=0.7");
                sb.AppendLine("shift_hold_coupling=0.75");
                sb.AppendLine("engage_rate=4.5");
                sb.AppendLine("disengage_rate=8.5");
                sb.AppendLine();
            }

            sb.AppendLine("[drivetrain]");
            sb.AppendLine("final_drive=3.5");
            sb.AppendLine("reverse_max_speed=35");
            sb.AppendLine("reverse_power_factor=0.55");
            sb.AppendLine("reverse_gear_ratio=3.2");
            sb.AppendLine("brake_strength=1.0");
            sb.AppendLine();

            sb.AppendLine("[gears]");
            sb.AppendLine($"number_of_gears={numberOfGears}");
            sb.AppendLine($"gear_ratios={gearRatios}");
            sb.AppendLine();

            sb.AppendLine("[steering]");
            sb.AppendLine("steering_response=1.0");
            sb.AppendLine("wheelbase=2.7");
            sb.AppendLine("max_steer_deg=35");
            sb.AppendLine("high_speed_stability=0.1");
            sb.AppendLine("high_speed_steer_gain=1.05");
            sb.AppendLine("high_speed_steer_start_kph=120");
            sb.AppendLine("high_speed_steer_full_kph=220");
            sb.AppendLine();

            sb.AppendLine("[tire_model]");
            sb.AppendLine("tire_grip=1.0");
            sb.AppendLine("lateral_grip=1.0");
            sb.AppendLine("combined_grip_penalty=0.72");
            sb.AppendLine("slip_angle_peak_deg=8");
            sb.AppendLine("slip_angle_falloff=1.25");
            sb.AppendLine("turn_response=1.0");
            sb.AppendLine("mass_sensitivity=0.75");
            sb.AppendLine("downforce_grip_gain=0.05");
            sb.AppendLine();

            sb.AppendLine("[dynamics]");
            sb.AppendLine("corner_stiffness_front=1.0");
            sb.AppendLine("corner_stiffness_rear=1.0");
            sb.AppendLine("yaw_inertia_scale=1.0");
            sb.AppendLine("steering_curve=1.0");
            sb.AppendLine("transient_damping=1.0");
            sb.AppendLine();

            sb.AppendLine("[dimensions]");
            sb.AppendLine("vehicle_width=1.8");
            sb.AppendLine("vehicle_length=4.5");
            sb.AppendLine();

            sb.AppendLine("[tires]");
            sb.AppendLine("tire_circumference=2.0");

            return sb.ToString();
        }
    }
}

