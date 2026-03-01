using System;
using System.Collections.Generic;
using System.IO;

namespace TopSpeed.Race.TimeTrial
{
    internal sealed class ScoreStore
    {
        private const string FileName = "highscore.cfg";
        private readonly string _path;

        public ScoreStore(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public static ScoreStore CreateDefault()
        {
            var path = Path.Combine(AppContext.BaseDirectory, FileName);
            return new ScoreStore(path);
        }

        public int Read(string trackName, int laps)
        {
            if (string.IsNullOrWhiteSpace(trackName))
                return 0;
            if (!File.Exists(_path))
                return 0;

            var key = BuildKey(trackName, laps);
            foreach (var line in File.ReadLines(_path))
            {
                if (!TryParseLine(line, out var field, out var value))
                    continue;
                if (!string.Equals(field, key, StringComparison.OrdinalIgnoreCase))
                    continue;
                return value;
            }

            return 0;
        }

        public void Write(string trackName, int laps, int raceTime)
        {
            if (string.IsNullOrWhiteSpace(trackName))
                return;

            var key = BuildKey(trackName, laps);
            var lines = new List<string>();
            var found = false;
            if (File.Exists(_path))
            {
                foreach (var line in File.ReadLines(_path))
                {
                    if (!TrySplitLine(line, out var field, out _))
                    {
                        lines.Add(line);
                        continue;
                    }

                    if (string.Equals(field, key, StringComparison.OrdinalIgnoreCase))
                    {
                        lines.Add($"{key}={raceTime}");
                        found = true;
                    }
                    else
                    {
                        lines.Add(line);
                    }
                }
            }

            if (!found)
                lines.Add($"{key}={raceTime}");

            File.WriteAllLines(_path, lines);
        }

        private static string BuildKey(string trackName, int laps)
        {
            return $"{trackName};{laps}";
        }

        private static bool TryParseLine(string line, out string field, out int value)
        {
            field = string.Empty;
            value = 0;
            if (!TrySplitLine(line, out field, out var valuePart))
                return false;
            return int.TryParse(valuePart, out value);
        }

        private static bool TrySplitLine(string line, out string field, out string value)
        {
            field = string.Empty;
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var idx = line.IndexOf('=');
            if (idx <= 0)
                return false;

            field = line.Substring(0, idx).Trim();
            value = line.Substring(idx + 1).Trim();
            return field.Length > 0;
        }
    }
}
