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
    }
}
