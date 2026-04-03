using System;
using System.IO;
using System.Linq;
using TopSpeed.Vehicles.Parsing;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class VehicleParserTests
    {
        [Fact]
        public void TryLoadFromFile_ShiftOnDemandWithoutAutomatic_AddsWarning()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: true,
                includeAtcSection: false));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);

                Assert.True(ok);
                var warningText = string.Join("\n", issues
                    .Where(issue => issue.Severity == VehicleTsvIssueSeverity.Warning)
                    .Select(issue => issue.Message));
                Assert.Contains("shift_on_demand is ignored", warningText, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(VehicleTsvIssueSeverity.Error, issues.Select(issue => issue.Severity));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_UnusedTransmissionSection_AddsWarning()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: false,
                includeAtcSection: true));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var _, out var issues);

                Assert.True(ok);
                var warningText = string.Join("\n", issues
                    .Where(issue => issue.Severity == VehicleTsvIssueSeverity.Warning)
                    .Select(issue => issue.Message));
                Assert.Contains("Section [transmission_atc] is unused", warningText, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(VehicleTsvIssueSeverity.Error, issues.Select(issue => issue.Severity));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_ResistanceAndRotationKeys_AreParsed()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: false,
                includeAtcSection: false));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var data, out var issues);

                Assert.True(ok);
                Assert.DoesNotContain(VehicleTsvIssueSeverity.Error, issues.Select(issue => issue.Severity));
                Assert.Equal(1.9f, data.CoastDragBaseMps2, 3);
                Assert.Equal(0.22f, data.CoastDragLinearPerMps, 3);
                Assert.Equal(0.25f, data.EngineOverrunIdleLossFraction, 3);
                Assert.Equal(1.35f, data.OverrunCurveExponent, 3);
                Assert.Equal(0.64f, data.EngineBrakeTransferEfficiency, 3);
                Assert.Equal(6f, data.EngineFrictionLinearNmPerKrpm, 3);
                Assert.Equal(0.4f, data.EngineFrictionQuadraticNmPerKrpm2, 3);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_StopSound_WhenProvided_IsParsed()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: false,
                includeAtcSection: false,
                stopSound: "stop.wav"));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var data, out var issues);

                Assert.True(ok);
                Assert.DoesNotContain(VehicleTsvIssueSeverity.Error, issues.Select(issue => issue.Severity));
                Assert.Equal("stop.wav", data.Sounds.Stop);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void TryLoadFromFile_StopSound_WhenOmitted_RemainsNull()
        {
            var path = WriteTempVehicle(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: false,
                includeAtcSection: false));

            try
            {
                var ok = VehicleTsvParser.TryLoadFromFile(path, out var data, out var issues);

                Assert.True(ok);
                Assert.DoesNotContain(VehicleTsvIssueSeverity.Error, issues.Select(issue => issue.Severity));
                Assert.Null(data.Sounds.Stop);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static string WriteTempVehicle(string content)
        {
            var path = Path.Combine(Path.GetTempPath(), $"topspeed_vehicle_{Guid.NewGuid():N}.tsv");
            File.WriteAllText(path, content);
            return path;
        }

        private static string BuildVehicleTsv(
            string primaryType,
            string supportedTypes,
            bool shiftOnDemand,
            bool includeAtcSection,
            string? stopSound = null)
        {
            var atcSection = includeAtcSection
                ? @"
[transmission_atc]
creep_accel_kphps=0.7
launch_coupling_min=0.2
launch_coupling_max=0.9
lock_speed_kph=30
lock_throttle_min=0.2
shift_release_coupling=0.5
engage_rate=12
disengage_rate=18
"
                : string.Empty;
            var stopLine = string.IsNullOrWhiteSpace(stopSound)
                ? string.Empty
                : $"stop={stopSound}\n";

            return $@"
[meta]
name=Parser Test Vehicle
version=1
description=Parser validation test

[sounds]
engine=builtin/engine.ogg
start=builtin/start.ogg
{stopLine}horn=builtin/horn.ogg
crash=builtin/crash.ogg
brake=builtin/brake.ogg
idle_freq=400
top_freq=2200
shift_freq=1200

[general]
surface_traction_factor=1
deceleration=0.1
max_speed=180
has_wipers=0

[engine]
idle_rpm=700
max_rpm=7000
rev_limiter=6500
auto_shift_rpm=0
engine_braking=0.3
mass_kg=1500
drivetrain_efficiency=0.85
launch_rpm=1800

[torque]
engine_braking_torque=150
peak_torque=280
peak_torque_rpm=3500
idle_torque=120
redline_torque=180
power_factor=0.5

[engine_rot]
inertia_kgm2=0.24
coupling_rate=12
friction_base_nm=20
friction_linear_nm_per_krpm=6
friction_quadratic_nm_per_krpm2=0.4
idle_control_window_rpm=150
idle_control_gain_nm_per_rpm=0.08
min_coupled_rise_idle_rpm_per_s=2200
min_coupled_rise_full_rpm_per_s=6200
overrun_idle_fraction=0.25
overrun_curve_exponent=1.35
brake_transfer_efficiency=0.64

[resistance]
drag_coefficient=0.30
frontal_area=2.2
rolling_resistance=0.015
coast_base_mps2=1.9
coast_linear_per_mps=0.22

[torque_curve]
1000rpm=120
3000rpm=280
6000rpm=180

[transmission]
primary_type={primaryType}
supported_types={supportedTypes}
shift_on_demand={(shiftOnDemand ? "1" : "0")}
{atcSection}
[drivetrain]
final_drive=3.5
reverse_max_speed=35
reverse_power_factor=0.55
reverse_gear_ratio=3.2
brake_strength=1.0

[gears]
number_of_gears=6
gear_ratios=3.6,2.1,1.4,1.0,0.84,0.72

[steering]
steering_response=1.0
wheelbase=2.7
max_steer_deg=35
high_speed_stability=0.1
high_speed_steer_gain=1.05
high_speed_steer_start_kph=120
high_speed_steer_full_kph=220

[tire_model]
tire_grip=1.0
lateral_grip=1.0
combined_grip_penalty=0.72
slip_angle_peak_deg=8
slip_angle_falloff=1.25
turn_response=1.0
mass_sensitivity=0.75
downforce_grip_gain=0.05

[dynamics]
corner_stiffness_front=1.0
corner_stiffness_rear=1.0
yaw_inertia_scale=1.0
steering_curve=1.0
transient_damping=1.0

[dimensions]
vehicle_width=1.8
vehicle_length=4.5

[tires]
tire_circumference=2.0
";
        }
    }
}

