using System;
using System.Collections.Generic;
using TopSpeed.Data;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private static string[] BuildNumericOptions(int min, int max, string suffix)
        {
            if (max < min)
                return Array.Empty<string>();

            var options = new string[max - min + 1];
            for (var i = min; i <= max; i++)
            {
                var index = i - min;
                var unit = i == 1
                    ? suffix.TrimEnd('s')
                    : suffix;
                options[index] = $"{i} {unit}";
            }

            return options;
        }

        private static TrackInfo[] BuildRoomTrackOptions()
        {
            var tracks = new List<TrackInfo>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var track in TrackList.RaceTracks)
            {
                if (names.Add(track.Display))
                    tracks.Add(track);
            }

            foreach (var track in TrackList.AdventureTracks)
            {
                if (names.Add(track.Display))
                    tracks.Add(track);
            }

            return tracks.ToArray();
        }

        private static string[] BuildRoomTrackLabels()
        {
            if (RoomTrackOptions.Length == 0)
                return new[] { "America" };

            var labels = new string[RoomTrackOptions.Length];
            for (var i = 0; i < RoomTrackOptions.Length; i++)
            {
                var track = RoomTrackOptions[i];
                labels[i] = track.Display;
            }

            return labels;
        }

        private bool TrySend(bool sent, string action)
        {
            if (sent)
                return true;

            _speech.Speak($"Failed to send {action}. Please check your connection.");
            return false;
        }
    }
}
