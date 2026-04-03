using System;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Race
        {
            public void UpdateCompletions(float deltaSeconds)
            {
                foreach (var room in _owner._rooms.Values)
                {
                    if (!room.RaceStarted)
                        continue;

                    UpdateStopState(room, deltaSeconds);
                }
            }

            public void UpdateStopState(RaceRoom room, float deltaSeconds)
            {
                if (room == null || room.RaceState != RoomRaceState.Racing)
                    return;

                if (RaceServer.GetRaceDistance(room) <= 0f)
                {
                    Abort(room, RoomRaceAbortReason.InvalidTrack);
                    return;
                }

                if (room.ActiveRaceParticipantIds.Count == 0)
                {
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Race completion ready: room={0}, no active participants remain."),
                        room.Id));
                    Stop(room);
                    return;
                }

                foreach (var id in room.ActiveRaceParticipantIds)
                {
                    if (!room.RaceParticipantResults.TryGetValue(id, out var result) || result.Status != RoomRaceResultStatus.Finished)
                        return;
                }

                _owner._logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Race completion ready: room={0}, all active participants finished."),
                    room.Id));
                Stop(room);
            }

            public int CaptureFinishTimeMs(RaceRoom room)
            {
                if (room.RaceStartedUtc == default(DateTime))
                    return 0;

                var elapsed = DateTime.UtcNow - room.RaceStartedUtc;
                if (elapsed <= TimeSpan.Zero)
                    return 0;

                var millis = (int)Math.Round(elapsed.TotalMilliseconds);
                return Math.Max(0, millis);
            }

            public bool TryMarkParticipantFinished(RaceRoom room, uint playerId, byte playerNumber, int finishTimeMs, out byte finishOrder)
            {
                finishOrder = 0;
                if (room == null || room.RaceState != RoomRaceState.Racing)
                    return false;

                if (!room.RaceParticipantResults.TryGetValue(playerId, out var result))
                {
                    result = new RoomRaceParticipantResult
                    {
                        PlayerId = playerId,
                        PlayerNumber = playerNumber,
                        Status = RoomRaceResultStatus.Pending,
                        TimeMs = 0,
                        FinishOrder = 0
                    };
                    room.RaceParticipantResults[playerId] = result;
                }

                if (result.Status == RoomRaceResultStatus.Finished)
                {
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Duplicate finish ignored: room={0}, player={1}, number={2}."),
                        room.Id,
                        playerId,
                        playerNumber));
                    return false;
                }

                var order = 1;
                foreach (var entry in room.RaceParticipantResults.Values)
                {
                    if (entry.Status == RoomRaceResultStatus.Finished)
                        order++;
                }

                result.PlayerNumber = playerNumber;
                result.Status = RoomRaceResultStatus.Finished;
                result.TimeMs = Math.Max(0, finishTimeMs);
                result.FinishOrder = (byte)Math.Min(order, byte.MaxValue);
                finishOrder = result.FinishOrder;

                if (!room.RaceResults.Contains(playerNumber))
                    room.RaceResults.Add(playerNumber);

                room.RaceFinishTimesMs[playerNumber] = Math.Max(0, finishTimeMs);
                return true;
            }

            public void MarkParticipantDnf(RaceRoom room, uint playerId, byte playerNumber)
            {
                if (room == null)
                    return;
                if (!room.RaceParticipantResults.TryGetValue(playerId, out var result))
                    return;
                if (result.Status == RoomRaceResultStatus.Finished)
                    return;

                result.PlayerNumber = playerNumber;
                result.Status = RoomRaceResultStatus.Dnf;
                result.TimeMs = 0;
                result.FinishOrder = 0;
            }

            public void FinalizeUnresolvedParticipantsAsDnf(RaceRoom room)
            {
                foreach (var result in room.RaceParticipantResults.Values)
                {
                    if (result.Status == RoomRaceResultStatus.Pending || result.Status == RoomRaceResultStatus.None)
                    {
                        result.Status = RoomRaceResultStatus.Dnf;
                        result.TimeMs = 0;
                        result.FinishOrder = 0;
                    }
                }
            }

            public void InitializeParticipants(RaceRoom room)
            {
                room.RaceParticipantResults.Clear();
                room.RaceResults.Clear();
                room.RaceFinishTimesMs.Clear();

                foreach (var id in room.ActiveRaceParticipantIds)
                {
                    if (_owner._players.TryGetValue(id, out var player))
                    {
                        room.RaceParticipantResults[id] = new RoomRaceParticipantResult
                        {
                            PlayerId = id,
                            PlayerNumber = player.PlayerNumber,
                            Status = RoomRaceResultStatus.Pending
                        };
                        continue;
                    }

                    if (TryGetActiveBot(room, id, out var bot))
                    {
                        room.RaceParticipantResults[id] = new RoomRaceParticipantResult
                        {
                            PlayerId = id,
                            PlayerNumber = bot.PlayerNumber,
                            Status = RoomRaceResultStatus.Pending
                        };
                    }
                }
            }

            private static bool TryGetActiveBot(RaceRoom room, uint botId, out RoomBot bot)
            {
                for (var i = 0; i < room.Bots.Count; i++)
                {
                    var candidate = room.Bots[i];
                    if (candidate.Id != botId)
                        continue;
                    bot = candidate;
                    return true;
                }

                bot = null!;
                return false;
            }
        }
    }
}
