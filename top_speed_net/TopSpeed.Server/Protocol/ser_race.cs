using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Protocol
{
    internal static partial class PacketSerializer
    {
        public static bool TryReadRacePlayerData(byte[] data, out PacketRacePlayerData packet)
        {
            packet = new PacketRacePlayerData();
            if (data.Length < 2 + 4 + 4 + 1 + 1 + 4 + 4 + 2 + 4 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 4)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RaceInstanceId = reader.ReadUInt32();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.Car = (CarType)reader.ReadByte();
            packet.RaceData.PositionX = reader.ReadSingle();
            packet.RaceData.PositionY = reader.ReadSingle();
            packet.RaceData.Speed = reader.ReadUInt16();
            packet.RaceData.Frequency = reader.ReadInt32();
            packet.State = (PlayerState)reader.ReadByte();
            packet.EngineRunning = reader.ReadBool();
            packet.Braking = reader.ReadBool();
            packet.Horning = reader.ReadBool();
            packet.Backfiring = reader.ReadBool();
            packet.MediaLoaded = reader.ReadBool();
            packet.MediaPlaying = reader.ReadBool();
            packet.MediaId = reader.ReadUInt32();
            return true;
        }

        public static byte[] WriteRaceSnapshot(PacketRaceSnapshot snapshot)
        {
            var players = snapshot.Players ?? Array.Empty<PacketPlayerData>();
            var count = Math.Min(players.Length, ProtocolConstants.MaxPlayers);
            var buffer = WritePacketHeader(Command.RaceSnapshot, 4 + 4 + 1 + (count * PlayerDataFieldSize));
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.RaceSnapshot);
            writer.WriteUInt32(snapshot.Sequence);
            writer.WriteUInt32(snapshot.Tick);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
                WritePlayerDataFields(ref writer, players[i]);
            return buffer;
        }

        public static byte[] WritePlayerBumped(PacketPlayerBumped bump)
        {
            var buffer = WritePacketHeader(Command.PlayerBumped, 4 + 1 + 4 + 4 + 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerBumped);
            writer.WriteUInt32(bump.PlayerId);
            writer.WriteByte(bump.PlayerNumber);
            writer.WriteSingle(bump.BumpX);
            writer.WriteSingle(bump.BumpY);
            writer.WriteSingle(bump.SpeedDeltaKph);
            return buffer;
        }

        public static byte[] WriteRaceResults(PacketRaceResults results)
        {
            var count = Math.Min(results.Results.Length, ProtocolConstants.MaxPlayers);
            var payload = 1 + (count * 5);
            var buffer = WritePacketHeader(Command.StopRace, payload);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.StopRace);
            writer.WriteByte((byte)count);
            for (var i = 0; i < count; i++)
            {
                writer.WriteByte(results.Results[i].PlayerNumber);
                writer.WriteInt32(results.Results[i].TimeMs);
            }
            return buffer;
        }
    }
}
