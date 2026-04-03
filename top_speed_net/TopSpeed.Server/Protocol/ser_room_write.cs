using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Protocol
{
    internal static partial class PacketSerializer
    {
        public static byte[] WriteLoadCustomTrack(PacketLoadCustomTrack track)
        {
            var maxLength = Math.Min(track.TrackLength, (ushort)ProtocolConstants.MaxMultiTrackLength);
            var definitionCount = Math.Min(track.Definitions.Length, maxLength);
            var payload = 1 + 12 + 1 + 1 + 2 + (definitionCount * (1 + 1 + 1 + 4));
            var buffer = WritePacketHeader(Command.LoadCustomTrack, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.LoadCustomTrack);
            writer.WriteByte(track.NrOfLaps);
            writer.WriteFixedString(track.TrackName, 12);
            writer.WriteByte((byte)track.TrackWeather);
            writer.WriteByte((byte)track.TrackAmbience);
            writer.WriteUInt16(maxLength);
            for (var i = 0; i < definitionCount; i++)
            {
                var def = track.Definitions[i];
                writer.WriteByte((byte)def.Type);
                writer.WriteByte((byte)def.Surface);
                writer.WriteByte((byte)def.Noise);
                writer.WriteSingle(def.Length);
            }
            return buffer;
        }

        public static byte[] WritePlayerJoined(PacketPlayerJoined joined)
        {
            var buffer = WritePacketHeader(Command.PlayerJoined, 4 + 1 + ProtocolConstants.MaxPlayerNameLength);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerJoined);
            writer.WriteUInt32(joined.PlayerId);
            writer.WriteByte(joined.PlayerNumber);
            writer.WriteFixedString(joined.Name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            return buffer;
        }

        public static byte[] WriteRoomList(PacketRoomList list)
        {
            var count = Math.Min(list.Rooms.Length, ProtocolConstants.MaxRoomListEntries);
            var payload = 1 + (count * (4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 12));
            var buffer = WritePacketHeader(Command.RoomList, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomList);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                var room = list.Rooms[i];
                writer.WriteUInt32(room.RoomId);
                writer.WriteFixedString(room.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
                writer.WriteByte((byte)room.RoomType);
                writer.WriteByte(room.PlayerCount);
                writer.WriteByte(room.PlayersToStart);
                writer.WriteByte((byte)room.RaceState);
                writer.WriteFixedString(room.TrackName ?? string.Empty, 12);
            }
            return buffer;
        }

        public static byte[] WriteRoomState(PacketRoomState state)
        {
            var count = Math.Min(state.Players.Length, ProtocolConstants.MaxPlayers);
            var payload = 4 + 4 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 1 + 12 + 1 + 4 + 1 +
                (count * (4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength));
            var buffer = WritePacketHeader(Command.RoomState, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomState);
            writer.WriteUInt32(state.RoomVersion);
            writer.WriteUInt32(state.RoomId);
            writer.WriteUInt32(state.RaceInstanceId);
            writer.WriteUInt32(state.HostPlayerId);
            writer.WriteFixedString(state.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteByte((byte)state.RoomType);
            writer.WriteByte(state.PlayersToStart);
            writer.WriteByte((byte)state.RaceState);
            writer.WriteBool(state.InRoom);
            writer.WriteBool(state.IsHost);
            writer.WriteFixedString(state.TrackName ?? string.Empty, 12);
            writer.WriteByte(state.Laps);
            writer.WriteUInt32(state.GameRulesFlags);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                var player = state.Players[i];
                writer.WriteUInt32(player.PlayerId);
                writer.WriteByte(player.PlayerNumber);
                writer.WriteByte((byte)player.State);
                writer.WriteFixedString(player.Name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            }
            return buffer;
        }

        public static byte[] WriteRoomGet(PacketRoomGet packet)
        {
            var count = Math.Min(packet.Players.Length, ProtocolConstants.MaxPlayers);
            var payload = 1 + 4 + 4 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 12 + 1 + 4 + 1 +
                (count * (4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength));
            var buffer = WritePacketHeader(Command.RoomGet, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomGet);
            writer.WriteBool(packet.Found);
            writer.WriteUInt32(packet.RoomVersion);
            writer.WriteUInt32(packet.RoomId);
            writer.WriteUInt32(packet.RaceInstanceId);
            writer.WriteUInt32(packet.HostPlayerId);
            writer.WriteFixedString(packet.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteByte((byte)packet.RoomType);
            writer.WriteByte(packet.PlayersToStart);
            writer.WriteByte((byte)packet.RaceState);
            writer.WriteFixedString(packet.TrackName ?? string.Empty, 12);
            writer.WriteByte(packet.Laps);
            writer.WriteUInt32(packet.GameRulesFlags);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                var player = packet.Players[i];
                writer.WriteUInt32(player.PlayerId);
                writer.WriteByte(player.PlayerNumber);
                writer.WriteByte((byte)player.State);
                writer.WriteFixedString(player.Name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            }

            return buffer;
        }

        public static byte[] WriteRoomEvent(PacketRoomEvent evt)
        {
            var payload = 4 + 4 + 4 + 1 + 4 + 1 + 1 + 1 + 1 + 12 + 1 + 4 +
                ProtocolConstants.MaxRoomNameLength + 4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength;
            var buffer = WritePacketHeader(Command.RoomEvent, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomEvent);
            writer.WriteUInt32(evt.RoomId);
            writer.WriteUInt32(evt.RoomVersion);
            writer.WriteUInt32(evt.RaceInstanceId);
            writer.WriteByte((byte)evt.Kind);
            writer.WriteUInt32(evt.HostPlayerId);
            writer.WriteByte((byte)evt.RoomType);
            writer.WriteByte(evt.PlayerCount);
            writer.WriteByte(evt.PlayersToStart);
            writer.WriteByte((byte)evt.RaceState);
            writer.WriteFixedString(evt.TrackName ?? string.Empty, 12);
            writer.WriteByte(evt.Laps);
            writer.WriteUInt32(evt.GameRulesFlags);
            writer.WriteFixedString(evt.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteUInt32(evt.SubjectPlayerId);
            writer.WriteByte(evt.SubjectPlayerNumber);
            writer.WriteByte((byte)evt.SubjectPlayerState);
            writer.WriteFixedString(evt.SubjectPlayerName ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            return buffer;
        }

        public static byte[] WriteRoomRaceStateChanged(PacketRoomRaceStateChanged packet)
        {
            var payload = 4 + 4 + 4 + 1;
            var buffer = WritePacketHeader(Command.RoomRaceStateChanged, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomRaceStateChanged);
            writer.WriteUInt32(packet.RoomId);
            writer.WriteUInt32(packet.RoomVersion);
            writer.WriteUInt32(packet.RaceInstanceId);
            writer.WriteByte((byte)packet.State);
            return buffer;
        }

        public static byte[] WriteRoomRacePlayerFinished(PacketRoomRacePlayerFinished packet)
        {
            var payload = 4 + 4 + 4 + 1 + 1 + 4;
            var buffer = WritePacketHeader(Command.RoomRacePlayerFinished, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomRacePlayerFinished);
            writer.WriteUInt32(packet.RoomId);
            writer.WriteUInt32(packet.RaceInstanceId);
            writer.WriteUInt32(packet.PlayerId);
            writer.WriteByte(packet.PlayerNumber);
            writer.WriteByte(packet.FinishOrder);
            writer.WriteInt32(packet.TimeMs);
            return buffer;
        }

        public static byte[] WriteRoomRaceCompleted(PacketRoomRaceCompleted packet)
        {
            var count = Math.Min(packet.Results.Length, ProtocolConstants.MaxPlayers);
            var payload = 4 + 4 + 4 + 1 + (count * (4 + 1 + 1 + 4 + 1));
            var buffer = WritePacketHeader(Command.RoomRaceCompleted, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomRaceCompleted);
            writer.WriteUInt32(packet.RoomId);
            writer.WriteUInt32(packet.RoomVersion);
            writer.WriteUInt32(packet.RaceInstanceId);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                var result = packet.Results[i];
                writer.WriteUInt32(result.PlayerId);
                writer.WriteByte(result.PlayerNumber);
                writer.WriteByte(result.FinishOrder);
                writer.WriteInt32(result.TimeMs);
                writer.WriteByte((byte)result.Status);
            }
            return buffer;
        }

        public static byte[] WriteRoomRaceAborted(PacketRoomRaceAborted packet)
        {
            var payload = 4 + 4 + 4 + 1;
            var buffer = WritePacketHeader(Command.RoomRaceAborted, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomRaceAborted);
            writer.WriteUInt32(packet.RoomId);
            writer.WriteUInt32(packet.RoomVersion);
            writer.WriteUInt32(packet.RaceInstanceId);
            writer.WriteByte((byte)packet.Reason);
            return buffer;
        }

        public static byte[] WriteOnlinePlayers(PacketOnlinePlayers packet)
        {
            var count = Math.Min(packet.Players.Length, ProtocolConstants.MaxRoomListEntries);
            var payload = 1 + (count * (4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength + ProtocolConstants.MaxRoomNameLength));
            var buffer = WritePacketHeader(Command.OnlinePlayers, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.OnlinePlayers);
            writer.WriteByte((byte)count);

            for (var i = 0; i < count; i++)
            {
                var player = packet.Players[i];
                writer.WriteUInt32(player.PlayerId);
                writer.WriteByte(player.PlayerNumber);
                writer.WriteByte((byte)player.PresenceState);
                writer.WriteFixedString(player.Name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
                writer.WriteFixedString(player.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            }

            return buffer;
        }

    }
}
