using System.Collections.Generic;
using TopSpeed.Vehicles;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
    {
        private static void ValidateResolvedValues(
            ParsedSections sections,
            ParsedValues values,
            List<VehicleTsvIssue> issues)
        {
            ValidateGearRatios(sections.Gears, values.GearCount, values.GearRatios, issues);
            ValidateTransmissionTypes(
                sections.Transmission,
                values.PrimaryTransmissionType,
                values.SupportedTransmissionTypes,
                issues);
            ValidateShiftOnDemandWarnings(
                sections.Transmission,
                values.SupportedTransmissionTypes,
                values.ShiftOnDemand,
                issues);

            if (values.MaxRpm < values.IdleRpm)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    sections.Engine.Entries["max_rpm"].Line,
                    Localized("max_rpm must be greater than or equal to idle_rpm.")));
            }

            if (values.RevLimiter < values.IdleRpm || values.RevLimiter > values.MaxRpm)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    sections.Engine.Entries["rev_limiter"].Line,
                    Localized("rev_limiter must be between idle_rpm and max_rpm.")));
            }

            if (values.AutoShiftRpm > 0f && (values.AutoShiftRpm < values.IdleRpm || values.AutoShiftRpm > values.RevLimiter))
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    sections.Engine.Entries["auto_shift_rpm"].Line,
                    Localized("auto_shift_rpm must be 0 or between idle_rpm and rev_limiter.")));
            }

            if (values.PeakTorqueRpm < values.IdleRpm || values.PeakTorqueRpm > values.RevLimiter)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    sections.Torque.Entries["peak_torque_rpm"].Line,
                    Localized("peak_torque_rpm must be between idle_rpm and rev_limiter.")));
            }

            if (values.LaunchRpm > values.RevLimiter)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    sections.Engine.Entries["launch_rpm"].Line,
                    Localized("launch_rpm must not exceed rev_limiter.")));
            }

            if (values.TopFreq < values.IdleFreq)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    sections.Sounds.Entries["top_freq"].Line,
                    Localized("top_freq must be greater than or equal to idle_freq.")));
            }

            if (values.ShiftFreq < values.IdleFreq || values.ShiftFreq > values.TopFreq)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    sections.Sounds.Entries["shift_freq"].Line,
                    Localized("shift_freq must be between idle_freq and top_freq.")));
            }

            if (float.IsNaN(values.PitchCurveExponent)
                || float.IsInfinity(values.PitchCurveExponent)
                || values.PitchCurveExponent < VehicleDefinition.PitchCurveExponentMin
                || values.PitchCurveExponent > VehicleDefinition.PitchCurveExponentMax)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    sections.Sounds.Entries["pitch_curve_exponent"].Line,
                    Localized(
                        "pitch_curve_exponent must be between {0} and {1}.",
                        VehicleDefinition.PitchCurveExponentMin,
                        VehicleDefinition.PitchCurveExponentMax)));
            }

            if (values.HighSpeedSteerFullKph <= values.HighSpeedSteerStartKph)
            {
                issues.Add(new VehicleTsvIssue(
                    VehicleTsvIssueSeverity.Error,
                    sections.Steering.Entries["high_speed_steer_full_kph"].Line,
                    Localized("high_speed_steer_full_kph must be greater than high_speed_steer_start_kph.")));
            }
        }
    }
}

