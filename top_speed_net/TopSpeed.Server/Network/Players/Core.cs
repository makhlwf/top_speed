using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void HandlePlayerHello(PlayerConnection player, PacketPlayerHello hello)
        {
            var name = (hello.Name ?? string.Empty).Trim();
            if (name.Length > ProtocolConstants.MaxPlayerNameLength)
                name = name.Substring(0, ProtocolConstants.MaxPlayerNameLength);
            player.Name = name;
            if (!player.ServerPresenceAnnounced)
            {
                player.ServerPresenceAnnounced = true;
                BroadcastServerConnectAnnouncement(player);
            }
            if (player.RoomId.HasValue && _rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                _room.TouchVersion(room);
                _notify.RoomParticipant(
                    room,
                    RoomEventKind.ParticipantStateChanged,
                    player.Id,
                    player.PlayerNumber,
                    player.State,
                    string.IsNullOrWhiteSpace(player.Name)
                        ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1)
                        : player.Name);
            }
        }
    }
}
