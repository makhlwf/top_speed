using System;
using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Localization;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private static string[] BuildNumericOptions(int min, int max, string singularUnit, string pluralUnit)
        {
            if (max < min)
                return Array.Empty<string>();

            var options = new string[max - min + 1];
            for (var i = min; i <= max; i++)
            {
                var index = i - min;
                var unit = i == 1 ? singularUnit : pluralUnit;
                options[index] = i + " " + LocalizationService.Translate(unit);
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

        private bool TrySend(bool sent, string action)
        {
            if (sent)
                return true;

            _speech.Speak(
                LocalizationService.Format(
                    LocalizationService.Mark("Failed to send {0}. Please check your connection."),
                    action));
            return false;
        }

        internal string ResolvePlayerName(byte playerNumber)
        {
            var players = _state.Rooms.CurrentRoom.Players ?? Array.Empty<RoomParticipant>();
            for (var i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (player == null || player.PlayerNumber != playerNumber)
                    continue;
                if (!string.IsNullOrWhiteSpace(player.Name))
                    return player.Name.Trim();
                break;
            }

            return LocalizationService.Format(LocalizationService.Mark("Player {0}"), playerNumber + 1);
        }
    }
}
