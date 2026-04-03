using System;
using System.Collections.Generic;
using TopSpeed.Localization;

namespace TopSpeed.Race
{
    internal sealed partial class SingleRaceMode
    {
        private const float BotSettledSpeedKph = 0.5f;

        private int ReadCurrentRaceTimeMs()
        {
            return Math.Max(0, (int)(_stopwatch.ElapsedMilliseconds - _stopwatchDiffMs));
        }

        private void RecordFinish(int playerNumber, int timeMs)
        {
            if (!_finishOrder.Contains(playerNumber))
                _finishOrder.Add(playerNumber);
            _finishTimesMs[playerNumber] = Math.Max(0, timeMs);
        }

        private void EnsureAllFinishEntries()
        {
            for (var playerNumber = 0; playerNumber <= _nComputerPlayers; playerNumber++)
            {
                if (_finishTimesMs.ContainsKey(playerNumber))
                    continue;

                _finishTimesMs[playerNumber] = ReadCurrentRaceTimeMs();
                if (!_finishOrder.Contains(playerNumber))
                    _finishOrder.Add(playerNumber);
            }
        }

        private RaceResultSummary BuildResultSummary()
        {
            EnsureAllFinishEntries();

            var entries = new List<RaceResultEntry>(_finishOrder.Count);
            var localPosition = 1;
            for (var i = 0; i < _finishOrder.Count; i++)
            {
                var playerNumber = _finishOrder[i];
                var position = i + 1;
                if (playerNumber == _playerNumber)
                    localPosition = position;

                entries.Add(new RaceResultEntry
                {
                    Name = LocalizationService.Format(LocalizationService.Mark("player {0}"), playerNumber + 1),
                    Position = position,
                    TimeMs = _finishTimesMs.TryGetValue(playerNumber, out var timeMs) ? timeMs : 0,
                    IsLocalPlayer = playerNumber == _playerNumber
                });
            }

            return new RaceResultSummary
            {
                IsMultiplayer = false,
                LocalPosition = localPosition,
                Entries = entries.ToArray()
            };
        }

        protected override void OnRaceFinishEvent()
        {
            SetResultSummary(BuildResultSummary());
            base.OnRaceFinishEvent();
        }

        protected override bool AreVehiclesSettledForExit()
        {
            if (!base.AreVehiclesSettledForExit())
                return false;

            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot == null || !bot.Finished)
                    continue;
                if (bot.Speed > BotSettledSpeedKph)
                    return false;
            }

            return true;
        }
    }
}

