using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TopSpeed.Localization;

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
                parts.Add(LocalizationService.Format(
                    minutes == 1
                        ? LocalizationService.Mark("{0} minute")
                        : LocalizationService.Mark("{0} minutes"),
                    minutes));
            parts.Add(LocalizationService.Format(
                seconds == 1
                    ? LocalizationService.Mark("{0} second")
                    : LocalizationService.Mark("{0} seconds"),
                seconds));
            if (detailed)
            {
                var millis = raceTimeMs % 1000;
                parts.Add(LocalizationService.Format(
                    LocalizationService.Mark("{0} milliseconds"),
                    millis.ToString("D3", CultureInfo.InvariantCulture)));
            }
            return string.Join(" ", parts);
        }

        protected static string FormatRacePercentageText(int percent)
        {
            var clamped = Math.Max(0, Math.Min(100, percent));
            return LocalizationService.Format(LocalizationService.Mark("Race percentage {0} percent"), clamped);
        }

        protected static string FormatLapPercentageText(int percent)
        {
            var clamped = Math.Max(0, Math.Min(100, percent));
            return LocalizationService.Format(LocalizationService.Mark("Lap percentage {0} percent"), clamped);
        }

        protected static string FormatPlayerPercentageText(int percent)
        {
            var clamped = Math.Max(0, Math.Min(100, percent));
            return clamped.ToString(CultureInfo.InvariantCulture)
                + " "
                + LocalizationService.Translate(LocalizationService.Mark("percent"));
        }

        protected static string FormatVehicleName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return LocalizationService.Mark("Vehicle");
            return name!.Replace('_', ' ').Replace('-', ' ').Trim();
        }

        protected static string FormatTrackName(string trackName)
        {
            switch (trackName)
            {
                case "america":
                    return LocalizationService.Mark("America");
                case "austria":
                    return LocalizationService.Mark("Austria");
                case "belgium":
                    return LocalizationService.Mark("Belgium");
                case "brazil":
                    return LocalizationService.Mark("Brazil");
                case "china":
                    return LocalizationService.Mark("China");
                case "england":
                    return LocalizationService.Mark("England");
                case "finland":
                    return LocalizationService.Mark("Finland");
                case "france":
                    return LocalizationService.Mark("France");
                case "germany":
                    return LocalizationService.Mark("Germany");
                case "ireland":
                    return LocalizationService.Mark("Ireland");
                case "italy":
                    return LocalizationService.Mark("Italy");
                case "netherlands":
                    return LocalizationService.Mark("Netherlands");
                case "portugal":
                    return LocalizationService.Mark("Portugal");
                case "russia":
                    return LocalizationService.Mark("Russia");
                case "spain":
                    return LocalizationService.Mark("Spain");
                case "sweden":
                    return LocalizationService.Mark("Sweden");
                case "switserland":
                    return LocalizationService.Mark("Switzerland");
                case "advHills":
                    return LocalizationService.Mark("Rally hills");
                case "advCoast":
                    return LocalizationService.Mark("French coast");
                case "advCountry":
                    return LocalizationService.Mark("English country");
                case "advAirport":
                    return LocalizationService.Mark("Ride airport");
                case "advDesert":
                    return LocalizationService.Mark("Rally desert");
                case "advRush":
                    return LocalizationService.Mark("Rush hour");
                case "advEscape":
                    return LocalizationService.Mark("Polar escape");
                case "custom":
                    return LocalizationService.Mark("Custom track");
            }

            var baseName = trackName;
            if (trackName.IndexOfAny(new[] { '\\', '/' }) >= 0)
                baseName = Path.GetFileNameWithoutExtension(trackName) ?? trackName;
            else if (trackName.Length > 4)
                baseName = Path.GetFileNameWithoutExtension(trackName) ?? trackName;
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = LocalizationService.Mark("Track");
            return FormatVehicleName(baseName);
        }
    }
}


