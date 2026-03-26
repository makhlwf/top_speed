using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private const float RaceStopGraceSeconds = 1.0f;
        private const float FinishDistanceToleranceM = 0.01f;

        private void UpdateRaceCompletions(float deltaSeconds)
        {
            foreach (var room in _rooms.Values)
            {
                if (!room.RaceStarted)
                    continue;

                UpdateRaceStopState(room, deltaSeconds);
            }
        }

        private void UpdateRaceStopState(RaceRoom room, float deltaSeconds)
        {
            var raceDistance = GetRaceDistance(room);
            if (raceDistance <= 0f)
            {
                room.RaceStopPending = false;
                room.RaceStopDelaySeconds = 0f;
                return;
            }

            if (!AreAllParticipantsFinishedAtLine(room, raceDistance))
            {
                room.RaceStopPending = false;
                room.RaceStopDelaySeconds = 0f;
                return;
            }

            if (!room.RaceStopPending)
            {
                room.RaceStopPending = true;
                room.RaceStopDelaySeconds = RaceStopGraceSeconds;
                return;
            }

            room.RaceStopDelaySeconds -= Math.Max(0f, deltaSeconds);
            if (room.RaceStopDelaySeconds <= 0f)
                StopRace(room);
        }

        private bool AreAllParticipantsFinishedAtLine(RaceRoom room, float raceDistance)
        {
            var participantCount = 0;

            foreach (var id in room.PlayerIds)
            {
                if (!_players.TryGetValue(id, out var player))
                    continue;

                participantCount++;
                if (player.State != PlayerState.Finished)
                    return false;
                if (!IsAtFinishDistance(player.PositionY, raceDistance))
                    return false;
            }

            for (var i = 0; i < room.Bots.Count; i++)
            {
                var bot = room.Bots[i];
                participantCount++;
                if (bot.State != PlayerState.Finished)
                    return false;
                if (!IsAtFinishDistance(bot.PositionY, raceDistance))
                    return false;
            }

            if (participantCount == 0)
                return false;
            return room.RaceResults.Count >= participantCount;
        }

        private static bool IsAtFinishDistance(float positionY, float raceDistance)
        {
            return positionY + FinishDistanceToleranceM >= raceDistance;
        }

        private int CaptureFinishTimeMs(RaceRoom room)
        {
            if (room.RaceStartedUtc == default(DateTime))
                return 0;

            var elapsed = DateTime.UtcNow - room.RaceStartedUtc;
            if (elapsed <= TimeSpan.Zero)
                return 0;

            var millis = (int)Math.Round(elapsed.TotalMilliseconds);
            return Math.Max(0, millis);
        }

        private void RecordRaceFinish(RaceRoom room, byte playerNumber, int finishTimeMs)
        {
            if (!room.RaceResults.Contains(playerNumber))
                room.RaceResults.Add(playerNumber);

            room.RaceFinishTimesMs[playerNumber] = Math.Max(0, finishTimeMs);
        }
    }
}
