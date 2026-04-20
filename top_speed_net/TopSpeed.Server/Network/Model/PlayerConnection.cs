using System;
using System.Collections.Generic;
using System.Net;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed class PlayerConnection
    {
        public PlayerConnection(IPEndPoint endPoint, uint id)
        {
            EndPoint = endPoint;
            Id = id;
            Frequency = ProtocolConstants.DefaultFrequency;
            State = PlayerState.NotReady;
            Name = string.Empty;
            LastSeenUtc = DateTime.UtcNow;
            WidthM = 1.8f;
            LengthM = 4.5f;
            MassKg = 1500f;
            Handshake = HandshakeState.Pending;
            NegotiatedProtocol = ProtocolProfile.ServerSupported.MaxSupported;
            RadioVolumePercent = 100;
        }

        public IPEndPoint EndPoint { get; }
        public uint Id { get; }
        public uint? RoomId { get; set; }
        public byte PlayerNumber { get; set; }
        public CarType Car { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public ushort Speed { get; set; }
        public int Frequency { get; set; }
        public PlayerState State { get; set; }
        public string Name { get; set; }
        public bool ServerPresenceAnnounced { get; set; }
        public bool EngineRunning { get; set; }
        public bool Braking { get; set; }
        public bool Horning { get; set; }
        public bool Backfiring { get; set; }
        public bool MediaLoaded { get; set; }
        public bool MediaPlaying { get; set; }
        public uint MediaId { get; set; }
        public byte RadioVolumePercent { get; set; }
        public InMedia? IncomingMedia { get; set; }
        public LiveState? Live { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public float WidthM { get; set; }
        public float LengthM { get; set; }
        public float MassKg { get; set; }
        public HandshakeState Handshake { get; set; }
        public ProtocolVer NegotiatedProtocol { get; set; }
        public ProtocolRange? ClientSupportedRange { get; set; }
        public ProtocolVer ClientVersion { get; set; }

        public PacketPlayerData ToPacket()
        {
            return new PacketPlayerData
            {
                PlayerId = Id,
                PlayerNumber = PlayerNumber,
                Car = Car,
                RaceData = new PlayerRaceData
                {
                    PositionX = PositionX,
                    PositionY = PositionY,
                    Speed = Speed,
                    Frequency = Frequency
                },
                State = State,
                EngineRunning = EngineRunning,
                Braking = Braking,
                Horning = Horning,
                Backfiring = Backfiring,
                MediaLoaded = MediaLoaded,
                MediaPlaying = MediaPlaying,
                MediaId = MediaId,
                RadioVolumePercent = RadioVolumePercent
            };
        }
    }

}
