using TopSpeed.Data;
using TopSpeed.Tracks;
using TopSpeed.Vehicles.Parsing;

namespace TopSpeed.Vehicles.Loader
{
    internal static partial class Spec
    {
        public static Common FromParameters(VehicleParameters parameters, TrackWeather weather)
        {
            return new Common
            {
                SurfaceTractionFactor = parameters.SurfaceTractionFactor,
                Deceleration = parameters.Deceleration,
                TopSpeed = parameters.TopSpeed,
                IdleFreq = parameters.IdleFreq,
                TopFreq = parameters.TopFreq,
                ShiftFreq = parameters.ShiftFreq,
                Gears = parameters.Gears,
                Steering = parameters.Steering,
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
                ReverseMaxSpeedKph = parameters.ReverseMaxSpeedKph,
                ReversePowerFactor = parameters.ReversePowerFactor,
                ReverseGearRatio = parameters.ReverseGearRatio,
                TireCircumferenceM = parameters.TireCircumferenceM,
                LateralGripCoefficient = parameters.LateralGripCoefficient,
                HighSpeedStability = parameters.HighSpeedStability,
                WheelbaseM = parameters.WheelbaseM,
                MaxSteerDeg = parameters.MaxSteerDeg,
                WidthM = parameters.WidthM,
                LengthM = parameters.LengthM,
                PowerFactor = parameters.PowerFactor,
                GearRatios = parameters.GearRatios,
                BrakeStrength = parameters.BrakeStrength,
                TransmissionPolicy = parameters.TransmissionPolicy
            };
        }

        public static Common FromCustom(CustomVehicleTsvData parsed, TrackWeather weather)
        {
            return new Common
            {
                SurfaceTractionFactor = parsed.SurfaceTractionFactor,
                Deceleration = parsed.Deceleration,
                TopSpeed = parsed.TopSpeed,
                IdleFreq = parsed.IdleFreq,
                TopFreq = parsed.TopFreq,
                ShiftFreq = parsed.ShiftFreq,
                Gears = parsed.Gears,
                Steering = parsed.Steering,
                HasWipers = weather == TrackWeather.Rain ? parsed.HasWipers : 0,
                IdleRpm = parsed.IdleRpm,
                MaxRpm = parsed.MaxRpm,
                RevLimiter = parsed.RevLimiter,
                AutoShiftRpm = parsed.AutoShiftRpm > 0f ? parsed.AutoShiftRpm : parsed.RevLimiter * 0.92f,
                EngineBraking = parsed.EngineBraking,
                MassKg = parsed.MassKg,
                DrivetrainEfficiency = parsed.DrivetrainEfficiency,
                EngineBrakingTorqueNm = parsed.EngineBrakingTorqueNm,
                TireGripCoefficient = parsed.TireGripCoefficient,
                PeakTorqueNm = parsed.PeakTorqueNm,
                PeakTorqueRpm = parsed.PeakTorqueRpm,
                IdleTorqueNm = parsed.IdleTorqueNm,
                RedlineTorqueNm = parsed.RedlineTorqueNm,
                DragCoefficient = parsed.DragCoefficient,
                FrontalAreaM2 = parsed.FrontalAreaM2,
                RollingResistanceCoefficient = parsed.RollingResistanceCoefficient,
                LaunchRpm = parsed.LaunchRpm,
                FinalDriveRatio = parsed.FinalDriveRatio,
                ReverseMaxSpeedKph = parsed.ReverseMaxSpeedKph,
                ReversePowerFactor = parsed.ReversePowerFactor,
                ReverseGearRatio = parsed.ReverseGearRatio,
                TireCircumferenceM = parsed.TireCircumferenceM,
                LateralGripCoefficient = parsed.LateralGripCoefficient,
                HighSpeedStability = parsed.HighSpeedStability,
                WheelbaseM = parsed.WheelbaseM,
                MaxSteerDeg = parsed.MaxSteerDeg,
                WidthM = parsed.WidthM,
                LengthM = parsed.LengthM,
                PowerFactor = parsed.PowerFactor,
                GearRatios = parsed.GearRatios,
                BrakeStrength = parsed.BrakeStrength,
                TransmissionPolicy = parsed.TransmissionPolicy
            };
        }
    }
}
