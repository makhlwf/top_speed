using TopSpeed.Network;
using TopSpeed.Protocol;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "Network")]
    public sealed class RaceProtocolSerializationTests
    {
        [Fact]
        public void WriteRacePlayerState_Encodes_Race_Instance_First()
        {
            var payload = ClientPacketSerializer.WriteRacePlayerState(
                Command.PlayerState,
                raceInstanceId: 42u,
                playerId: 7u,
                playerNumber: 3,
                state: PlayerState.Racing);

            Assert.Equal(2 + 4 + 4 + 1 + 1, payload.Length);

            var reader = new PacketReader(payload);
            Assert.Equal(ProtocolConstants.Version, reader.ReadByte());
            Assert.Equal((byte)Command.PlayerState, reader.ReadByte());
            Assert.Equal(42u, reader.ReadUInt32());
            Assert.Equal(7u, reader.ReadUInt32());
            Assert.Equal((byte)3, reader.ReadByte());
            Assert.Equal((byte)PlayerState.Racing, reader.ReadByte());
        }

        [Fact]
        public void WriteRacePlayerDataToServer_Encodes_Race_Instance()
        {
            var payload = ClientPacketSerializer.WriteRacePlayerDataToServer(
                raceInstanceId: 9001u,
                playerId: 11u,
                playerNumber: 2,
                car: CarType.Vehicle4,
                raceData: new PlayerRaceData
                {
                    PositionX = 1.5f,
                    PositionY = 300.25f,
                    Speed = 188,
                    Frequency = 9200
                },
                state: PlayerState.Finished,
                engineRunning: true,
                braking: false,
                horning: true,
                backfiring: false,
                mediaLoaded: true,
                mediaPlaying: true,
                mediaId: 99u);

            Assert.Equal(2 + 35, payload.Length);

            var reader = new PacketReader(payload);
            Assert.Equal(ProtocolConstants.Version, reader.ReadByte());
            Assert.Equal((byte)Command.PlayerDataToServer, reader.ReadByte());
            Assert.Equal(9001u, reader.ReadUInt32());
            Assert.Equal(11u, reader.ReadUInt32());
            Assert.Equal((byte)2, reader.ReadByte());
            Assert.Equal((byte)CarType.Vehicle4, reader.ReadByte());
            Assert.Equal(1.5f, reader.ReadSingle());
            Assert.Equal(300.25f, reader.ReadSingle());
            Assert.Equal((ushort)188, reader.ReadUInt16());
            Assert.Equal(9200, reader.ReadInt32());
            Assert.Equal((byte)PlayerState.Finished, reader.ReadByte());
            Assert.True(reader.ReadBool());
            Assert.False(reader.ReadBool());
            Assert.True(reader.ReadBool());
            Assert.False(reader.ReadBool());
            Assert.True(reader.ReadBool());
            Assert.True(reader.ReadBool());
            Assert.Equal(99u, reader.ReadUInt32());
        }

        [Fact]
        public void RoomEvent_RoundTrip_Uses_RaceState_Without_Legacy_Bools()
        {
            var roomEvent = new PacketRoomEvent
            {
                RoomId = 12,
                RoomVersion = 5,
                RaceInstanceId = 6,
                Kind = RoomEventKind.RoomSummaryUpdated,
                HostPlayerId = 44,
                RoomType = GameRoomType.PlayersRace,
                PlayerCount = 2,
                PlayersToStart = 2,
                RaceState = RoomRaceState.Preparing,
                TrackName = "desert",
                Laps = 3,
                GameRulesFlags = 17,
                RoomName = "room-a",
                SubjectPlayerId = 7,
                SubjectPlayerNumber = 1,
                SubjectPlayerState = PlayerState.AwaitingStart,
                SubjectPlayerName = "alice"
            };

            var payload = ClientPacketSerializer.WriteRoomEvent(roomEvent);
            var expectedPayload =
                4 + 4 + 4 + 1 + 4 + 1 + 1 + 1 + 1 + 12 + 1 + 4 +
                ProtocolConstants.MaxRoomNameLength + 4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength;
            Assert.Equal(2 + expectedPayload, payload.Length);

            Assert.True(ClientPacketSerializer.TryReadRoomEvent(payload, out var parsed));
            Assert.Equal(roomEvent.RoomId, parsed.RoomId);
            Assert.Equal(roomEvent.RaceInstanceId, parsed.RaceInstanceId);
            Assert.Equal(roomEvent.RaceState, parsed.RaceState);
            Assert.Equal(roomEvent.TrackName, parsed.TrackName);
            Assert.Equal(roomEvent.SubjectPlayerId, parsed.SubjectPlayerId);
            Assert.Equal(roomEvent.SubjectPlayerNumber, parsed.SubjectPlayerNumber);
        }
    }
}
