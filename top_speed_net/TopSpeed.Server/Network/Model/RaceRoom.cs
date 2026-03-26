using System;
using System.Collections.Generic;
using System.Net;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal sealed class RaceRoom
    {
        public RaceRoom(uint id, string name, GameRoomType roomType, byte playersToStart)
        {
            Id = id;
            Name = name;
            RoomType = roomType;
            PlayersToStart = playersToStart;
            TrackName = "america";
            Laps = 3;
        }

        public uint Id { get; }
        public uint Version { get; set; }
        public string Name { get; set; }
        public GameRoomType RoomType { get; set; }
        public byte PlayersToStart { get; set; }
        public uint HostId { get; set; }
        public HashSet<uint> PlayerIds { get; } = new HashSet<uint>();
        public List<RoomBot> Bots { get; } = new List<RoomBot>();
        public Dictionary<uint, PlayerLoadout> PendingLoadouts { get; } = new Dictionary<uint, PlayerLoadout>();
        public HashSet<uint> PrepareSkips { get; } = new HashSet<uint>();
        public bool PreparingRace { get; set; }
        public bool RaceStarted { get; set; }
        public bool TrackSelected { get; set; }
        public TrackData? TrackData { get; set; }
        public string TrackName { get; set; }
        public byte Laps { get; set; }
        public List<byte> RaceResults { get; } = new List<byte>();
        public Dictionary<byte, int> RaceFinishTimesMs { get; } = new Dictionary<byte, int>();
        public DateTime RaceStartedUtc { get; set; }
        public bool RaceStopPending { get; set; }
        public float RaceStopDelaySeconds { get; set; }
        public HashSet<ulong> ActiveBumpPairs { get; } = new HashSet<ulong>();
        public Dictionary<uint, MediaBlob> MediaMap { get; } = new Dictionary<uint, MediaBlob>();
        public uint RaceSnapshotSequence { get; set; }
        public uint RaceSnapshotTick { get; set; }
    }

}
