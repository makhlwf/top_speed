using System;
using System.Collections.Generic;
using System.Linq;
using TopSpeed.Common;
using TopSpeed.Localization;

namespace TopSpeed.Core
{
    internal readonly struct TrackInfo
    {
        public TrackInfo(string key, string display)
        {
            Key = key;
            Display = display;
        }

        public string Key { get; }
        public string Display { get; }
    }

    internal static class TrackList
    {
        public static readonly TrackInfo[] RaceTracks =
        {
            new TrackInfo("america", LocalizationService.Mark("America")),
            new TrackInfo("austria", LocalizationService.Mark("Austria")),
            new TrackInfo("belgium", LocalizationService.Mark("Belgium")),
            new TrackInfo("brazil", LocalizationService.Mark("Brazil")),
            new TrackInfo("china", LocalizationService.Mark("China")),
            new TrackInfo("england", LocalizationService.Mark("England")),
            new TrackInfo("finland", LocalizationService.Mark("Finland")),
            new TrackInfo("france", LocalizationService.Mark("France")),
            new TrackInfo("germany", LocalizationService.Mark("Germany")),
            new TrackInfo("ireland", LocalizationService.Mark("Ireland")),
            new TrackInfo("italy", LocalizationService.Mark("Italy")),
            new TrackInfo("netherlands", LocalizationService.Mark("Netherlands")),
            new TrackInfo("portugal", LocalizationService.Mark("Portugal")),
            new TrackInfo("russia", LocalizationService.Mark("Russia")),
            new TrackInfo("spain", LocalizationService.Mark("Spain")),
            new TrackInfo("sweden", LocalizationService.Mark("Sweden")),
            new TrackInfo("switserland", LocalizationService.Mark("Switserland"))
        };

        public static readonly TrackInfo[] AdventureTracks =
        {
            new TrackInfo("advHills", LocalizationService.Mark("Rally hills")),
            new TrackInfo("advCoast", LocalizationService.Mark("French coast")),
            new TrackInfo("advCountry", LocalizationService.Mark("English country")),
            new TrackInfo("advAirport", LocalizationService.Mark("Ride airport")),
            new TrackInfo("advDesert", LocalizationService.Mark("Rally desert")),
            new TrackInfo("advRush", LocalizationService.Mark("Rush hour")),
            new TrackInfo("advEscape", LocalizationService.Mark("Polar escape"))
        };

        public static IReadOnlyList<TrackInfo> GetTracks(TrackCategory category)
        {
            return category switch
            {
                TrackCategory.RaceTrack => RaceTracks,
                TrackCategory.StreetAdventure => AdventureTracks,
                _ => Array.Empty<TrackInfo>()
            };
        }

        public static string GetRandomTrackKey(TrackCategory category, IEnumerable<string> customTracks)
        {
            var candidates = new List<string>();
            var source = category switch
            {
                TrackCategory.RaceTrack => RaceTracks,
                TrackCategory.StreetAdventure => AdventureTracks,
                _ => Array.Empty<TrackInfo>()
            };
            candidates.AddRange(source.Select(t => t.Key));

            if (customTracks != null)
                candidates.AddRange(customTracks);

            if (candidates.Count == 0)
                return RaceTracks[0].Key;

            var index = Algorithm.RandomInt(candidates.Count);
            return candidates[index];
        }

        public static (string Key, TrackCategory Category) GetRandomTrackAny(IEnumerable<string> customTracks)
        {
            var candidates = new List<(string Key, TrackCategory Category)>();
            candidates.AddRange(RaceTracks.Select(track => (track.Key, TrackCategory.RaceTrack)));
            candidates.AddRange(AdventureTracks.Select(track => (track.Key, TrackCategory.StreetAdventure)));
            if (customTracks != null)
                candidates.AddRange(customTracks.Select(file => (file, TrackCategory.CustomTrack)));

            if (candidates.Count == 0)
                return (RaceTracks[0].Key, TrackCategory.RaceTrack);

            var pick = candidates[Algorithm.RandomInt(candidates.Count)];
            return pick;
        }
    }
}

