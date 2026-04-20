using System;

namespace TS.Sdl.Input
{
    public static class TouchZoneLayout
    {
        public static TouchZone[] Horizontal(
            string topId,
            string bottomId,
            float splitY = 0.5f,
            int topPriority = 0,
            int bottomPriority = 0,
            TouchZoneBehavior topBehavior = TouchZoneBehavior.Lock,
            TouchZoneBehavior bottomBehavior = TouchZoneBehavior.Lock)
        {
            ValidateSplit(splitY);
            return new[]
            {
                new TouchZone(topId, new TouchZoneRect(0f, 0f, 1f, splitY), topPriority, topBehavior),
                new TouchZone(bottomId, new TouchZoneRect(0f, splitY, 1f, 1f - splitY), bottomPriority, bottomBehavior)
            };
        }

        public static TouchZone[] Vertical(
            string leftId,
            string rightId,
            float splitX = 0.5f,
            int leftPriority = 0,
            int rightPriority = 0,
            TouchZoneBehavior leftBehavior = TouchZoneBehavior.Lock,
            TouchZoneBehavior rightBehavior = TouchZoneBehavior.Lock)
        {
            ValidateSplit(splitX);
            return new[]
            {
                new TouchZone(leftId, new TouchZoneRect(0f, 0f, splitX, 1f), leftPriority, leftBehavior),
                new TouchZone(rightId, new TouchZoneRect(splitX, 0f, 1f - splitX, 1f), rightPriority, rightBehavior)
            };
        }

        public static TouchZone[] Grid(
            int columns,
            int rows,
            string prefix = "zone",
            int priority = 0,
            TouchZoneBehavior behavior = TouchZoneBehavior.Lock)
        {
            if (columns <= 0)
                throw new ArgumentOutOfRangeException(nameof(columns), "Columns must be greater than zero.");
            if (rows <= 0)
                throw new ArgumentOutOfRangeException(nameof(rows), "Rows must be greater than zero.");
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Zone id prefix is required.", nameof(prefix));

            var result = new TouchZone[rows * columns];
            var cellWidth = 1f / columns;
            var cellHeight = 1f / rows;
            var index = 0;
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    var x = col * cellWidth;
                    var y = row * cellHeight;
                    var width = col == columns - 1 ? 1f - x : cellWidth;
                    var height = row == rows - 1 ? 1f - y : cellHeight;
                    var id = $"{prefix}_{row}_{col}";
                    result[index++] = new TouchZone(id, new TouchZoneRect(x, y, width, height), priority, behavior);
                }
            }

            return result;
        }

        private static void ValidateSplit(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f || value >= 1f)
                throw new ArgumentOutOfRangeException(nameof(value), "Split must be between zero and one.");
        }
    }
}

