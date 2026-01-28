using System;
using System.Numerics;

namespace TopSpeed.Tracks.Map
{
    internal static class MapMovement
    {
        public static MapMovementState CreateStart(TrackMap map)
        {
            var position = new Vector3(map.StartX, 0f, map.StartZ);
            var heading = map.StartHeadingDegrees;
            if (Math.Abs(heading) < 0.001f && map.StartHeading != MapDirection.North)
                heading = HeadingFromDirection(map.StartHeading);
            return new MapMovementState
            {
                WorldPosition = position,
                HeadingDegrees = NormalizeDegrees(heading),
                DistanceMeters = 0f
            };
        }

        public static float HeadingFromYaw(float yawRadians)
        {
            var degrees = yawRadians * 180f / (float)Math.PI;
            return NormalizeDegrees(degrees);
        }

        public static Vector3 HeadingVector(float headingDegrees)
        {
            var radians = headingDegrees * (float)Math.PI / 180f;
            return new Vector3((float)Math.Sin(radians), 0f, (float)Math.Cos(radians));
        }

        public static float HeadingFromDirection(MapDirection direction)
        {
            return direction switch
            {
                MapDirection.North => 0f,
                MapDirection.East => 90f,
                MapDirection.South => 180f,
                MapDirection.West => 270f,
                _ => 0f
            };
        }

        public static MapDirection ToCardinal(float headingDegrees)
        {
            var normalized = NormalizeDegrees(headingDegrees);
            if (normalized >= 45f && normalized < 135f)
                return MapDirection.East;
            if (normalized >= 135f && normalized < 225f)
                return MapDirection.South;
            if (normalized >= 225f && normalized < 315f)
                return MapDirection.West;
            return MapDirection.North;
        }

        public static float NormalizeDegrees(float degrees)
        {
            var result = degrees % 360f;
            if (result < 0f)
                result += 360f;
            return result;
        }
    }
}
