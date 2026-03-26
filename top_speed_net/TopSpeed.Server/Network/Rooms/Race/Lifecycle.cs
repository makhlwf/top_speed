using System;
using System.Linq;
using LiteNetLib;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;
using TopSpeed.Server.Tracks;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void StartRace(RaceRoom room)
        {
            if (room.RaceStarted)
                return;

            var activePlayerIds = room.PlayerIds
                .Where(id => room.PendingLoadouts.ContainsKey(id))
                .ToList();

            room.PreparingRace = false;

            if (!room.TrackSelected || room.TrackData == null)
                SetTrack(room, room.TrackName);

            room.RaceStarted = true;
            room.RaceResults.Clear();
            room.RaceFinishTimesMs.Clear();
            room.RaceStartedUtc = DateTime.UtcNow;
            room.RaceStopPending = false;
            room.RaceStopDelaySeconds = 0f;
            room.ActiveBumpPairs.Clear();
            room.RaceSnapshotSequence = 0;
            room.RaceSnapshotTick = 0;
            var laneHalfWidth = GetLaneHalfWidth(room);
            var rowSpacing = GetStartRowSpacing(room);
            foreach (var id in room.PlayerIds)
            {
                if (_players.TryGetValue(id, out var p))
                {
                    if (activePlayerIds.Contains(id))
                    {
                        p.State = PlayerState.AwaitingStart;
                        p.PositionX = CalculateStartX(p.PlayerNumber, p.WidthM, laneHalfWidth);
                        p.PositionY = CalculateStartY(p.PlayerNumber, rowSpacing);
                        p.Speed = 0;
                        p.Frequency = ProtocolConstants.DefaultFrequency;
                        p.EngineRunning = false;
                        p.Braking = false;
                        p.Horning = false;
                        p.Backfiring = false;
                    }
                    else
                    {
                        p.State = PlayerState.NotReady;
                    }
                }
            }
            foreach (var bot in room.Bots)
            {
                bot.State = PlayerState.AwaitingStart;
                bot.RacePhase = BotRacePhase.Normal;
                bot.CrashRecoverySeconds = 0f;
                bot.SpeedKph = 0f;
                bot.StartDelaySeconds = BotRaceStartDelaySeconds + GetBotReactionDelay(bot.Difficulty);
                bot.EngineStartSecondsRemaining = 0f;
                bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                bot.Horning = false;
                bot.HornSecondsRemaining = 0f;
                bot.BackfireArmed = true;
                bot.BackfirePulseSeconds = 0f;
                bot.PositionX = CalculateStartX(bot.PlayerNumber, bot.WidthM, laneHalfWidth);
                bot.PositionY = CalculateStartY(bot.PlayerNumber, rowSpacing);
                bot.PhysicsState = new BotPhysicsState
                {
                    PositionX = bot.PositionX,
                    PositionY = bot.PositionY,
                    SpeedKph = 0f,
                    Gear = 1,
                    AutoShiftCooldownSeconds = 0f
                };
            }

            SendTrackToRoom(room);
            var startPayload = PacketSerializer.WriteGeneral(Command.StartRace);
            foreach (var id in activePlayerIds)
            {
                if (_players.TryGetValue(id, out var player))
                    SendStream(player, startPayload, PacketStream.RaceEvent);
            }
            SendRaceSnapshot(room, DeliveryMethod.ReliableOrdered);
            TouchRoomVersion(room);
            EmitRoomLifecycleEvent(room, RoomEventKind.RaceStarted);
            EmitRoomLifecycleEvent(room, RoomEventKind.RoomSummaryUpdated);
            _logger.Info(LocalizationService.Format(
                LocalizationService.Mark("Race started: room={0} \"{1}\", track={2}, laps={3}, humans={4}, bots={5}."),
                room.Id,
                room.Name,
                room.TrackName,
                room.Laps,
                activePlayerIds.Count,
                room.Bots.Count));
            room.PendingLoadouts.Clear();
            room.PrepareSkips.Clear();
        }

        private void StopRace(RaceRoom room)
        {
            room.RaceStarted = false;
            room.PreparingRace = false;
            room.PendingLoadouts.Clear();
            room.PrepareSkips.Clear();
            room.ActiveBumpPairs.Clear();

            var order = room.RaceResults.ToArray();
            var results = new PacketRaceResultEntry[order.Length];
            for (var i = 0; i < order.Length; i++)
            {
                var playerNumber = order[i];
                var timeMs = 0;
                if (room.RaceFinishTimesMs.TryGetValue(playerNumber, out var savedTime))
                    timeMs = Math.Max(0, savedTime);

                results[i] = new PacketRaceResultEntry
                {
                    PlayerNumber = playerNumber,
                    TimeMs = timeMs
                };
            }
            SendToRoomOnStream(room, PacketSerializer.WriteRaceResults(new PacketRaceResults
            {
                NPlayers = (byte)Math.Min(results.Length, ProtocolConstants.MaxPlayers),
                Results = results
            }), PacketStream.RaceEvent);

            room.RaceResults.Clear();
            room.RaceFinishTimesMs.Clear();
            room.RaceStartedUtc = default(DateTime);
            room.RaceStopPending = false;
            room.RaceStopDelaySeconds = 0f;
            room.RaceSnapshotSequence = 0;
            room.RaceSnapshotTick = 0;
            foreach (var id in room.PlayerIds)
            {
                if (_players.TryGetValue(id, out var p))
                    p.State = PlayerState.NotReady;
            }
            foreach (var bot in room.Bots)
            {
                bot.State = PlayerState.NotReady;
                bot.RacePhase = BotRacePhase.Normal;
                bot.CrashRecoverySeconds = 0f;
                bot.SpeedKph = 0f;
                bot.StartDelaySeconds = 0f;
                bot.EngineStartSecondsRemaining = 0f;
                bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                bot.Horning = false;
                bot.HornSecondsRemaining = 0f;
                bot.BackfireArmed = true;
                bot.BackfirePulseSeconds = 0f;
                bot.PhysicsState = new BotPhysicsState
                {
                    PositionX = bot.PositionX,
                    PositionY = bot.PositionY,
                    SpeedKph = 0f,
                    Gear = 1,
                    AutoShiftCooldownSeconds = 0f
                };
            }

            TouchRoomVersion(room);
            EmitRoomLifecycleEvent(room, RoomEventKind.RaceStopped);
            EmitRoomLifecycleEvent(room, RoomEventKind.RoomSummaryUpdated);
            _logger.Info(LocalizationService.Format(
                LocalizationService.Mark("Race stopped: room={0} \"{1}\", results={2}."),
                room.Id,
                room.Name,
                string.Join(",", order)));
        }

    }
}
