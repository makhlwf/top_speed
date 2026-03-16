using System;
using System.Collections.Generic;

namespace TopSpeed.Physics.Torque
{
    public static class CurveFactory
    {
        public static CurveProfile FromLegacy(
            float idleRpm,
            float revLimiter,
            float peakTorqueRpm,
            float idleTorqueNm,
            float peakTorqueNm,
            float redlineTorqueNm)
        {
            var points = BuildPreset(
                "family_sedan",
                idleRpm,
                revLimiter,
                peakTorqueRpm,
                idleTorqueNm,
                peakTorqueNm,
                redlineTorqueNm);

            return new CurveProfile(points);
        }

        public static CurveProfile FromPoints(
            IReadOnlyList<CurvePoint> points,
            float idleRpm,
            float revLimiter,
            float peakTorqueRpm,
            float idleTorqueNm,
            float peakTorqueNm,
            float redlineTorqueNm)
        {
            if (points != null && points.Count >= 2)
                return new CurveProfile(points);

            return FromLegacy(
                idleRpm,
                revLimiter,
                peakTorqueRpm,
                idleTorqueNm,
                peakTorqueNm,
                redlineTorqueNm);
        }

        public static IReadOnlyList<CurvePoint> BuildPreset(
            string presetName,
            float idleRpm,
            float revLimiter,
            float peakTorqueRpm,
            float idleTorqueNm,
            float peakTorqueNm,
            float redlineTorqueNm)
        {
            if (string.IsNullOrWhiteSpace(presetName))
                presetName = "family_sedan";

            var preset = PresetCatalog.Get(presetName);

            var idle = Math.Max(500f, idleRpm);
            var rev = Math.Max(idle + 1200f, revLimiter);
            var peakRpm = Clamp(peakTorqueRpm > 0f ? peakTorqueRpm : idle + ((rev - idle) * 0.60f), idle + 200f, rev - 200f);
            var peakTorque = Math.Max(1f, peakTorqueNm);

            var idleTorque = idleTorqueNm > 0f ? idleTorqueNm : peakTorque * preset.IdleTorqueFactor;
            var redlineTorque = redlineTorqueNm > 0f ? redlineTorqueNm : peakTorque * preset.RedlineTorqueFactor;

            idleTorque = Clamp(idleTorque, 0f, peakTorque * 0.98f);
            redlineTorque = Clamp(redlineTorque, 0f, peakTorque * 0.98f);

            var samples = new[]
            {
                0.00f, 0.08f, 0.15f, 0.25f, 0.35f, 0.45f, 0.55f, 0.65f, 0.75f, 0.85f, 0.92f, 1.00f
            };

            var points = new CurvePoint[samples.Length];
            var peakPosition = (peakRpm - idle) / (rev - idle);
            peakPosition = Clamp(peakPosition, 0.10f, 0.95f);

            for (var i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                var rpm = idle + ((rev - idle) * sample);
                var torque = EvaluatePresetTorque(
                    sample,
                    peakPosition,
                    idleTorque,
                    peakTorque,
                    redlineTorque,
                    preset.RiseExponent,
                    preset.FallExponent);
                points[i] = new CurvePoint(rpm, torque);
            }

            return points;
        }

        private static float EvaluatePresetTorque(
            float sample,
            float peakPosition,
            float idleTorqueNm,
            float peakTorqueNm,
            float redlineTorqueNm,
            float riseExponent,
            float fallExponent)
        {
            if (sample <= peakPosition)
            {
                var normalized = peakPosition <= 0f ? 0f : sample / peakPosition;
                normalized = (float)Math.Pow(Clamp(normalized, 0f, 1f), riseExponent);
                return Lerp(idleTorqueNm, peakTorqueNm, normalized);
            }

            var remaining = 1f - peakPosition;
            var normalizedFall = remaining <= 0f ? 1f : (sample - peakPosition) / remaining;
            normalizedFall = (float)Math.Pow(Clamp(normalizedFall, 0f, 1f), fallExponent);
            return Lerp(peakTorqueNm, redlineTorqueNm, normalizedFall);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + ((b - a) * t);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
