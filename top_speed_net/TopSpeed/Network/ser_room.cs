using System;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        public static bool TryReadPlayerJoined(byte[] data, out PacketPlayerJoined packet)
        {
            packet = new PacketPlayerJoined();
            if (data.Length < 2 + 4 + 1 + ProtocolConstants.MaxPlayerNameLength)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.PlayerJoined)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.Name = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength);
            return true;
        }

        public static bool TryReadRoomList(byte[] data, out PacketRoomList packet)
        {
            packet = new PacketRoomList();
            if (data.Length < 2 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomList)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            var count = reader.ReadByte();
            var stride = 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 12;
            var available = (data.Length - 3) / stride;
            var actualCount = Math.Min(count, available);
            var rooms = new PacketRoomSummary[actualCount];
            for (var i = 0; i < actualCount; i++)
            {
                rooms[i] = new PacketRoomSummary
                {
                    RoomId = reader.ReadUInt32(),
                    RoomName = reader.ReadFixedString(ProtocolConstants.MaxRoomNameLength),
                    RoomType = (GameRoomType)reader.ReadByte(),
                    PlayerCount = reader.ReadByte(),
                    PlayersToStart = reader.ReadByte(),
                    RaceStarted = reader.ReadBool(),
                    TrackName = reader.ReadFixedString(12)
                };
            }
            packet.Rooms = rooms;
            return true;
        }

        public static bool TryReadRoomState(byte[] data, out PacketRoomState packet)
        {
            packet = new PacketRoomState();
            if (data.Length < 2 + 4 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 1 + 1 + 12 + 1 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomState)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RoomVersion = reader.ReadUInt32();
            packet.RoomId = reader.ReadUInt32();
            packet.HostPlayerId = reader.ReadUInt32();
            packet.RoomName = reader.ReadFixedString(ProtocolConstants.MaxRoomNameLength);
            packet.RoomType = (GameRoomType)reader.ReadByte();
            packet.PlayersToStart = reader.ReadByte();
            packet.InRoom = reader.ReadBool();
            packet.IsHost = reader.ReadBool();
            packet.RaceStarted = reader.ReadBool();
            packet.PreparingRace = reader.ReadBool();
            packet.TrackName = reader.ReadFixedString(12);
            packet.Laps = reader.ReadByte();
            var count = reader.ReadByte();
            var stride = 4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength;
            var available = Math.Max(0, (data.Length - (2 + 4 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 1 + 1 + 12 + 1 + 1)) / stride);
            var actualCount = Math.Min(count, available);
            var players = new PacketRoomPlayer[actualCount];
            for (var i = 0; i < actualCount; i++)
            {
                players[i] = new PacketRoomPlayer
                {
                    PlayerId = reader.ReadUInt32(),
                    PlayerNumber = reader.ReadByte(),
                    State = (PlayerState)reader.ReadByte(),
                    Name = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength)
                };
            }
            packet.Players = players;
            return true;
        }

        public static bool TryReadRoomEvent(byte[] data, out PacketRoomEvent packet)
        {
            packet = new PacketRoomEvent();
            if (data.Length < 2 + 4 + 4 + 1 + 4 + 1 + 1 + 1 + 1 + 1 + 12 + 1 + ProtocolConstants.MaxRoomNameLength + 4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomEvent)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RoomId = reader.ReadUInt32();
            packet.RoomVersion = reader.ReadUInt32();
            packet.Kind = (RoomEventKind)reader.ReadByte();
            packet.HostPlayerId = reader.ReadUInt32();
            packet.RoomType = (GameRoomType)reader.ReadByte();
            packet.PlayerCount = reader.ReadByte();
            packet.PlayersToStart = reader.ReadByte();
            packet.RaceStarted = reader.ReadBool();
            packet.PreparingRace = reader.ReadBool();
            packet.TrackName = reader.ReadFixedString(12);
            packet.Laps = reader.ReadByte();
            packet.RoomName = reader.ReadFixedString(ProtocolConstants.MaxRoomNameLength);
            packet.SubjectPlayerId = reader.ReadUInt32();
            packet.SubjectPlayerNumber = reader.ReadByte();
            packet.SubjectPlayerState = (PlayerState)reader.ReadByte();
            packet.SubjectPlayerName = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength);
            return true;
        }

        public static bool TryReadRoomGet(byte[] data, out PacketRoomGet packet)
        {
            packet = new PacketRoomGet();
            if (data.Length < 2 + 1 + 4 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 12 + 1 + 1)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.RoomGet)
                return false;

            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Found = reader.ReadBool();
            packet.RoomVersion = reader.ReadUInt32();
            packet.RoomId = reader.ReadUInt32();
            packet.HostPlayerId = reader.ReadUInt32();
            packet.RoomName = reader.ReadFixedString(ProtocolConstants.MaxRoomNameLength);
            packet.RoomType = (GameRoomType)reader.ReadByte();
            packet.PlayersToStart = reader.ReadByte();
            packet.RaceStarted = reader.ReadBool();
            packet.PreparingRace = reader.ReadBool();
            packet.TrackName = reader.ReadFixedString(12);
            packet.Laps = reader.ReadByte();
            var count = reader.ReadByte();
            var stride = 4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength;
            var available = Math.Max(0, (data.Length - (2 + 1 + 4 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 12 + 1 + 1)) / stride);
            var actualCount = Math.Min(count, available);
            var players = new PacketRoomPlayer[actualCount];
            for (var i = 0; i < actualCount; i++)
            {
                players[i] = new PacketRoomPlayer
                {
                    PlayerId = reader.ReadUInt32(),
                    PlayerNumber = reader.ReadByte(),
                    State = (PlayerState)reader.ReadByte(),
                    Name = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength)
                };
            }

            packet.Players = players;
            return true;
        }

        public static byte[] WriteRoomListRequest()
        {
            return WriteGeneral(Command.RoomListRequest);
        }

        public static byte[] WriteRoomStateRequest()
        {
            return WriteGeneral(Command.RoomStateRequest);
        }

        public static byte[] WriteRoomGetRequest(uint roomId)
        {
            var buffer = WritePacketHeader(Command.RoomGetRequest, 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomGetRequest);
            writer.WriteUInt32(roomId);
            return buffer;
        }

        public static byte[] WriteRoomCreate(string roomName, GameRoomType roomType, byte playersToStart)
        {
            var buffer = WritePacketHeader(Command.RoomCreate, ProtocolConstants.MaxRoomNameLength + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomCreate);
            writer.WriteFixedString(roomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteByte((byte)roomType);
            writer.WriteByte(playersToStart);
            return buffer;
        }

        public static byte[] WriteRoomJoin(uint roomId)
        {
            var buffer = WritePacketHeader(Command.RoomJoin, 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomJoin);
            writer.WriteUInt32(roomId);
            return buffer;
        }

        public static byte[] WriteRoomLeave()
        {
            return WriteGeneral(Command.RoomLeave);
        }

        public static byte[] WriteRoomSetTrack(string trackName)
        {
            var buffer = WritePacketHeader(Command.RoomSetTrack, 12);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomSetTrack);
            writer.WriteFixedString(trackName ?? string.Empty, 12);
            return buffer;
        }

        public static byte[] WriteRoomSetLaps(byte laps)
        {
            var buffer = WritePacketHeader(Command.RoomSetLaps, 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomSetLaps);
            writer.WriteByte(laps);
            return buffer;
        }

        public static byte[] WriteRoomStartRace()
        {
            return WriteGeneral(Command.RoomStartRace);
        }

        public static byte[] WriteRoomSetPlayersToStart(byte playersToStart)
        {
            var buffer = WritePacketHeader(Command.RoomSetPlayersToStart, 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomSetPlayersToStart);
            writer.WriteByte(playersToStart);
            return buffer;
        }

        public static byte[] WriteRoomAddBot()
        {
            return WriteGeneral(Command.RoomAddBot);
        }

        public static byte[] WriteRoomRemoveBot()
        {
            return WriteGeneral(Command.RoomRemoveBot);
        }

        public static byte[] WriteRoomPlayerReady(CarType car, bool automaticTransmission)
        {
            var buffer = WritePacketHeader(Command.RoomPlayerReady, 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomPlayerReady);
            writer.WriteByte((byte)car);
            writer.WriteBool(automaticTransmission);
            return buffer;
        }

        public static byte[] WriteRoomEvent(PacketRoomEvent evt)
        {
            var payload = 4 + 4 + 1 + 4 + 1 + 1 + 1 + 1 + 1 + 12 + 1 +
                ProtocolConstants.MaxRoomNameLength + 4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength;
            var buffer = WritePacketHeader(Command.RoomEvent, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomEvent);
            writer.WriteUInt32(evt.RoomId);
            writer.WriteUInt32(evt.RoomVersion);
            writer.WriteByte((byte)evt.Kind);
            writer.WriteUInt32(evt.HostPlayerId);
            writer.WriteByte((byte)evt.RoomType);
            writer.WriteByte(evt.PlayerCount);
            writer.WriteByte(evt.PlayersToStart);
            writer.WriteBool(evt.RaceStarted);
            writer.WriteBool(evt.PreparingRace);
            writer.WriteFixedString(evt.TrackName ?? string.Empty, 12);
            writer.WriteByte(evt.Laps);
            writer.WriteFixedString(evt.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteUInt32(evt.SubjectPlayerId);
            writer.WriteByte(evt.SubjectPlayerNumber);
            writer.WriteByte((byte)evt.SubjectPlayerState);
            writer.WriteFixedString(evt.SubjectPlayerName ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            return buffer;
        }

        public static byte[] WriteRoomGet(PacketRoomGet packet)
        {
            var count = Math.Min(packet.Players.Length, ProtocolConstants.MaxPlayers);
            var payload = 1 + 4 + 4 + 4 + ProtocolConstants.MaxRoomNameLength + 1 + 1 + 1 + 1 + 12 + 1 + 1 +
                (count * (4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength));
            var buffer = WritePacketHeader(Command.RoomGet, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RoomGet);
            writer.WriteBool(packet.Found);
            writer.WriteUInt32(packet.RoomVersion);
            writer.WriteUInt32(packet.RoomId);
            writer.WriteUInt32(packet.HostPlayerId);
            writer.WriteFixedString(packet.RoomName ?? string.Empty, ProtocolConstants.MaxRoomNameLength);
            writer.WriteByte((byte)packet.RoomType);
            writer.WriteByte(packet.PlayersToStart);
            writer.WriteBool(packet.RaceStarted);
            writer.WriteBool(packet.PreparingRace);
            writer.WriteFixedString(packet.TrackName ?? string.Empty, 12);
            writer.WriteByte(packet.Laps);
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
    }
}
