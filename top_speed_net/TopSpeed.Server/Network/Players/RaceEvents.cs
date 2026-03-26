using TopSpeed.Protocol;
using TopSpeed.Localization;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void HandlePlayerFinished(PlayerConnection player, PacketPlayer finished)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                _authorityDropsPlayerFinished++;
                return;
            }
            if (!room.RaceStarted)
            {
                _authorityDropsPlayerFinished++;
                return;
            }

            if (finished.PlayerId != player.Id || finished.PlayerNumber != player.PlayerNumber)
            {
                _authorityDropsPlayerFinished++;
                _logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("PlayerFinished payload mismatch: room={0}, connectionPlayer={1}/{2}, payload={3}/{4}."),
                    room.Id,
                    player.Id,
                    player.PlayerNumber,
                    finished.PlayerId,
                    finished.PlayerNumber));
            }

            player.State = PlayerState.Finished;
            var raceDistance = GetRaceDistance(room);
            if (raceDistance > 0f && player.PositionY < raceDistance)
                player.PositionY = raceDistance;

            RecordRaceFinish(room, player.PlayerNumber, CaptureFinishTimeMs(room));

            SendToRoomExceptOnStream(room, player.Id, PacketSerializer.WritePlayer(Command.PlayerFinished, player.Id, player.PlayerNumber), PacketStream.RaceEvent);
            _logger.Debug(LocalizationService.Format(
                LocalizationService.Mark("Player finished: room={0}, player={1}, number={2}, results={3}."),
                room.Id,
                player.Id,
                player.PlayerNumber,
                room.RaceResults.Count));
            UpdateRaceStopState(room, 0f);
        }

        private void HandlePlayerCrashed(PlayerConnection player, PacketPlayer crashed)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                _authorityDropsPlayerCrashed++;
                return;
            }
            if (!room.RaceStarted)
            {
                _authorityDropsPlayerCrashed++;
                return;
            }

            if (crashed.PlayerId != player.Id || crashed.PlayerNumber != player.PlayerNumber)
            {
                _authorityDropsPlayerCrashed++;
                _logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("PlayerCrashed payload mismatch: room={0}, connectionPlayer={1}/{2}, payload={3}/{4}."),
                    room.Id,
                    player.Id,
                    player.PlayerNumber,
                    crashed.PlayerId,
                    crashed.PlayerNumber));
            }

            SendToRoomExceptOnStream(room, player.Id, PacketSerializer.WritePlayer(Command.PlayerCrashed, player.Id, player.PlayerNumber), PacketStream.RaceEvent);
        }

    }
}
