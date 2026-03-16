using System;
using System.Collections.Generic;

namespace TopSpeed.Physics.Torque
{
    public sealed class CurveProfile
    {
        private readonly float[] _rpm;
        private readonly float[] _torqueNm;

        public CurveProfile(IReadOnlyList<CurvePoint> points)
        {
            if (points == null || points.Count < 2)
                throw new ArgumentException("Torque curve requires at least two points.", nameof(points));

            var sorted = new List<CurvePoint>(points.Count);
            for (var i = 0; i < points.Count; i++)
            {
                if (points[i].Rpm > 0f)
                    sorted.Add(points[i]);
            }

            if (sorted.Count < 2)
                throw new ArgumentException("Torque curve requires at least two valid points.", nameof(points));

            sorted.Sort((a, b) => a.Rpm.CompareTo(b.Rpm));

            _rpm = new float[sorted.Count];
            _torqueNm = new float[sorted.Count];
            for (var i = 0; i < sorted.Count; i++)
            {
                _rpm[i] = sorted[i].Rpm;
                _torqueNm[i] = Math.Max(0f, sorted[i].TorqueNm);
            }
        }

        public float EvaluateTorque(float rpm)
        {
            if (rpm <= _rpm[0])
                return _torqueNm[0];
            if (rpm >= _rpm[_rpm.Length - 1])
                return _torqueNm[_torqueNm.Length - 1];

            var index = Array.BinarySearch(_rpm, rpm);
            if (index >= 0)
                return _torqueNm[index];

            index = ~index;
            var left = Math.Max(0, index - 1);
            var right = Math.Min(_rpm.Length - 1, index);
            if (left == right)
                return _torqueNm[left];

            var span = _rpm[right] - _rpm[left];
            if (span <= 0f)
                return _torqueNm[left];

            var t = (rpm - _rpm[left]) / span;
            return _torqueNm[left] + ((_torqueNm[right] - _torqueNm[left]) * t);
        }

        public IReadOnlyList<CurvePoint> ToPoints()
        {
            var points = new CurvePoint[_rpm.Length];
            for (var i = 0; i < _rpm.Length; i++)
                points[i] = new CurvePoint(_rpm[i], _torqueNm[i]);
            return points;
        }
    }
}
