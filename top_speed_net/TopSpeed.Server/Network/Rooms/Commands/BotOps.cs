using System;
using System.Linq;
using LiteNetLib;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;
using TopSpeed.Server.Tracks;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void HandleAddBot(PlayerConnection player)
        {
            if (!TryGetHostedRoom(player, out var room))
                return;
            if (room.RaceStarted || room.PreparingRace)
            {
                _roomMutationDenied++;
                _logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Room add-bot denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                    room.Id,
                    player.Id,
                    room.RaceStarted,
                    room.PreparingRace));
                SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot add bots while race setup or race is active."));
                return;
            }

            if (room.RoomType != GameRoomType.BotsRace)
            {
                SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Bots can only be added in race-with-bots rooms."));
                return;
            }

            if (GetRoomParticipantCount(room) >= room.PlayersToStart)
            {
                SendProtocolMessage(player, ProtocolMessageCode.RoomFull, LocalizationService.Mark("This game room is unavailable because it is full."));
                return;
            }

            var bot = CreateBot(room);
            room.Bots.Add(bot);
            CompactRoomNumbers(room);
            TouchRoomVersion(room);
            EmitRoomParticipantEvent(room, RoomEventKind.BotAdded, bot.Id, bot.PlayerNumber, bot.State, FormatBotDisplayName(bot));
            EmitRoomLifecycleEvent(room, RoomEventKind.RoomSummaryUpdated);
            SendToRoomOnStream(room, PacketSerializer.WritePlayerJoined(new PacketPlayerJoined
            {
                PlayerId = bot.Id,
                PlayerNumber = bot.PlayerNumber,
                Name = FormatBotJoinName(bot)
            }), PacketStream.Room);
            if (room.PreparingRace)
                TryStartRaceAfterLoadout(room);
        }

        private void HandleRemoveBot(PlayerConnection player)
        {
            if (!TryGetHostedRoom(player, out var room))
                return;
            if (room.RaceStarted || room.PreparingRace)
            {
                _roomMutationDenied++;
                _logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Room remove-bot denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                    room.Id,
                    player.Id,
                    room.RaceStarted,
                    room.PreparingRace));
                SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot remove bots while race setup or race is active."));
                return;
            }

            if (room.RoomType != GameRoomType.BotsRace)
            {
                SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Bots can only be removed in race-with-bots rooms."));
                return;
            }

            if (room.Bots.Count == 0)
            {
                SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("There are no bots to remove."));
                return;
            }

            var bot = room.Bots.OrderByDescending(b => b.AddedOrder).First();
            room.Bots.Remove(bot);
            CompactRoomNumbers(room);
            SendToRoomOnStream(room, PacketSerializer.WritePlayer(Command.PlayerDisconnected, bot.Id, bot.PlayerNumber), PacketStream.Room);
            TouchRoomVersion(room);
            EmitRoomParticipantEvent(room, RoomEventKind.BotRemoved, bot.Id, bot.PlayerNumber, bot.State, FormatBotDisplayName(bot));
            EmitRoomLifecycleEvent(room, RoomEventKind.RoomSummaryUpdated);
            SendProtocolMessage(
                player,
                ProtocolMessageCode.Ok,
                LocalizationService.Format(
                    LocalizationService.Mark("Removed bot {0}."),
                    bot.Name));
            if (room.RaceStarted)
                UpdateRaceStopState(room, 0f);
            if (room.PreparingRace)
                TryStartRaceAfterLoadout(room);
        }

    }
}
