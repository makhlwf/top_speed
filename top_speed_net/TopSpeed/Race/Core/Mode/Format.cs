using System;
using System.Collections.Generic;
using System.IO;

namespace TopSpeed.Race
{
    internal abstract partial class RaceMode
    {
        protected static string FormatTimeText(int raceTimeMs, bool detailed)
        {
            if (raceTimeMs < 0)
                raceTimeMs = 0;
            var minutes = raceTimeMs / 60000;
            var seconds = (raceTimeMs % 60000) / 1000;
            var parts = new List<string>();
            if (minutes > 0)
                parts.Add($"{minutes} {(minutes == 1 ? "minute" : "minutes")}");
            parts.Add($"{seconds} {(seconds == 1 ? "second" : "seconds")}");
            if (detailed)
            {
                var millis = raceTimeMs % 1000;
                parts.Add($"point {millis:D3}");
            }
            return string.Join(" ", parts);
        }

        protected static string FormatPercentageText(string label, int percent)
        {
            var clamped = Math.Max(0, Math.Min(100, percent));
            return string.IsNullOrWhiteSpace(label)
                ? $"{clamped} percent"
                : $"{label} {clamped} percent";
        }

        protected static string FormatVehicleName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Vehicle";
            return name!.Replace('_', ' ').Replace('-', ' ').Trim();
        }

        protected static string FormatTrackName(string trackName)
        {
            switch (trackName)
            {
                case "america":
                    return "America";
                case "austria":
                    return "Austria";
                case "belgium":
                    return "Belgium";
                case "brazil":
                    return "Brazil";
                case "china":
                    return "China";
                case "england":
                    return "England";
                case "finland":
                    return "Finland";
                case "france":
                    return "France";
                case "germany":
                    return "Germany";
                case "ireland":
                    return "Ireland";
                case "italy":
                    return "Italy";
                case "netherlands":
                    return "Netherlands";
                case "portugal":
                    return "Portugal";
                case "russia":
                    return "Russia";
                case "spain":
                    return "Spain";
                case "sweden":
                    return "Sweden";
                case "switserland":
                    return "Switzerland";
                case "advHills":
                    return "Rally hills";
                case "advCoast":
                    return "French coast";
                case "advCountry":
                    return "English country";
                case "advAirport":
                    return "Ride airport";
                case "advDesert":
                    return "Rally desert";
                case "advRush":
                    return "Rush hour";
                case "advEscape":
                    return "Polar escape";
                case "custom":
                    return "Custom track";
            }

            var baseName = trackName;
            if (trackName.IndexOfAny(new[] { '\\', '/' }) >= 0)
                baseName = Path.GetFileNameWithoutExtension(trackName) ?? trackName;
            else if (trackName.Length > 4)
                baseName = Path.GetFileNameWithoutExtension(trackName) ?? trackName;
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "Track";
            return FormatVehicleName(baseName);
        }
    }
}

