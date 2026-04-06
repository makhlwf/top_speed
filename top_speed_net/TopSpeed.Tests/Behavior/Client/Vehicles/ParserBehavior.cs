using System;
using System.IO;
using System.Linq;
using TopSpeed.Vehicles.Parsing;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "Behavior")]
    public sealed class VehicleParserBehaviorTests
    {
        [Fact]
        public void ShiftOnDemandWithoutAutomatic_ShouldAddWarning()
        {
            using var tempFile = TempVehicleFile.Create(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: true,
                includeAtcSection: false));

            var ok = VehicleTsvParser.TryLoadFromFile(tempFile.Path, out var _, out var issues);

            ok.Should().BeTrue(DescribeIssues(issues));
            issues.Where(x => x.Severity == VehicleTsvIssueSeverity.Warning)
                .Select(x => x.Message)
                .Should()
                .ContainSingle(x => x.Contains("shift_on_demand is ignored", StringComparison.OrdinalIgnoreCase));
            issues.Select(x => x.Severity).Should().NotContain(VehicleTsvIssueSeverity.Error);
        }

        [Fact]
        public void AdvancedResistanceKeys_ShouldBeParsed()
        {
            using var tempFile = TempVehicleFile.Create(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: false,
                includeAtcSection: false));

            var ok = VehicleTsvParser.TryLoadFromFile(tempFile.Path, out var data, out var issues);

            ok.Should().BeTrue(DescribeIssues(issues));
            issues.Select(x => x.Severity).Should().NotContain(VehicleTsvIssueSeverity.Error);
            data.CoastDragBaseMps2.Should().BeApproximately(1.9f, 0.001f);
            data.CoastDragLinearPerMps.Should().BeApproximately(0.22f, 0.001f);
            data.EngineOverrunIdleLossFraction.Should().BeApproximately(0.25f, 0.001f);
            data.OverrunCurveExponent.Should().BeApproximately(1.35f, 0.001f);
            data.EngineBrakeTransferEfficiency.Should().BeApproximately(0.64f, 0.001f);
        }

        [Fact]
        public void StopSound_ShouldStayOptional()
        {
            using var withStop = TempVehicleFile.Create(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: false,
                includeAtcSection: false,
                stopSound: "stop.wav"));
            using var withoutStop = TempVehicleFile.Create(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: false,
                includeAtcSection: false));

            VehicleTsvParser.TryLoadFromFile(withStop.Path, out var withStopData, out var withStopIssues).Should().BeTrue(DescribeIssues(withStopIssues));
            VehicleTsvParser.TryLoadFromFile(withoutStop.Path, out var withoutStopData, out var withoutStopIssues).Should().BeTrue(DescribeIssues(withoutStopIssues));

            withStopData.Sounds.Stop.Should().Be("stop.wav");
            withoutStopData.Sounds.Stop.Should().BeNull();
        }

        private static string DescribeIssues(System.Collections.Generic.IReadOnlyList<VehicleTsvIssue> issues)
        {
            if (issues.Count == 0)
                return "the parser should accept the generated fixture";

            return string.Join("; ", issues.Select(x => $"{x.Severity}@{x.Line}:{x.Message}"));
        }

        [Fact]
        public void MissingTorqueCurveSection_ShouldFailWithHelpfulMessage()
        {
            using var tempFile = TempVehicleFile.Create(BuildVehicleTsv(
                primaryType: "manual",
                supportedTypes: "manual",
                shiftOnDemand: false,
                includeAtcSection: false).Replace("[torque_curve]\n700rpm=120\n3000rpm=280\n6500rpm=180\n\n", string.Empty));

            var ok = VehicleTsvParser.TryLoadFromFile(tempFile.Path, out var _, out var issues);

            ok.Should().BeFalse();
            issues.Select(x => x.Message)
                .Should()
                .Contain(x => x.Contains("Missing required section [torque_curve]", StringComparison.OrdinalIgnoreCase));
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
            var stopLine = string.IsNullOrWhiteSpace(stopSound) ? string.Empty : $"stop={stopSound}\n";

            return $@"
[meta]
name=Parser Test Vehicle
version=1
description=Parser validation test

[sounds]
engine=builtin6
start=builtin1
{stopLine}horn=builtin/horn.ogg
crash=builtin3
brake=builtin/brake.ogg
idle_freq=400
top_freq=2200
shift_freq=1200
pitch_curve_exponent=0.85

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
drag_coefficient=0.3
frontal_area=2.2
rolling_resistance=0.015
coast_base_mps2=1.9
coast_linear_per_mps=0.22

[torque_curve]
700rpm=120
3000rpm=280
6500rpm=180

[transmission]
primary_type={primaryType}
supported_types={supportedTypes}
shift_on_demand={(shiftOnDemand ? 1 : 0)}
{atcSection}

[drivetrain]
final_drive=3.8
reverse_max_speed=35
reverse_power_factor=0.55
reverse_gear_ratio=3.2
brake_strength=1.0

[gears]
number_of_gears=5
gear_ratios=3.7,2.1,1.4,1.1,0.9

[steering]
steering_response=1.2
wheelbase=2.6
max_steer_deg=32
high_speed_stability=0.25
high_speed_steer_gain=0.92
high_speed_steer_start_kph=140
high_speed_steer_full_kph=220

[tire_model]
tire_grip=0.92
lateral_grip=1.00
combined_grip_penalty=0.72
slip_angle_peak_deg=8.0
slip_angle_falloff=1.25
turn_response=1.05
mass_sensitivity=0.75
downforce_grip_gain=0.10

[dynamics]
corner_stiffness_front=1.05
corner_stiffness_rear=0.98
yaw_inertia_scale=1.05
steering_curve=1.00
transient_damping=1.10

[dimensions]
vehicle_width=1.84
vehicle_length=4.40

[tires]
tire_width=215
tire_aspect=55
tire_rim=17
";
        }

        private sealed class TempVehicleFile : IDisposable
        {
            public string Path { get; }

            private TempVehicleFile(string path)
            {
                Path = path;
            }

            public static TempVehicleFile Create(string content)
            {
                var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"topspeed_vehicle_{Guid.NewGuid():N}.tsv");
                File.WriteAllText(path, content);
                return new TempVehicleFile(path);
            }

            public void Dispose()
            {
                if (File.Exists(Path))
                    File.Delete(Path);
            }
        }
    }
}
