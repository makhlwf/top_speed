using System;
using System.Collections.Generic;
using System.Numerics;

namespace TopSpeed.Tracks.Map
{
    internal readonly struct StartGridLayout
    {
        public StartGridLayout(
            Vector3 forward,
            Vector3 right,
            float centerRight,
            float startForward,
            float rowSpacing,
            float columnSpacing,
            int columns,
            int rows)
        {
            Forward = forward;
            Right = right;
            CenterRight = centerRight;
            StartForward = startForward;
            RowSpacing = rowSpacing;
            ColumnSpacing = columnSpacing;
            Columns = columns;
            Rows = rows;
        }

        public Vector3 Forward { get; }
        public Vector3 Right { get; }
        public float CenterRight { get; }
        public float StartForward { get; }
        public float RowSpacing { get; }
        public float ColumnSpacing { get; }
        public int Columns { get; }
        public int Rows { get; }
        public int Capacity => Math.Max(0, Columns * Rows);
    }

    internal static class StartGridBuilder
    {
        public static bool TryBuild(TrackMap map, float maxVehicleWidth, float rowSpacing, out StartGridLayout layout)
        {
            layout = default;
            if (map == null)
                return false;
            if (!map.TryGetStartAreaBounds(out var minX, out var minZ, out var maxX, out var maxZ))
                return false;

            var hasStartArea = map.TryGetStartAreaDefinition(out var startArea);
            var metadata = hasStartArea ? startArea.Metadata : null;

            var forward = MapMovement.HeadingVector(map.StartHeadingDegrees);
            if (forward.LengthSquared() <= float.Epsilon)
                return false;
            forward = Vector3.Normalize(forward);
            var right = new Vector3(forward.Z, 0f, -forward.X);

            var corners = new[]
            {
                new Vector3(minX, 0f, minZ),
                new Vector3(minX, 0f, maxZ),
                new Vector3(maxX, 0f, minZ),
                new Vector3(maxX, 0f, maxZ)
            };

            var minRight = float.MaxValue;
            var maxRight = float.MinValue;
            var minForward = float.MaxValue;
            var maxForward = float.MinValue;

            foreach (var corner in corners)
            {
                var r = Vector3.Dot(corner, right);
                var f = Vector3.Dot(corner, forward);
                if (r < minRight) minRight = r;
                if (r > maxRight) maxRight = r;
                if (f < minForward) minForward = f;
                if (f > maxForward) maxForward = f;
            }

            var width = maxRight - minRight;
            var length = maxForward - minForward;
            if (width <= 0f || length <= 0f)
                return false;

            var margin = Math.Max(0.5f, maxVehicleWidth * 0.2f);
            if (TryGetFloat(metadata, out var marginValue, "grid_margin", "margin"))
                margin = Math.Max(0f, marginValue);

            if (TryGetFloat(metadata, out var rowSpacingValue, "grid_row_spacing", "row_spacing", "grid_row"))
                rowSpacing = Math.Max(1f, rowSpacingValue);

            var columnSpacing = Math.Max(maxVehicleWidth + 0.6f, 2f);
            if (TryGetFloat(metadata, out var colSpacingValue, "grid_col_spacing", "col_spacing", "grid_col"))
                columnSpacing = Math.Max(1f, colSpacingValue);

            var columns = (int)Math.Floor((width - margin * 2f) / columnSpacing);
            if (columns < 1)
                columns = 1;

            var rows = (int)Math.Floor((length - margin * 2f) / rowSpacing);
            if (rows < 1)
                rows = 1;

            if (TryGetInt(metadata, out var gridCols, "grid_cols", "grid_columns", "columns"))
                columns = Math.Max(1, Math.Min(columns, gridCols));
            if (TryGetInt(metadata, out var gridRows, "grid_rows", "rows"))
                rows = Math.Max(1, Math.Min(rows, gridRows));

            if (TryGetInt(metadata, out var maxVehicles, "grid_max_vehicles", "max_vehicles", "grid_capacity"))
            {
                if (maxVehicles > 0)
                {
                    if (columns > maxVehicles)
                        columns = maxVehicles;
                    var maxRows = (int)Math.Ceiling(maxVehicles / (float)columns);
                    if (maxRows < rows)
                        rows = Math.Max(1, maxRows);
                }
            }

            var centerRight = (minRight + maxRight) * 0.5f;
            var startForward = maxForward - margin - rowSpacing * 0.5f;

            layout = new StartGridLayout(forward, right, centerRight, startForward, rowSpacing, columnSpacing, columns, rows);
            return true;
        }

        public static Vector3 GetPosition(StartGridLayout layout, int gridIndex)
        {
            var index = Math.Max(0, gridIndex - 1);
            var columns = Math.Max(1, layout.Columns);
            var rows = Math.Max(1, layout.Rows);

            var row = index / columns;
            if (row >= rows)
                row = rows - 1;

            var col = index % columns;
            var startRight = layout.CenterRight - ((columns - 1) * 0.5f * layout.ColumnSpacing);
            var localRight = startRight + (col * layout.ColumnSpacing);
            var localForward = layout.StartForward - (row * layout.RowSpacing);

            return (layout.Right * localRight) + (layout.Forward * localForward);
        }

        private static bool TryGetFloat(
            IReadOnlyDictionary<string, string>? metadata,
            out float value,
            params string[] keys)
        {
            value = 0f;
            if (metadata == null || metadata.Count == 0)
                return false;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw))
                    continue;
                if (float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                    return true;
            }
            return false;
        }

        private static bool TryGetInt(
            IReadOnlyDictionary<string, string>? metadata,
            out int value,
            params string[] keys)
        {
            value = 0;
            if (metadata == null || metadata.Count == 0)
                return false;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw))
                    continue;
                if (int.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out value))
                    return true;
            }
            return false;
        }
    }
}
