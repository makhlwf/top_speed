using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Race
{
    internal sealed partial class MultiplayerMode
    {
        private const float RemoteSettledSpeedKph = 0.5f;

        private RaceResultSummary BuildResultSummary(PacketRaceResults packet)
        {
            var source = packet?.Results ?? System.Array.Empty<PacketRaceResultEntry>();
            var entries = new List<RaceResultEntry>(source.Length > 0 ? source.Length : 1);
            var localPosition = 1;

            for (var i = 0; i < source.Length; i++)
            {
                var result = source[i];
                var position = i + 1;
                var isLocal = result.PlayerNumber == _playerNumber;
                if (isLocal)
                    localPosition = position;

                var name = _resolvePlayerName(result.PlayerNumber);
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = LocalizationService.Format(
                        LocalizationService.Mark("Player {0}"),
                        result.PlayerNumber + 1);
                }

                entries.Add(new RaceResultEntry
                {
                    Name = name,
                    Position = position,
                    TimeMs = result.TimeMs < 0 ? 0 : result.TimeMs,
                    IsLocalPlayer = isLocal
                });
            }

            if (entries.Count == 0)
            {
                entries.Add(new RaceResultEntry
                {
                    Name = _resolvePlayerName(_playerNumber),
                    Position = 1,
                    TimeMs = _raceTime < 0 ? 0 : _raceTime,
                    IsLocalPlayer = true
                });
            }

            return new RaceResultSummary
            {
                IsMultiplayer = true,
                LocalPosition = localPosition,
                Entries = entries.ToArray()
            };
        }

        protected override bool AreVehiclesSettledForExit()
        {
            if (!base.AreVehiclesSettledForExit())
                return false;

            foreach (var remote in _remotePlayers.Values)
            {
                if (remote == null || !remote.Finished)
                    continue;
                if (remote.Player.Speed > RemoteSettledSpeedKph)
                    return false;
            }

            return true;
        }
    }
}
