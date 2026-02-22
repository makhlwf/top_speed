using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Bogus;
using LiteNetLib;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Server.Logging;
using TopSpeed.Server.Protocol;
using TopSpeed.Server.Tracks;

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
        public bool EngineRunning { get; set; }
        public bool Braking { get; set; }
        public bool Horning { get; set; }
        public bool Backfiring { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public float WidthM { get; set; }
        public float LengthM { get; set; }

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
                Backfiring = Backfiring
            };
        }
    }

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
        public string Name { get; set; }
        public GameRoomType RoomType { get; set; }
        public byte PlayersToStart { get; set; }
        public uint HostId { get; set; }
        public HashSet<uint> PlayerIds { get; } = new HashSet<uint>();
        public List<RoomBot> Bots { get; } = new List<RoomBot>();
        public Dictionary<uint, PlayerLoadout> PendingLoadouts { get; } = new Dictionary<uint, PlayerLoadout>();
        public bool PreparingRace { get; set; }
        public bool RaceStarted { get; set; }
        public bool TrackSelected { get; set; }
        public TrackData? TrackData { get; set; }
        public string TrackName { get; set; }
        public byte Laps { get; set; }
        public List<byte> RaceResults { get; } = new List<byte>();
        public HashSet<ulong> ActiveBumpPairs { get; } = new HashSet<ulong>();
    }

    internal readonly struct PlayerLoadout
    {
        public PlayerLoadout(CarType car, bool automaticTransmission)
        {
            Car = car;
            AutomaticTransmission = automaticTransmission;
        }

        public CarType Car { get; }
        public bool AutomaticTransmission { get; }
    }

    internal readonly struct VehicleDimensions
    {
        public VehicleDimensions(float widthM, float lengthM)
        {
            WidthM = widthM;
            LengthM = lengthM;
        }

        public float WidthM { get; }
        public float LengthM { get; }
    }

    internal readonly struct BotAudioProfile
    {
        public BotAudioProfile(int idleFrequency, int topFrequency, int shiftFrequency)
        {
            IdleFrequency = idleFrequency;
            TopFrequency = topFrequency;
            ShiftFrequency = shiftFrequency;
        }

        public int IdleFrequency { get; }
        public int TopFrequency { get; }
        public int ShiftFrequency { get; }
    }

    internal enum BotDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    internal enum BotRacePhase
    {
        Normal = 0,
        Crashing = 1,
        Restarting = 2
    }

    internal sealed class RoomBot
    {
        public uint Id { get; set; }
        public byte PlayerNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public BotDifficulty Difficulty { get; set; }
        public int AddedOrder { get; set; }
        public CarType Car { get; set; } = CarType.Vehicle1;
        public bool AutomaticTransmission { get; set; } = true;
        public PlayerState State { get; set; } = PlayerState.NotReady;
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float SpeedKph { get; set; }
        public float StartDelaySeconds { get; set; }
        public float EngineStartSecondsRemaining { get; set; }
        public float WidthM { get; set; } = 1.8f;
        public float LengthM { get; set; } = 4.5f;
        public BotPhysicsState PhysicsState { get; set; }
        public BotPhysicsConfig PhysicsConfig { get; set; } = BotPhysicsCatalog.Get(CarType.Vehicle1);
        public BotAudioProfile AudioProfile { get; set; } = new BotAudioProfile(22050, 55000, 26000);
        public int EngineFrequency { get; set; } = 22050;
        public bool Horning { get; set; }
        public float HornSecondsRemaining { get; set; }
        public bool BackfireArmed { get; set; } = true;
        public float BackfirePulseSeconds { get; set; }
        public BotRacePhase RacePhase { get; set; } = BotRacePhase.Normal;
        public float CrashRecoverySeconds { get; set; }
    }

    internal sealed class RaceServer : IDisposable
    {
        private const float ServerSimulationStepSeconds = 0.008f;
        private const float ServerSnapshotIntervalSeconds = 1f / 60f;
        private const float CleanupIntervalSeconds = 1.0f;
        private const float BotRaceStartDelaySeconds = 6.5f;
        private const float BotAiLookaheadMeters = 30.0f;
        private const float BotHornMinDistanceMeters = 100.0f;
        private const float BotBackfirePulseSeconds = 0.1f;
        private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(10);

        private readonly RaceServerConfig _config;
        private readonly Logger _logger;
        private readonly object _lock = new object();
        private readonly UdpServerTransport _transport;
        private readonly Dictionary<uint, PlayerConnection> _players = new Dictionary<uint, PlayerConnection>();
        private readonly Dictionary<string, uint> _endpointIndex = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<uint, RaceRoom> _rooms = new Dictionary<uint, RaceRoom>();
        private readonly Faker _faker = new Faker();
        private readonly Random _random = new Random();

        private uint _nextPlayerId = 1;
        private uint _nextRoomId = 1;
        private uint _nextBotId = 1_000_000;
        private float _simulationAccumulator;
        private float _snapshotAccumulator;
        private float _cleanupAccumulator;
        private int _authorityDropsPlayerState;
        private int _authorityDropsPlayerData;
        private int _authorityDropsPlayerStarted;
        private int _authorityDropsPlayerFinished;
        private int _authorityDropsPlayerCrashed;
        private int _joinDeniedRaceInProgress;
        private int _roomMutationDenied;
        private int _raceSnapshotSends;
        private int _stateSyncFramesSent;
        private int _bumpEventsHumanHuman;
        private int _bumpEventsHumanBot;
        private int _botCrashEvents;
        private int _botRestartEvents;
        private int _botResumeEvents;
        private int _botStartEvents;
        private int _botFinishEvents;
        private int _botHornOvertakeEvents;
        private int _botHornBumpEvents;

        public RaceServer(RaceServerConfig config, Logger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transport = new UdpServerTransport(_logger);
            _transport.PacketReceived += OnPacketReceived;
            _transport.PeerDisconnected += OnPeerDisconnected;
        }

        public void Start()
        {
            _transport.Start(_config.Port);
            _logger.Info("Race server started.");
        }

        public void Stop()
        {
            lock (_lock)
            {
                _rooms.Clear();
                _players.Clear();
                _endpointIndex.Clear();
            }

            _transport.Stop();
            _logger.Info("Race server stopped.");
        }

        public void Update(float deltaSeconds)
        {
            lock (_lock)
            {
                if (deltaSeconds <= 0f)
                    return;

                _simulationAccumulator += deltaSeconds;
                while (_simulationAccumulator >= ServerSimulationStepSeconds)
                {
                    _simulationAccumulator -= ServerSimulationStepSeconds;
                    _cleanupAccumulator += ServerSimulationStepSeconds;
                    _snapshotAccumulator += ServerSimulationStepSeconds;

                    if (_cleanupAccumulator >= CleanupIntervalSeconds)
                    {
                        _cleanupAccumulator -= CleanupIntervalSeconds;
                        CleanupConnections();
                    }

                    UpdateBots(ServerSimulationStepSeconds);
                    CheckForBumps();

                    if (_snapshotAccumulator >= ServerSnapshotIntervalSeconds)
                    {
                        _snapshotAccumulator -= ServerSnapshotIntervalSeconds;
                        BroadcastPlayerData();
                    }
                }
            }
        }

        private void OnPacketReceived(IPEndPoint endPoint, byte[] payload)
        {
            if (!PacketSerializer.TryReadHeader(payload, out var header))
            {
                _logger.Warning($"Dropped packet with invalid header from {endPoint}.");
                return;
            }
            if (header.Version != ProtocolConstants.Version)
            {
                _logger.Debug($"Dropped packet with protocol version mismatch from {endPoint}: received={header.Version}, expected={ProtocolConstants.Version}.");
                return;
            }

            lock (_lock)
            {
                var player = GetOrAddConnection(endPoint);
                if (player == null)
                    return;

                player.LastSeenUtc = DateTime.UtcNow;

                switch (header.Command)
                {
                    case Command.KeepAlive:
                        break;
                    case Command.PlayerHello:
                        if (PacketSerializer.TryReadPlayerHello(payload, out var hello))
                            HandlePlayerHello(player, hello);
                        else
                            LogPacketParseFailure(endPoint, header.Command);
                        break;
                    case Command.PlayerState:
                        if (PacketSerializer.TryReadPlayerState(payload, out var state))
                            HandlePlayerState(player, state);
                        else
                            LogPacketParseFailure(endPoint, header.Command);
                        break;
                    case Command.PlayerDataToServer:
                        if (PacketSerializer.TryReadPlayerData(payload, out var playerData))
                            HandlePlayerData(player, playerData);
                        else
                            LogPacketParseFailure(endPoint, header.Command);
                        break;
                    case Command.PlayerStarted:
                        if (PacketSerializer.TryReadPlayer(payload, out _))
                            HandlePlayerStarted(player);
                        else
                            LogPacketParseFailure(endPoint, header.Command);
                        break;
                    case Command.PlayerFinished:
                        if (PacketSerializer.TryReadPlayer(payload, out var finished))
                            HandlePlayerFinished(player, finished);
                        else
                            LogPacketParseFailure(endPoint, header.Command);
                        break;
                    case Command.PlayerCrashed:
                        if (PacketSerializer.TryReadPlayer(payload, out var crashed))
                            HandlePlayerCrashed(player, crashed);
                        else
                            LogPacketParseFailure(endPoint, header.Command);
                        break;
                    case Command.RoomListRequest:
                        SendRoomList(player);
                        break;
                    case Command.RoomCreate:
                        if (PacketSerializer.TryReadRoomCreate(payload, out var create))
                            HandleCreateRoom(player, create);
                        else
                            LogPacketParseFailure(endPoint, header.Command);
                        break;
                    case Command.RoomJoin:
                        if (PacketSerializer.TryReadRoomJoin(payload, out var join))
                            HandleJoinRoom(player, join);
                        else
                            LogPacketParseFailure(endPoint, header.Command);
                        break;
                    case Command.RoomLeave:
                        HandleLeaveRoom(player, true);
                        break;
                    case Command.RoomSetTrack:
                        if (PacketSerializer.TryReadRoomSetTrack(payload, out var track))
                            HandleSetTrack(player, track);
                        else
                            LogPacketParseFailure(endPoint, header.Command);
                        break;
                    case Command.RoomSetLaps:
                        if (PacketSerializer.TryReadRoomSetLaps(payload, out var laps))
                            HandleSetLaps(player, laps);
                        else
                            LogPacketParseFailure(endPoint, header.Command);
                        break;
                    case Command.RoomStartRace:
                        HandleStartRace(player);
                        break;
                    case Command.RoomSetPlayersToStart:
                        if (PacketSerializer.TryReadRoomSetPlayersToStart(payload, out var setPlayers))
                            HandleSetPlayersToStart(player, setPlayers);
                        else
                            LogPacketParseFailure(endPoint, header.Command);
                        break;
                    case Command.RoomAddBot:
                        HandleAddBot(player);
                        break;
                    case Command.RoomRemoveBot:
                        HandleRemoveBot(player);
                        break;
                    case Command.RoomPlayerReady:
                        if (PacketSerializer.TryReadRoomPlayerReady(payload, out var ready))
                            HandlePlayerReady(player, ready);
                        else
                            LogPacketParseFailure(endPoint, header.Command);
                        break;
                    default:
                        _logger.Warning($"Ignoring unknown packet command {(byte)header.Command} from {endPoint}.");
                        break;
                }
            }
        }

        private void LogPacketParseFailure(IPEndPoint endPoint, Command command)
        {
            _logger.Warning($"Failed to parse {command} packet from {endPoint}.");
        }

        private PlayerConnection? GetOrAddConnection(IPEndPoint endpoint)
        {
            var key = endpoint.ToString();
            if (_endpointIndex.TryGetValue(key, out var id) && _players.TryGetValue(id, out var existing))
                return existing;

            if (_players.Count >= _config.MaxPlayers)
            {
                _transport.Send(endpoint, PacketSerializer.WriteGeneral(Command.Disconnect));
                _logger.Warning($"Refused connection from {endpoint}: server is full.");
                return null;
            }

            var playerId = _nextPlayerId++;
            var player = new PlayerConnection(endpoint, playerId);
            _players[playerId] = player;
            _endpointIndex[key] = playerId;

            _transport.Send(endpoint, PacketSerializer.WritePlayerNumber(playerId, 0));
            if (!string.IsNullOrWhiteSpace(_config.Motd))
                _transport.Send(endpoint, PacketSerializer.WriteServerInfo(new PacketServerInfo { Motd = _config.Motd }));

            SendRoomState(player, null);
            SendRoomList(player);
            _logger.Info($"Connection established: playerId={player.Id}, endpoint={endpoint}.");
            return player;
        }

        private void HandlePlayerHello(PlayerConnection player, PacketPlayerHello hello)
        {
            var name = (hello.Name ?? string.Empty).Trim();
            if (name.Length > ProtocolConstants.MaxPlayerNameLength)
                name = name.Substring(0, ProtocolConstants.MaxPlayerNameLength);
            player.Name = name;
            if (player.RoomId.HasValue && _rooms.TryGetValue(player.RoomId.Value, out var room))
                BroadcastRoomState(room);
        }

        private void HandlePlayerState(PlayerConnection player, PacketPlayerState state)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                _authorityDropsPlayerState++;
                return;
            }

            var previousState = player.State;

            if (room.RaceStarted)
            {
                if (state.State == PlayerState.AwaitingStart
                    || state.State == PlayerState.Racing
                    || state.State == PlayerState.Finished)
                {
                    player.State = state.State;
                }
                else
                {
                    _authorityDropsPlayerState++;
                }
            }
            else
            {
                if (state.State != PlayerState.NotReady && state.State != PlayerState.Undefined)
                    _authorityDropsPlayerState++;
                player.State = PlayerState.NotReady;
                if (room.TrackSelected)
                    SendTrack(room, player);
            }

            if (previousState != player.State)
                _logger.Debug($"Player state transition: room={room.Id}, player={player.Id}, {previousState} -> {player.State} (packet={state.State}).");
            BroadcastRoomState(room);
        }

        private void HandlePlayerData(PlayerConnection player, PacketPlayerData data)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                _authorityDropsPlayerData++;
                return;
            }

            var previousState = player.State;
            player.Car = NormalizeNetworkCar(data.Car);
            ApplyVehicleDimensions(player, player.Car);
            player.PositionX = data.RaceData.PositionX;
            player.PositionY = data.RaceData.PositionY;
            player.Speed = data.RaceData.Speed;
            player.Frequency = data.RaceData.Frequency;
            player.EngineRunning = data.EngineRunning;
            player.Braking = data.Braking;
            player.Horning = data.Horning;
            player.Backfiring = data.Backfiring;
            var nextState = data.State;

            if (room.RaceStarted)
            {
                if (nextState == PlayerState.Undefined || nextState == PlayerState.NotReady)
                {
                    _authorityDropsPlayerData++;
                    nextState = player.State;
                }

                if (nextState != PlayerState.AwaitingStart
                    && nextState != PlayerState.Racing
                    && nextState != PlayerState.Finished)
                {
                    _authorityDropsPlayerData++;
                    nextState = player.State;
                }
            }
            else
            {
                if (nextState != PlayerState.NotReady && nextState != PlayerState.Undefined)
                    _authorityDropsPlayerData++;
                nextState = PlayerState.NotReady;
            }

            player.State = nextState;
            if (previousState != nextState)
                _logger.Debug($"Player state transition from data: room={room.Id}, player={player.Id}, {previousState} -> {nextState}.");
        }

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
                _logger.Debug($"PlayerFinished payload mismatch: room={room.Id}, connectionPlayer={player.Id}/{player.PlayerNumber}, payload={finished.PlayerId}/{finished.PlayerNumber}.");
            }

            player.State = PlayerState.Finished;
            if (!room.RaceResults.Contains(player.PlayerNumber))
                room.RaceResults.Add(player.PlayerNumber);

            SendToRoomExcept(room, player.Id, PacketSerializer.WritePlayer(Command.PlayerFinished, player.Id, player.PlayerNumber));
            _logger.Debug($"Player finished: room={room.Id}, player={player.Id}, number={player.PlayerNumber}, results={room.RaceResults.Count}.");
            if (CountActiveRaceParticipants(room) == 0)
                StopRace(room);
        }

        private void HandlePlayerStarted(PlayerConnection player)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                _authorityDropsPlayerStarted++;
                return;
            }
            if (!room.RaceStarted)
            {
                _authorityDropsPlayerStarted++;
                return;
            }

            if (player.State == PlayerState.AwaitingStart || player.State == PlayerState.Racing)
            {
                player.State = PlayerState.Racing;
            }
            else
            {
                _authorityDropsPlayerStarted++;
            }
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
                _logger.Debug($"PlayerCrashed payload mismatch: room={room.Id}, connectionPlayer={player.Id}/{player.PlayerNumber}, payload={crashed.PlayerId}/{crashed.PlayerNumber}.");
            }

            SendToRoomExcept(room, player.Id, PacketSerializer.WritePlayer(Command.PlayerCrashed, player.Id, player.PlayerNumber));
        }

        private void HandleCreateRoom(PlayerConnection player, PacketRoomCreate packet)
        {
            var roomName = (packet.RoomName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(roomName))
                roomName = $"Game {_nextRoomId}";
            if (roomName.Length > ProtocolConstants.MaxRoomNameLength)
                roomName = roomName.Substring(0, ProtocolConstants.MaxRoomNameLength);

            var roomType = packet.RoomType;
            var playersToStart = packet.PlayersToStart;
            if (playersToStart < 1 || playersToStart > ProtocolConstants.MaxRoomPlayersToStart)
                playersToStart = 2;

            var room = new RaceRoom(_nextRoomId++, roomName, roomType, playersToStart);
            _rooms[room.Id] = room;
            SetTrack(room, room.TrackName);
            JoinRoom(player, room);
            SendProtocolMessage(player, ProtocolMessageCode.Ok, $"Created {room.Name}.");
            BroadcastRoomList();
            BroadcastLobbyAnnouncement($"{DescribePlayer(player)} created game room {room.Name}.");
            _logger.Info($"Room created: room={room.Id} \"{room.Name}\", host={player.Id}, type={room.RoomType}, playersToStart={room.PlayersToStart}.");
        }

        private void HandleJoinRoom(PlayerConnection player, PacketRoomJoin packet)
        {
            if (!_rooms.TryGetValue(packet.RoomId, out var room))
            {
                SendProtocolMessage(player, ProtocolMessageCode.RoomNotFound, "Game room not found.");
                return;
            }

            if (room.RaceStarted || room.PreparingRace)
            {
                _joinDeniedRaceInProgress++;
                _logger.Debug($"Join denied: player={player.Id}, room={room.Id}, raceStarted={room.RaceStarted}, preparing={room.PreparingRace}.");
                SendProtocolMessage(player, ProtocolMessageCode.Failed, "This game room is currently in progress.");
                return;
            }

            if (GetRoomParticipantCount(room) >= room.PlayersToStart)
            {
                SendProtocolMessage(player, ProtocolMessageCode.RoomFull, "This game room is unavailable because it is full.");
                return;
            }

            JoinRoom(player, room);
            SendProtocolMessage(player, ProtocolMessageCode.Ok, $"Joined {room.Name}.");
            BroadcastRoomList();
            _logger.Info($"Player joined room: room={room.Id} \"{room.Name}\", player={player.Id}, participants={GetRoomParticipantCount(room)}/{room.PlayersToStart}.");
        }

        private void HandleLeaveRoom(PlayerConnection player, bool notify)
        {
            if (!player.RoomId.HasValue)
            {
                SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, "You are not in a game room.");
                return;
            }

            var roomId = player.RoomId.Value;
            if (!_rooms.TryGetValue(roomId, out var room))
            {
                player.RoomId = null;
                SendRoomState(player, null);
                return;
            }

            var oldNumber = player.PlayerNumber;
            var leftName = DescribePlayer(player);
            room.PlayerIds.Remove(player.Id);
            player.RoomId = null;
            player.PlayerNumber = 0;
            player.State = PlayerState.NotReady;
            room.PendingLoadouts.Remove(player.Id);

            if (notify)
            {
                SendToRoom(room, PacketSerializer.WritePlayer(Command.PlayerDisconnected, player.Id, oldNumber));
                SendProtocolMessageToRoom(room, $"{leftName} has left the game.");
            }

            SendRoomState(player, null);

            if (room.PlayerIds.Count == 0)
            {
                _rooms.Remove(room.Id);
                _logger.Info($"Room closed: room={room.Id} \"{room.Name}\".");
            }
            else
            {
                if (room.HostId == player.Id)
                    room.HostId = room.PlayerIds.OrderBy(x => x).First();
                if (room.RaceStarted && CountActiveRaceParticipants(room) == 0)
                    StopRace(room);
                if (room.PreparingRace)
                    TryStartRaceAfterLoadout(room);
                BroadcastRoomState(room);
            }

            BroadcastRoomList();
            _logger.Info($"Player left room: room={room.Id} \"{room.Name}\", player={player.Id}, notify={notify}.");
        }

        private void HandleSetTrack(PlayerConnection player, PacketRoomSetTrack packet)
        {
            if (!TryGetHostedRoom(player, out var room))
                return;
            if (room.RaceStarted || room.PreparingRace)
            {
                _roomMutationDenied++;
                _logger.Debug($"Room track change denied: room={room.Id}, player={player.Id}, raceStarted={room.RaceStarted}, preparing={room.PreparingRace}.");
                SendProtocolMessage(player, ProtocolMessageCode.Failed, "Cannot change track while race setup or race is active.");
                return;
            }

            var trackName = (packet.TrackName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trackName))
            {
                SendProtocolMessage(player, ProtocolMessageCode.InvalidTrack, "Track cannot be empty.");
                return;
            }

            SetTrack(room, trackName);
            SendTrackToNotReady(room);
            BroadcastRoomState(room);
        }

        private void HandleSetLaps(PlayerConnection player, PacketRoomSetLaps packet)
        {
            if (!TryGetHostedRoom(player, out var room))
                return;
            if (room.RaceStarted || room.PreparingRace)
            {
                _roomMutationDenied++;
                _logger.Debug($"Room laps change denied: room={room.Id}, player={player.Id}, raceStarted={room.RaceStarted}, preparing={room.PreparingRace}.");
                SendProtocolMessage(player, ProtocolMessageCode.Failed, "Cannot change laps while race setup or race is active.");
                return;
            }

            if (packet.Laps < 1 || packet.Laps > 16)
            {
                SendProtocolMessage(player, ProtocolMessageCode.InvalidLaps, "Laps must be between 1 and 16.");
                return;
            }

            room.Laps = packet.Laps;
            if (room.TrackSelected)
                SetTrack(room, room.TrackName);
            SendTrackToNotReady(room);
            BroadcastRoomState(room);
        }

        private void HandleStartRace(PlayerConnection player)
        {
            if (!TryGetHostedRoom(player, out var room))
                return;

            if (GetRoomParticipantCount(room) < room.PlayersToStart)
            {
                SendProtocolMessage(player, ProtocolMessageCode.Failed, $"Not enough players. {room.PlayersToStart} required.");
                return;
            }

            if (room.RaceStarted)
            {
                SendProtocolMessage(player, ProtocolMessageCode.Failed, "A race is already in progress.");
                return;
            }

            if (room.PreparingRace)
            {
                SendProtocolMessage(player, ProtocolMessageCode.Failed, "Race setup is already in progress.");
                return;
            }

            room.PreparingRace = true;
            room.PendingLoadouts.Clear();
            AssignRandomBotLoadouts(room);
            AnnounceBotsReady(room);
            _logger.Info($"Race prepare started: room={room.Id} \"{room.Name}\", requestedBy={player.Id}, humans={room.PlayerIds.Count}, bots={room.Bots.Count}, required={room.PlayersToStart}.");

            SendProtocolMessageToRoom(room, $"{DescribePlayer(player)} is about to start the game. Choose your vehicle and transmission mode.");
            SendToRoom(room, PacketSerializer.WriteGeneral(Command.RoomPrepareRace));
            TryStartRaceAfterLoadout(room);
        }

        private void HandlePlayerReady(PlayerConnection player, PacketRoomPlayerReady ready)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, "You are not in a game room.");
                return;
            }

            if (!room.PreparingRace)
            {
                SendProtocolMessage(player, ProtocolMessageCode.Failed, "Race setup has not started yet.");
                return;
            }

            if (!room.PlayerIds.Contains(player.Id))
            {
                SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, "You are not in this game room.");
                return;
            }

            var selectedCar = NormalizeNetworkCar(ready.Car);
            player.Car = selectedCar;
            ApplyVehicleDimensions(player, selectedCar);
            room.PendingLoadouts[player.Id] = new PlayerLoadout(selectedCar, ready.AutomaticTransmission);
            _logger.Debug($"Player ready: room={room.Id}, player={player.Id}, car={selectedCar}, automatic={ready.AutomaticTransmission}, ready={room.PendingLoadouts.Count}/{room.PlayerIds.Count}.");
            SendProtocolMessageToRoom(room, $"{DescribePlayer(player)} is ready.");
            TryStartRaceAfterLoadout(room);
        }

        private void HandleSetPlayersToStart(PlayerConnection player, PacketRoomSetPlayersToStart packet)
        {
            if (!TryGetHostedRoom(player, out var room))
                return;
            if (room.RaceStarted || room.PreparingRace)
            {
                _roomMutationDenied++;
                _logger.Debug($"Room player-limit change denied: room={room.Id}, player={player.Id}, raceStarted={room.RaceStarted}, preparing={room.PreparingRace}.");
                SendProtocolMessage(player, ProtocolMessageCode.Failed, "Cannot change player limit while race setup or race is active.");
                return;
            }

            var value = packet.PlayersToStart;
            if (value < 1 || value > ProtocolConstants.MaxRoomPlayersToStart)
            {
                SendProtocolMessage(player, ProtocolMessageCode.InvalidPlayersToStart, "Players to start must be between 1 and 10.");
                return;
            }

            if (GetRoomParticipantCount(room) > value)
            {
                SendProtocolMessage(player, ProtocolMessageCode.InvalidPlayersToStart, "Cannot set lower than current players in room.");
                return;
            }

            room.PlayersToStart = value;
            BroadcastRoomState(room);
            BroadcastRoomList();
        }

        private void HandleAddBot(PlayerConnection player)
        {
            if (!TryGetHostedRoom(player, out var room))
                return;
            if (room.RaceStarted || room.PreparingRace)
            {
                _roomMutationDenied++;
                _logger.Debug($"Room add-bot denied: room={room.Id}, player={player.Id}, raceStarted={room.RaceStarted}, preparing={room.PreparingRace}.");
                SendProtocolMessage(player, ProtocolMessageCode.Failed, "Cannot add bots while race setup or race is active.");
                return;
            }

            if (room.RoomType != GameRoomType.BotsRace)
            {
                SendProtocolMessage(player, ProtocolMessageCode.Failed, "Bots can only be added in race-with-bots rooms.");
                return;
            }

            if (GetRoomParticipantCount(room) >= room.PlayersToStart)
            {
                SendProtocolMessage(player, ProtocolMessageCode.RoomFull, "This game room is unavailable because it is full.");
                return;
            }

            var bot = CreateBot(room);
            room.Bots.Add(bot);
            BroadcastRoomState(room);
            BroadcastRoomList();
            SendToRoom(room, PacketSerializer.WritePlayerJoined(new PacketPlayerJoined
            {
                PlayerId = bot.Id,
                PlayerNumber = bot.PlayerNumber,
                Name = FormatBotJoinName(bot)
            }));
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
                _logger.Debug($"Room remove-bot denied: room={room.Id}, player={player.Id}, raceStarted={room.RaceStarted}, preparing={room.PreparingRace}.");
                SendProtocolMessage(player, ProtocolMessageCode.Failed, "Cannot remove bots while race setup or race is active.");
                return;
            }

            if (room.RoomType != GameRoomType.BotsRace)
            {
                SendProtocolMessage(player, ProtocolMessageCode.Failed, "Bots can only be removed in race-with-bots rooms.");
                return;
            }

            if (room.Bots.Count == 0)
            {
                SendProtocolMessage(player, ProtocolMessageCode.Failed, "There are no bots to remove.");
                return;
            }

            var bot = room.Bots.OrderByDescending(b => b.AddedOrder).First();
            room.Bots.Remove(bot);
            SendToRoom(room, PacketSerializer.WritePlayer(Command.PlayerDisconnected, bot.Id, bot.PlayerNumber));
            BroadcastRoomState(room);
            BroadcastRoomList();
            SendProtocolMessage(player, ProtocolMessageCode.Ok, $"Removed bot {bot.Name}.");
            if (room.RaceStarted && CountActiveRaceParticipants(room) == 0)
                StopRace(room);
            if (room.PreparingRace)
                TryStartRaceAfterLoadout(room);
        }

        private bool TryGetHostedRoom(PlayerConnection player, out RaceRoom room)
        {
            room = null!;
            if (!player.RoomId.HasValue)
            {
                SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, "You are not in a game room.");
                return false;
            }

            if (!_rooms.TryGetValue(player.RoomId.Value, out var foundRoom) || foundRoom == null)
            {
                SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, "You are not in a game room.");
                return false;
            }

            room = foundRoom;

            if (room.HostId != player.Id)
            {
                SendProtocolMessage(player, ProtocolMessageCode.NotHost, "Only host can do this.");
                return false;
            }

            return true;
        }

        private void JoinRoom(PlayerConnection player, RaceRoom room)
        {
            if (player.RoomId.HasValue)
                HandleLeaveRoom(player, true);

            room.PlayerIds.Add(player.Id);
            if (room.HostId == 0 || !room.PlayerIds.Contains(room.HostId))
                room.HostId = player.Id;

            player.RoomId = room.Id;
            player.PlayerNumber = (byte)FindFreeRoomNumber(room);
            player.State = PlayerState.NotReady;

            _transport.Send(player.EndPoint, PacketSerializer.WritePlayerNumber(player.Id, player.PlayerNumber));
            SendTrack(room, player);
            BroadcastRoomState(room);

            var joinedName = string.IsNullOrWhiteSpace(player.Name)
                ? $"Player {player.PlayerNumber + 1}"
                : player.Name;
            var joined = new PacketPlayerJoined { PlayerId = player.Id, PlayerNumber = player.PlayerNumber, Name = joinedName };
            SendToRoomExcept(room, player.Id, PacketSerializer.WritePlayerJoined(joined));
            _logger.Debug($"Join room assignment: room={room.Id}, player={player.Id}, playerNumber={player.PlayerNumber}, host={room.HostId}.");
        }

        private int FindFreeRoomNumber(RaceRoom room)
        {
            for (var i = 0; i < room.PlayersToStart; i++)
            {
                var usedByPlayer = room.PlayerIds.Any(id => _players.TryGetValue(id, out var p) && p.PlayerNumber == i);
                var usedByBot = room.Bots.Any(bot => bot.PlayerNumber == i);
                var used = usedByPlayer || usedByBot;
                if (!used)
                    return i;
            }

            return 0;
        }

        private void SetTrack(RaceRoom room, string trackName)
        {
            room.TrackName = trackName;
            room.TrackData = TrackLoader.LoadTrack(room.TrackName, room.Laps);
            room.TrackSelected = true;
        }

        private void StartRace(RaceRoom room)
        {
            if (room.RaceStarted)
                return;

            room.PreparingRace = false;
            room.PendingLoadouts.Clear();

            if (!room.TrackSelected || room.TrackData == null)
                SetTrack(room, room.TrackName);

            room.RaceStarted = true;
            room.RaceResults.Clear();
            room.ActiveBumpPairs.Clear();
            var laneHalfWidth = GetLaneHalfWidth(room);
            var rowSpacing = GetStartRowSpacing(room);
            foreach (var id in room.PlayerIds)
            {
                if (_players.TryGetValue(id, out var p))
                {
                    p.State = PlayerState.AwaitingStart;
                    p.PositionX = CalculateStartX(p.PlayerNumber, p.WidthM, laneHalfWidth);
                    p.PositionY = CalculateStartY(p.PlayerNumber, rowSpacing);
                    p.Speed = 0;
                    p.Frequency = ProtocolConstants.DefaultFrequency;
                    p.EngineRunning = false;
                    p.Braking = false;
                    p.Horning = false;
                    p.Backfiring = false;
                }
            }
            foreach (var bot in room.Bots)
            {
                bot.State = PlayerState.AwaitingStart;
                bot.RacePhase = BotRacePhase.Normal;
                bot.CrashRecoverySeconds = 0f;
                bot.SpeedKph = 0f;
                bot.StartDelaySeconds = BotRaceStartDelaySeconds + GetBotReactionDelay(bot.Difficulty);
                bot.EngineStartSecondsRemaining = 0f;
                bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                bot.Horning = false;
                bot.HornSecondsRemaining = 0f;
                bot.BackfireArmed = true;
                bot.BackfirePulseSeconds = 0f;
                bot.PositionX = CalculateStartX(bot.PlayerNumber, bot.WidthM, laneHalfWidth);
                bot.PositionY = CalculateStartY(bot.PlayerNumber, rowSpacing);
                bot.PhysicsState = new BotPhysicsState
                {
                    PositionX = bot.PositionX,
                    PositionY = bot.PositionY,
                    SpeedKph = 0f,
                    Gear = 1,
                    AutoShiftCooldownSeconds = 0f
                };
            }

            SendTrackToRoom(room);
            SendToRoom(room, PacketSerializer.WriteGeneral(Command.StartRace));
            SendRaceSnapshot(room, DeliveryMethod.ReliableOrdered);
            BroadcastRoomState(room);
            _logger.Info($"Race started: room={room.Id} \"{room.Name}\", track={room.TrackName}, laps={room.Laps}, humans={room.PlayerIds.Count}, bots={room.Bots.Count}.");
        }

        private void StopRace(RaceRoom room)
        {
            room.RaceStarted = false;
            room.PreparingRace = false;
            room.PendingLoadouts.Clear();
            room.ActiveBumpPairs.Clear();

            var results = room.RaceResults.ToArray();
            SendToRoom(room, PacketSerializer.WriteRaceResults(new PacketRaceResults
            {
                NPlayers = (byte)Math.Min(results.Length, ProtocolConstants.MaxPlayers),
                Results = results
            }));

            room.RaceResults.Clear();
            foreach (var id in room.PlayerIds)
            {
                if (_players.TryGetValue(id, out var p))
                    p.State = PlayerState.NotReady;
            }
            foreach (var bot in room.Bots)
            {
                bot.State = PlayerState.NotReady;
                bot.RacePhase = BotRacePhase.Normal;
                bot.CrashRecoverySeconds = 0f;
                bot.SpeedKph = 0f;
                bot.StartDelaySeconds = 0f;
                bot.EngineStartSecondsRemaining = 0f;
                bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                bot.Horning = false;
                bot.HornSecondsRemaining = 0f;
                bot.BackfireArmed = true;
                bot.BackfirePulseSeconds = 0f;
                bot.PhysicsState = new BotPhysicsState
                {
                    PositionX = bot.PositionX,
                    PositionY = bot.PositionY,
                    SpeedKph = 0f,
                    Gear = 1,
                    AutoShiftCooldownSeconds = 0f
                };
            }

            BroadcastRoomState(room);
            _logger.Info($"Race stopped: room={room.Id} \"{room.Name}\", results={string.Join(",", results)}.");
        }

        private void SendTrackToRoom(RaceRoom room)
        {
            foreach (var id in room.PlayerIds)
            {
                if (_players.TryGetValue(id, out var player))
                    SendTrack(room, player);
            }
        }

        private void SendTrackToNotReady(RaceRoom room)
        {
            foreach (var id in room.PlayerIds)
            {
                if (_players.TryGetValue(id, out var player) && player.State == PlayerState.NotReady)
                    SendTrack(room, player);
            }
        }

        private void SendTrack(RaceRoom room, PlayerConnection player)
        {
            if (!room.TrackSelected || room.TrackData == null)
                return;

            var trackLength = (ushort)Math.Min(room.TrackData.Definitions.Length, ProtocolConstants.MaxMultiTrackLength);
            _transport.Send(player.EndPoint, PacketSerializer.WriteLoadCustomTrack(new PacketLoadCustomTrack
            {
                NrOfLaps = room.TrackData.Laps,
                TrackName = room.TrackData.UserDefined ? "custom" : room.TrackName,
                TrackWeather = room.TrackData.Weather,
                TrackAmbience = room.TrackData.Ambience,
                TrackLength = trackLength,
                Definitions = room.TrackData.Definitions
            }));
        }

        private void SendRoomList(PlayerConnection player)
        {
            var list = new PacketRoomList
            {
                Rooms = _rooms.Values.OrderBy(r => r.Id).Take(ProtocolConstants.MaxRoomListEntries).Select(r => new PacketRoomSummary
                {
                    RoomId = r.Id,
                    RoomName = r.Name,
                    RoomType = r.RoomType,
                    PlayerCount = (byte)GetRoomParticipantCount(r),
                    PlayersToStart = r.PlayersToStart,
                    RaceStarted = r.RaceStarted,
                    TrackName = r.TrackName
                }).ToArray()
            };

            _transport.Send(player.EndPoint, PacketSerializer.WriteRoomList(list));
        }

        private void BroadcastRoomList()
        {
            foreach (var player in _players.Values)
                SendRoomList(player);
        }

        private void SendRoomState(PlayerConnection player, RaceRoom? room)
        {
            if (room == null)
            {
                _transport.Send(player.EndPoint, PacketSerializer.WriteRoomState(new PacketRoomState
                {
                    InRoom = false,
                    HostPlayerId = 0,
                    RoomType = GameRoomType.BotsRace,
                    PlayersToStart = 0,
                    Players = Array.Empty<PacketRoomPlayer>()
                }));
                return;
            }

            var players = room.PlayerIds
                .Where(id => _players.ContainsKey(id))
                .Select(id => _players[id])
                .Select(p => new PacketRoomPlayer
                {
                    PlayerId = p.Id,
                    PlayerNumber = p.PlayerNumber,
                    State = p.State,
                    Name = string.IsNullOrWhiteSpace(p.Name) ? $"Player {p.PlayerNumber + 1}" : p.Name
                })
                .Concat(room.Bots.Select(bot => new PacketRoomPlayer
                {
                    PlayerId = bot.Id,
                    PlayerNumber = bot.PlayerNumber,
                    State = bot.State,
                    Name = FormatBotDisplayName(bot)
                }))
                .OrderBy(p => p.PlayerNumber)
                .ToArray();

            _transport.Send(player.EndPoint, PacketSerializer.WriteRoomState(new PacketRoomState
            {
                RoomId = room.Id,
                HostPlayerId = room.HostId,
                RoomName = room.Name,
                RoomType = room.RoomType,
                PlayersToStart = room.PlayersToStart,
                InRoom = true,
                IsHost = room.HostId == player.Id,
                RaceStarted = room.RaceStarted,
                TrackName = room.TrackName,
                Laps = room.Laps,
                Players = players
            }));
        }

        private void BroadcastRoomState(RaceRoom room)
        {
            foreach (var id in room.PlayerIds)
            {
                if (_players.TryGetValue(id, out var player))
                    SendRoomState(player, room);
            }
        }

        private void AssignRandomBotLoadouts(RaceRoom room)
        {
            foreach (var bot in room.Bots)
            {
                bot.Car = (CarType)_random.Next((int)CarType.Vehicle1, (int)CarType.CustomVehicle);
                bot.AutomaticTransmission = _random.Next(0, 2) == 0;
                ApplyVehicleDimensions(bot, bot.Car);
            }
        }

        private void AnnounceBotsReady(RaceRoom room)
        {
            foreach (var bot in room.Bots.OrderBy(b => b.PlayerNumber))
            {
                SendProtocolMessageToRoom(room, $"{FormatBotJoinName(bot)} is ready.");
            }
        }

        private void TryStartRaceAfterLoadout(RaceRoom room)
        {
            if (!room.PreparingRace)
                return;
            if (GetRoomParticipantCount(room) < room.PlayersToStart)
            {
                room.PreparingRace = false;
                room.PendingLoadouts.Clear();
                SendProtocolMessageToRoom(room, "Race start cancelled because there are not enough players.");
                _logger.Info($"Race prepare cancelled: room={room.Id} \"{room.Name}\", participants={GetRoomParticipantCount(room)}, required={room.PlayersToStart}.");
                return;
            }
            if (room.PendingLoadouts.Count < room.PlayerIds.Count)
            {
                _logger.Debug($"Waiting for loadouts: room={room.Id}, ready={room.PendingLoadouts.Count}/{room.PlayerIds.Count}.");
                return;
            }

            room.PreparingRace = false;
            SendProtocolMessageToRoom(room, "All players are ready. Starting game.");
            _logger.Info($"All loadouts ready: room={room.Id} \"{room.Name}\", starting race.");
            StartRace(room);
        }

        private void SendProtocolMessage(PlayerConnection player, ProtocolMessageCode code, string text)
        {
            _transport.Send(player.EndPoint, PacketSerializer.WriteProtocolMessage(new PacketProtocolMessage
            {
                Code = code,
                Message = text ?? string.Empty
            }));
        }

        private void SendProtocolMessageToRoom(RaceRoom room, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var payload = PacketSerializer.WriteProtocolMessage(new PacketProtocolMessage
            {
                Code = ProtocolMessageCode.Ok,
                Message = text
            });

            SendToRoom(room, payload);
        }

        private void BroadcastLobbyAnnouncement(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            foreach (var player in _players.Values)
            {
                if (player.RoomId.HasValue)
                    continue;

                SendProtocolMessage(player, ProtocolMessageCode.Ok, text);
            }
        }

        private static string DescribePlayer(PlayerConnection player)
        {
            if (!string.IsNullOrWhiteSpace(player.Name))
                return player.Name;
            return "A player";
        }

        private RoomBot CreateBot(RaceRoom room)
        {
            var name = (_faker.Name.FirstName() ?? "Bot").Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = "Bot";
            if (name.Length > ProtocolConstants.MaxPlayerNameLength)
                name = name.Substring(0, ProtocolConstants.MaxPlayerNameLength);

            var car = (CarType)_random.Next((int)CarType.Vehicle1, (int)CarType.CustomVehicle);
            var bot = new RoomBot
            {
                Id = _nextBotId++,
                PlayerNumber = (byte)FindFreeRoomNumber(room),
                Name = name,
                Difficulty = (BotDifficulty)_random.Next(0, 3),
                AddedOrder = room.Bots.Count == 0 ? 1 : room.Bots.Max(b => b.AddedOrder) + 1,
                Car = car,
                AutomaticTransmission = _random.Next(0, 2) == 0
            };

            ApplyVehicleDimensions(bot, car);
            bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
            return bot;
        }

        private static int GetRoomParticipantCount(RaceRoom room)
        {
            return room.PlayerIds.Count + room.Bots.Count;
        }

        private static string DifficultyLabel(BotDifficulty difficulty)
        {
            return difficulty switch
            {
                BotDifficulty.Easy => "easy",
                BotDifficulty.Hard => "hard",
                _ => "normal"
            };
        }

        private float GetBotReactionDelay(BotDifficulty difficulty)
        {
            return difficulty switch
            {
                BotDifficulty.Hard => 0.1f + (float)_random.NextDouble() * 0.4f,
                BotDifficulty.Normal => 1.0f + (float)_random.NextDouble() * 1.5f,
                _ => 2.5f + (float)_random.NextDouble() * 2.5f
            };
        }

        private static string FormatBotDisplayName(RoomBot bot)
        {
            var label = $"{FormatBotJoinName(bot)} ({DifficultyLabel(bot.Difficulty)})";
            if (label.Length > ProtocolConstants.MaxPlayerNameLength)
                return label.Substring(0, ProtocolConstants.MaxPlayerNameLength);
            return label;
        }

        private static string FormatBotJoinName(RoomBot bot)
        {
            var label = $"Bot {bot.Name}";
            if (label.Length > ProtocolConstants.MaxPlayerNameLength)
                return label.Substring(0, ProtocolConstants.MaxPlayerNameLength);
            return label;
        }

        private static CarType NormalizeNetworkCar(CarType car)
        {
            if (car < CarType.Vehicle1 || car >= CarType.CustomVehicle)
                return CarType.Vehicle1;
            return car;
        }

        private static void ApplyVehicleDimensions(PlayerConnection player, CarType car)
        {
            var dimensions = GetVehicleDimensions(car);
            player.WidthM = dimensions.WidthM;
            player.LengthM = dimensions.LengthM;
        }

        private static void ApplyVehicleDimensions(RoomBot bot, CarType car)
        {
            var dimensions = GetVehicleDimensions(car);
            bot.WidthM = dimensions.WidthM;
            bot.LengthM = dimensions.LengthM;
            bot.PhysicsConfig = BotPhysicsCatalog.Get(car);
            bot.AudioProfile = GetVehicleAudioProfile(car);
            bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
            var state = bot.PhysicsState;
            if (state.Gear <= 0)
                state.Gear = 1;
            bot.PhysicsState = state;
        }

        private static VehicleDimensions GetVehicleDimensions(CarType car)
        {
            return car switch
            {
                CarType.Vehicle1 => new VehicleDimensions(1.895f, 4.689f),
                CarType.Vehicle2 => new VehicleDimensions(1.852f, 4.572f),
                CarType.Vehicle3 => new VehicleDimensions(1.627f, 3.546f),
                CarType.Vehicle4 => new VehicleDimensions(1.744f, 3.876f),
                CarType.Vehicle5 => new VehicleDimensions(1.811f, 4.760f),
                CarType.Vehicle6 => new VehicleDimensions(1.839f, 4.879f),
                CarType.Vehicle7 => new VehicleDimensions(2.030f, 4.780f),
                CarType.Vehicle8 => new VehicleDimensions(1.811f, 4.624f),
                CarType.Vehicle9 => new VehicleDimensions(2.019f, 5.931f),
                CarType.Vehicle10 => new VehicleDimensions(0.749f, 2.085f),
                CarType.Vehicle11 => new VehicleDimensions(0.806f, 2.110f),
                CarType.Vehicle12 => new VehicleDimensions(0.690f, 2.055f),
                _ => new VehicleDimensions(1.8f, 4.5f)
            };
        }

        private static BotAudioProfile GetVehicleAudioProfile(CarType car)
        {
            return car switch
            {
                CarType.Vehicle1 => new BotAudioProfile(22050, 55000, 26000),
                CarType.Vehicle2 => new BotAudioProfile(22050, 60000, 35000),
                CarType.Vehicle3 => new BotAudioProfile(6000, 25000, 19000),
                CarType.Vehicle4 => new BotAudioProfile(6000, 27000, 20000),
                CarType.Vehicle5 => new BotAudioProfile(6000, 33000, 27500),
                CarType.Vehicle6 => new BotAudioProfile(7025, 40000, 32500),
                CarType.Vehicle7 => new BotAudioProfile(6000, 26000, 21000),
                CarType.Vehicle8 => new BotAudioProfile(10000, 45000, 34000),
                CarType.Vehicle9 => new BotAudioProfile(22050, 30550, 22550),
                CarType.Vehicle10 => new BotAudioProfile(22050, 60000, 35000),
                CarType.Vehicle11 => new BotAudioProfile(22050, 60000, 35000),
                CarType.Vehicle12 => new BotAudioProfile(22050, 27550, 23550),
                _ => new BotAudioProfile(22050, 55000, 26000)
            };
        }

        private void SendToRoom(RaceRoom room, byte[] payload, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            foreach (var id in room.PlayerIds)
            {
                if (_players.TryGetValue(id, out var player))
                    _transport.Send(player.EndPoint, payload, deliveryMethod);
            }
        }

        private void SendToRoomExcept(RaceRoom room, uint exceptId, byte[] payload, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            foreach (var id in room.PlayerIds)
            {
                if (id == exceptId)
                    continue;
                if (_players.TryGetValue(id, out var player))
                    _transport.Send(player.EndPoint, payload, deliveryMethod);
            }
        }

        private int CountActiveRaceParticipants(RaceRoom room)
        {
            var humanRacers = room.PlayerIds.Count(id => _players.TryGetValue(id, out var player) && IsActiveRaceState(player.State));
            var botRacers = room.Bots.Count(bot => IsActiveRaceState(bot.State));
            return humanRacers + botRacers;
        }

        private static bool IsActiveRaceState(PlayerState state)
        {
            return state == PlayerState.AwaitingStart || state == PlayerState.Racing;
        }

        private void SendRaceSnapshot(RaceRoom room, DeliveryMethod deliveryMethod)
        {
            _raceSnapshotSends++;
            _logger.Debug($"Race snapshot send: room={room.Id}, delivery={deliveryMethod}.");
            foreach (var id in room.PlayerIds)
            {
                if (!_players.TryGetValue(id, out var player))
                    continue;
                if (player.State == PlayerState.NotReady || player.State == PlayerState.Undefined)
                    continue;

                SendToRoomExcept(room, player.Id, PacketSerializer.WritePlayerData(player.ToPacket()), deliveryMethod);
                _stateSyncFramesSent++;
            }

            foreach (var bot in room.Bots)
            {
                if (bot.State == PlayerState.NotReady || bot.State == PlayerState.Undefined)
                    continue;

                var payload = PacketSerializer.WritePlayerData(ToBotPacket(bot));
                SendToRoom(room, payload, deliveryMethod);
                _stateSyncFramesSent++;
            }
        }

        private void BroadcastPlayerData()
        {
            foreach (var room in _rooms.Values)
            {
                foreach (var id in room.PlayerIds)
                {
                    if (!_players.TryGetValue(id, out var player))
                        continue;
                    if (player.State == PlayerState.NotReady || player.State == PlayerState.Undefined)
                        continue;

                    SendToRoomExcept(room, player.Id, PacketSerializer.WritePlayerData(player.ToPacket()), DeliveryMethod.Sequenced);
                    _stateSyncFramesSent++;
                }

                if (!room.RaceStarted)
                    continue;

                foreach (var bot in room.Bots)
                {
                    if (bot.State == PlayerState.NotReady || bot.State == PlayerState.Undefined)
                        continue;

                    var payload = PacketSerializer.WritePlayerData(ToBotPacket(bot));
                    SendToRoom(room, payload, DeliveryMethod.Sequenced);
                    _stateSyncFramesSent++;
                }
            }
        }

        private void UpdateBots(float deltaSeconds)
        {
            foreach (var room in _rooms.Values)
            {
                if (!room.RaceStarted)
                    continue;
                if (room.TrackData == null)
                    continue;

                var definitions = room.TrackData.Definitions;
                if (definitions == null || definitions.Length == 0)
                    continue;

                var lapDistance = GetLapDistance(room);
                var raceDistance = GetRaceDistance(room);
                if (lapDistance <= 0f || raceDistance <= 0f)
                    continue;
                var laneHalfWidth = GetLaneHalfWidth(room);

                foreach (var bot in room.Bots)
                {
                    if (bot.BackfirePulseSeconds > 0f)
                    {
                        bot.BackfirePulseSeconds -= deltaSeconds;
                        if (bot.BackfirePulseSeconds < 0f)
                            bot.BackfirePulseSeconds = 0f;
                    }

                    if (bot.HornSecondsRemaining > 0f)
                    {
                        bot.HornSecondsRemaining -= deltaSeconds;
                        if (bot.HornSecondsRemaining <= 0f)
                        {
                            bot.HornSecondsRemaining = 0f;
                            bot.Horning = false;
                        }
                    }

                    if (bot.State == PlayerState.Finished || bot.State == PlayerState.NotReady)
                        continue;

                    if (bot.State == PlayerState.AwaitingStart)
                    {
                        if (bot.StartDelaySeconds > 0f)
                        {
                            bot.StartDelaySeconds -= deltaSeconds;
                            if (bot.StartDelaySeconds > 0f)
                                continue;
                            bot.StartDelaySeconds = 0f;
                        }

                        if (bot.EngineStartSecondsRemaining <= 0f)
                        {
                            bot.EngineStartSecondsRemaining = BotRaceRules.DefaultBotEngineStartSeconds;
                            bot.SpeedKph = 0f;
                            bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                            continue;
                        }

                        bot.EngineStartSecondsRemaining -= deltaSeconds;
                        if (bot.EngineStartSecondsRemaining > 0f)
                        {
                            bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                            continue;
                        }

                        bot.EngineStartSecondsRemaining = 0f;
                        bot.State = PlayerState.Racing;
                        bot.RacePhase = BotRacePhase.Normal;
                        bot.CrashRecoverySeconds = 0f;
                        bot.SpeedKph = 0f;
                        bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                        bot.BackfireArmed = true;
                        _botStartEvents++;
                        _logger.Debug($"Bot started racing: room={room.Id}, bot={bot.Id}, number={bot.PlayerNumber}.");
                    }

                    if (bot.State != PlayerState.Racing)
                        continue;

                    if (bot.RacePhase == BotRacePhase.Crashing)
                    {
                        bot.CrashRecoverySeconds -= deltaSeconds;
                        bot.SpeedKph = 0f;
                        var crashState = bot.PhysicsState;
                        crashState.SpeedKph = 0f;
                        crashState.Gear = 1;
                        crashState.AutoShiftCooldownSeconds = 0f;
                        bot.PhysicsState = crashState;
                        bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                        bot.Horning = false;
                        bot.HornSecondsRemaining = 0f;
                        bot.BackfirePulseSeconds = 0f;
                        bot.BackfireArmed = true;
                        if (bot.CrashRecoverySeconds > 0f)
                            continue;

                        bot.CrashRecoverySeconds = 0f;
                        bot.RacePhase = BotRacePhase.Restarting;
                        bot.StartDelaySeconds = BotRaceRules.DefaultBotRestartDelaySeconds;
                        bot.EngineStartSecondsRemaining = 0f;
                        _botRestartEvents++;
                        _logger.Debug($"Bot restarting after crash: room={room.Id}, bot={bot.Id}, number={bot.PlayerNumber}, restartDelay={BotRaceRules.DefaultBotRestartDelaySeconds:0.00}s, startDelay={BotRaceRules.DefaultBotEngineStartSeconds:0.00}s.");
                        continue;
                    }

                    if (bot.RacePhase == BotRacePhase.Restarting)
                    {
                        bot.SpeedKph = 0f;
                        var restartState = bot.PhysicsState;
                        restartState.SpeedKph = 0f;
                        restartState.Gear = 1;
                        restartState.AutoShiftCooldownSeconds = 0f;
                        bot.PhysicsState = restartState;
                        bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                        bot.Horning = false;
                        bot.HornSecondsRemaining = 0f;
                        bot.BackfirePulseSeconds = 0f;
                        bot.BackfireArmed = true;
                        if (bot.StartDelaySeconds > 0f)
                        {
                            bot.StartDelaySeconds -= deltaSeconds;
                            if (bot.StartDelaySeconds > 0f)
                                continue;
                            bot.StartDelaySeconds = 0f;
                        }

                        if (bot.EngineStartSecondsRemaining <= 0f)
                        {
                            bot.EngineStartSecondsRemaining = BotRaceRules.DefaultBotEngineStartSeconds;
                            continue;
                        }

                        bot.EngineStartSecondsRemaining -= deltaSeconds;
                        if (bot.EngineStartSecondsRemaining > 0f)
                            continue;

                        bot.EngineStartSecondsRemaining = 0f;
                        bot.RacePhase = BotRacePhase.Normal;
                        _botResumeEvents++;
                        _logger.Debug($"Bot recovered and resumed: room={room.Id}, bot={bot.Id}, number={bot.PlayerNumber}.");
                        continue;
                    }

                    var currentRoad = BotRoadModel.RoadAtPosition(definitions, bot.PositionY, laneHalfWidth);
                    var nextRoad = BotRoadModel.RoadAtPosition(definitions, bot.PositionY + BotAiLookaheadMeters, laneHalfWidth);
                    var relPos = BotRaceRules.CalculateRelativeLanePosition(bot.PositionX, currentRoad.Left, laneHalfWidth);
                    relPos = Math.Max(0f, Math.Min(1f, relPos));
                    var controlRandom = (bot.AddedOrder * 37) % 100;
                    BotSharedModel.GetControlInputs((int)bot.Difficulty, controlRandom, currentRoad.Type, nextRoad.Type, relPos, out var throttle, out var steering);

                    var physicsState = bot.PhysicsState;
                    physicsState.PositionX = bot.PositionX;
                    physicsState.PositionY = bot.PositionY;
                    physicsState.SpeedKph = bot.SpeedKph;
                    if (physicsState.Gear <= 0)
                        physicsState.Gear = 1;

                    var physicsInput = new BotPhysicsInput(
                        deltaSeconds,
                        currentRoad.Surface,
                        (int)Math.Round(throttle),
                        brake: 0,
                        steering: (int)Math.Round(steering));
                    BotPhysics.Step(bot.PhysicsConfig, ref physicsState, in physicsInput);

                    bot.PhysicsState = physicsState;
                    bot.PositionX = physicsState.PositionX;
                    bot.PositionY = physicsState.PositionY;
                    bot.SpeedKph = physicsState.SpeedKph;
                    bot.EngineFrequency = CalculateBotEngineFrequency(bot, out var inShiftBand);
                    if (inShiftBand)
                    {
                        if (bot.BackfireArmed && _random.Next(5) == 0)
                        {
                            bot.BackfirePulseSeconds = BotBackfirePulseSeconds;
                            bot.BackfireArmed = false;
                        }
                    }
                    else
                    {
                        bot.BackfireArmed = true;
                    }
                    TryStartBotHorn(room, bot, raceDistance);

                    var evalRoad = BotRoadModel.RoadAtPosition(definitions, bot.PositionY, laneHalfWidth);
                    var evalRelPos = BotRaceRules.CalculateRelativeLanePosition(bot.PositionX, evalRoad.Left, laneHalfWidth);
                    if (BotRaceRules.IsOutsideRoad(evalRelPos))
                    {
                        var center = BotRaceRules.RoadCenter(evalRoad.Left, evalRoad.Right);
                        var fullCrash = BotRaceRules.IsFullCrash(physicsState.Gear, bot.SpeedKph);
                        if (fullCrash)
                        {
                            physicsState.PositionX = center;
                            physicsState.SpeedKph = 0f;
                            physicsState.Gear = 1;
                            physicsState.AutoShiftCooldownSeconds = 0f;
                            bot.PhysicsState = physicsState;
                            bot.PositionX = center;
                            bot.SpeedKph = 0f;
                            bot.EngineStartSecondsRemaining = 0f;
                            bot.StartDelaySeconds = 0f;
                            bot.RacePhase = BotRacePhase.Crashing;
                            bot.CrashRecoverySeconds = BotRaceRules.DefaultBotCrashRecoverySeconds;
                            bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                            bot.Horning = false;
                            bot.HornSecondsRemaining = 0f;
                            bot.BackfirePulseSeconds = 0f;
                            bot.BackfireArmed = true;
                            _botCrashEvents++;
                            _logger.Debug($"Bot crashed: room={room.Id}, bot={bot.Id}, number={bot.PlayerNumber}, y={bot.PositionY:0.0}.");
                            SendToRoom(room, PacketSerializer.WritePlayer(Command.PlayerCrashed, bot.Id, bot.PlayerNumber));
                            continue;
                        }

                        physicsState.PositionX = center;
                        physicsState.SpeedKph /= 4f;
                        bot.PhysicsState = physicsState;
                        bot.PositionX = center;
                        bot.SpeedKph = Math.Max(0f, physicsState.SpeedKph);
                    }

                    if (bot.PositionY < raceDistance)
                        continue;

                    bot.PositionY = raceDistance;
                    bot.SpeedKph = 0f;
                    bot.State = PlayerState.Finished;
                    bot.EngineFrequency = bot.AudioProfile.IdleFrequency;
                    bot.Horning = false;
                    bot.HornSecondsRemaining = 0f;
                    bot.BackfirePulseSeconds = 0f;
                    bot.BackfireArmed = true;
                    if (!room.RaceResults.Contains(bot.PlayerNumber))
                        room.RaceResults.Add(bot.PlayerNumber);

                    SendToRoom(room, PacketSerializer.WritePlayer(Command.PlayerFinished, bot.Id, bot.PlayerNumber));
                    _botFinishEvents++;
                    _logger.Debug($"Bot finished: room={room.Id}, bot={bot.Id}, number={bot.PlayerNumber}, place={room.RaceResults.Count}.");
                }

                if (CountActiveRaceParticipants(room) == 0)
                    StopRace(room);
            }
        }

        private static float GetLapDistance(RaceRoom room)
        {
            if (room.TrackData == null || room.TrackData.Definitions == null || room.TrackData.Definitions.Length == 0)
                return 0f;

            var lapDistance = 0f;
            foreach (var definition in room.TrackData.Definitions)
                lapDistance += Math.Max(1f, definition.Length);
            return lapDistance;
        }

        private float GetLaneHalfWidth(RaceRoom room)
        {
            return BotRaceRules.GetLaneHalfWidthForTrack(room.TrackName);
        }

        private float GetStartRowSpacing(RaceRoom room)
        {
            var maxLength = 4.5f;

            foreach (var playerId in room.PlayerIds)
            {
                if (_players.TryGetValue(playerId, out var player))
                    maxLength = Math.Max(maxLength, player.LengthM);
            }

            for (var i = 0; i < room.Bots.Count; i++)
                maxLength = Math.Max(maxLength, room.Bots[i].LengthM);

            return BotRaceRules.CalculateStartRowSpacing(maxLength);
        }

        private static float CalculateStartX(int gridIndex, float vehicleWidth, float laneHalfWidth)
        {
            return BotRaceRules.CalculateStartX(gridIndex, vehicleWidth, laneHalfWidth);
        }

        private static float CalculateStartY(int gridIndex, float rowSpacing)
        {
            return BotRaceRules.CalculateStartY(gridIndex, rowSpacing);
        }

        private static PacketPlayerData ToBotPacket(RoomBot bot)
        {
            return new PacketPlayerData
            {
                PlayerId = bot.Id,
                PlayerNumber = bot.PlayerNumber,
                Car = bot.Car,
                RaceData = new PlayerRaceData
                {
                    PositionX = bot.PositionX,
                    PositionY = bot.PositionY,
                    Speed = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, (int)Math.Round(bot.SpeedKph))),
                    Frequency = bot.EngineFrequency > 0 ? bot.EngineFrequency : bot.AudioProfile.IdleFrequency
                },
                State = bot.State,
                EngineRunning = (bot.State == PlayerState.Racing && bot.RacePhase == BotRacePhase.Normal)
                    || bot.EngineStartSecondsRemaining > 0f,
                Braking = false,
                Horning = bot.Horning,
                Backfiring = bot.BackfirePulseSeconds > 0f
            };
        }

        private static float GetRaceDistance(RaceRoom room)
        {
            var lapDistance = GetLapDistance(room);
            if (lapDistance <= 0f)
                return 0f;

            var laps = room.Laps > 0 ? room.Laps : (byte)1;
            return lapDistance * laps;
        }

        private void TryStartBotHorn(RaceRoom room, RoomBot bot, float raceDistance)
        {
            if (bot.Horning || bot.HornSecondsRemaining > 0f)
                return;
            if (raceDistance <= 0f)
                return;

            foreach (var id in room.PlayerIds)
            {
                if (!_players.TryGetValue(id, out var player))
                    continue;
                if (player.State != PlayerState.Racing && player.State != PlayerState.Finished)
                    continue;

                var delta = bot.PositionY - player.PositionY;
                if (delta < -BotHornMinDistanceMeters)
                {
                    if (_random.Next(2500) == 0)
                        TriggerBotHorn(bot, "overtake", 0.2f);
                    return;
                }
            }
        }

        private void TriggerBotHorn(RoomBot bot, string reason, float minDurationSeconds = 0.2f)
        {
            var duration = minDurationSeconds + (_random.Next(80) / 80.0f);
            if (duration <= bot.HornSecondsRemaining)
                return;

            bot.Horning = true;
            bot.HornSecondsRemaining = duration;
            if (string.Equals(reason, "overtake", StringComparison.Ordinal))
                _botHornOvertakeEvents++;
            else if (string.Equals(reason, "bump", StringComparison.Ordinal))
                _botHornBumpEvents++;

            _logger.Debug($"Bot horn triggered: bot={bot.Id}, number={bot.PlayerNumber}, reason={reason}, duration={duration:0.00}s.");
        }

        private static int CalculateBotEngineFrequency(RoomBot bot, out bool inShiftBand)
        {
            inShiftBand = false;
            var speedKph = Math.Max(0f, bot.SpeedKph);
            var config = bot.PhysicsConfig;
            var profile = bot.AudioProfile;

            var gearForSound = GetGearForSpeed(config, speedKph);
            if (!TryGetGearBand(config, gearForSound, out var gearMinKph, out var gearRangeKph))
                return profile.IdleFrequency;

            int frequency;
            if (gearForSound <= 1)
            {
                var gearSpeed = gearRangeKph <= 0f ? 0f : Math.Min(1.0f, speedKph / gearRangeKph);
                frequency = (int)(gearSpeed * (profile.TopFrequency - profile.IdleFrequency)) + profile.IdleFrequency;
            }
            else
            {
                var gearSpeed = (speedKph - gearMinKph) / gearRangeKph;
                if (gearSpeed < 0.07f)
                {
                    inShiftBand = true;
                    frequency = (int)(((0.07f - gearSpeed) / 0.07f) * (profile.TopFrequency - profile.ShiftFrequency) + profile.ShiftFrequency);
                }
                else
                {
                    if (gearSpeed > 1.0f)
                        gearSpeed = 1.0f;
                    frequency = (int)(gearSpeed * (profile.TopFrequency - profile.ShiftFrequency) + profile.ShiftFrequency);
                }
            }

            var minFrequency = Math.Max(1000, profile.IdleFrequency / 2);
            var maxFrequency = Math.Max(profile.TopFrequency, profile.TopFrequency * 2);
            if (frequency < minFrequency)
                frequency = minFrequency;
            if (frequency > maxFrequency)
                frequency = maxFrequency;
            return frequency;
        }

        private static int GetGearForSpeed(BotPhysicsConfig config, float speedKph)
        {
            var speedMps = Math.Max(0f, speedKph / 3.6f);
            var topSpeedMps = config.TopSpeedKph / 3.6f;
            var autoShiftRpm = config.IdleRpm + ((config.RevLimiter - config.IdleRpm) * 0.92f);
            for (var gear = 1; gear <= config.Gears; gear++)
            {
                var rpm = gear == config.Gears ? config.RevLimiter : autoShiftRpm;
                var gearMax = Math.Min(SpeedMpsFromRpm(config, rpm, gear), topSpeedMps);
                if (speedMps <= gearMax + 0.01f)
                    return gear;
            }

            return config.Gears;
        }

        private static bool TryGetGearBand(BotPhysicsConfig config, int gear, out float minSpeedKph, out float rangeKph)
        {
            minSpeedKph = 0f;
            rangeKph = 0f;

            if (config.Gears <= 0)
                return false;

            var clampedGear = gear;
            if (clampedGear < 1)
                clampedGear = 1;
            if (clampedGear > config.Gears)
                clampedGear = config.Gears;

            var maxSpeedMps = SpeedMpsFromRpm(config, config.RevLimiter, clampedGear);
            var shiftRpm = config.IdleRpm + ((config.RevLimiter - config.IdleRpm) * 0.35f);
            var minSpeedMps = clampedGear == 1 ? 0f : SpeedMpsFromRpm(config, shiftRpm, clampedGear);
            minSpeedKph = minSpeedMps * 3.6f;
            rangeKph = Math.Max(0.1f, (maxSpeedMps - minSpeedMps) * 3.6f);
            return true;
        }

        private static float SpeedMpsFromRpm(BotPhysicsConfig config, float rpm, int gear)
        {
            var ratio = config.GetGearRatio(gear) * config.FinalDriveRatio;
            if (ratio <= 0f)
                return 0f;

            var tireCircumference = config.WheelRadiusM * 2f * (float)Math.PI;
            return (rpm / ratio) * (tireCircumference / 60f);
        }

        private static ulong MakePairKey(uint first, uint second)
        {
            if (first > second)
            {
                var swap = first;
                first = second;
                second = swap;
            }

            return ((ulong)first << 32) | second;
        }

        private void CheckForBumps()
        {
            foreach (var room in _rooms.Values)
            {
                var racers = room.PlayerIds.Where(id => _players.TryGetValue(id, out var p) && p.State == PlayerState.Racing)
                    .Select(id => _players[id]).ToList();
                var botRacers = room.Bots.Where(bot => bot.State == PlayerState.Racing).ToList();
                var activePairs = new HashSet<ulong>();

                for (var i = 0; i < racers.Count; i++)
                {
                    for (var j = i + 1; j < racers.Count; j++)
                    {
                        var player = racers[i];
                        var other = racers[j];
                        var xThreshold = (player.WidthM + other.WidthM) * 0.5f;
                        var yThreshold = (player.LengthM + other.LengthM) * 0.5f;
                        var dx = player.PositionX - other.PositionX;
                        var dy = player.PositionY - other.PositionY;
                        if (Math.Abs(dx) >= xThreshold || Math.Abs(dy) >= yThreshold)
                            continue;

                        var pairKey = MakePairKey(player.Id, other.Id);
                        activePairs.Add(pairKey);
                        if (room.ActiveBumpPairs.Contains(pairKey))
                            continue;

                        _transport.Send(player.EndPoint, PacketSerializer.WritePlayerBumped(new PacketPlayerBumped
                        {
                            PlayerId = player.Id,
                            PlayerNumber = player.PlayerNumber,
                            BumpX = dx,
                            BumpY = dy,
                            BumpSpeed = (ushort)Math.Max(0, player.Speed - other.Speed)
                        }), DeliveryMethod.Sequenced);

                        _transport.Send(other.EndPoint, PacketSerializer.WritePlayerBumped(new PacketPlayerBumped
                        {
                            PlayerId = other.Id,
                            PlayerNumber = other.PlayerNumber,
                            BumpX = -dx,
                            BumpY = -dy,
                            BumpSpeed = (ushort)Math.Max(0, other.Speed - player.Speed)
                        }), DeliveryMethod.Sequenced);
                        _bumpEventsHumanHuman++;
                    }
                }

                foreach (var player in racers)
                {
                    foreach (var bot in botRacers)
                    {
                        var xThreshold = (player.WidthM + bot.WidthM) * 0.5f;
                        var yThreshold = (player.LengthM + bot.LengthM) * 0.5f;
                        var dx = player.PositionX - bot.PositionX;
                        var dy = player.PositionY - bot.PositionY;
                        if (Math.Abs(dx) >= xThreshold || Math.Abs(dy) >= yThreshold)
                            continue;

                        var pairKey = MakePairKey(player.Id, bot.Id);
                        activePairs.Add(pairKey);
                        if (room.ActiveBumpPairs.Contains(pairKey))
                            continue;

                        var botSpeed = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, (int)Math.Round(bot.SpeedKph)));
                        _transport.Send(player.EndPoint, PacketSerializer.WritePlayerBumped(new PacketPlayerBumped
                        {
                            PlayerId = player.Id,
                            PlayerNumber = player.PlayerNumber,
                            BumpX = dx,
                            BumpY = dy,
                            BumpSpeed = (ushort)Math.Max(0, player.Speed - botSpeed)
                        }), DeliveryMethod.Sequenced);

                        bot.PositionX -= 2f * dx;
                        bot.PositionY -= dy;
                        if (bot.PositionY < 0f)
                            bot.PositionY = 0f;
                        bot.SpeedKph = Math.Max(0f, bot.SpeedKph * 0.8f);
                        var state = bot.PhysicsState;
                        state.PositionX = bot.PositionX;
                        state.PositionY = bot.PositionY;
                        state.SpeedKph = bot.SpeedKph;
                        bot.PhysicsState = state;
                        TriggerBotHorn(bot, "bump", 0.2f);
                        _bumpEventsHumanBot++;
                    }
                }

                room.ActiveBumpPairs.RemoveWhere(key => !activePairs.Contains(key));
                foreach (var pairKey in activePairs)
                    room.ActiveBumpPairs.Add(pairKey);
            }
        }

        private void CleanupConnections()
        {
            var expired = _players.Values.Where(p => DateTime.UtcNow - p.LastSeenUtc > ConnectionTimeout).Select(p => p.Id).ToList();
            foreach (var id in expired)
            {
                if (!_players.TryGetValue(id, out var player))
                    continue;

                RemoveConnection(player, notifyRoom: true, sendDisconnectPacket: true, reason: "timeout");
            }
        }

        private void OnPeerDisconnected(IPEndPoint endpoint)
        {
            lock (_lock)
            {
                var key = endpoint.ToString();
                if (!_endpointIndex.TryGetValue(key, out var id))
                    return;
                if (!_players.TryGetValue(id, out var player))
                    return;

                RemoveConnection(player, notifyRoom: true, sendDisconnectPacket: false, reason: "peer_disconnect");
            }
        }

        private void RemoveConnection(PlayerConnection player, bool notifyRoom, bool sendDisconnectPacket, string reason)
        {
            var roomId = player.RoomId;
            if (player.RoomId.HasValue)
                HandleLeaveRoom(player, notifyRoom);
            if (sendDisconnectPacket)
                _transport.Send(player.EndPoint, PacketSerializer.WriteGeneral(Command.Disconnect));
            _endpointIndex.Remove(player.EndPoint.ToString());
            _players.Remove(player.Id);
            _logger.Info($"Connection removed: player={player.Id}, endpoint={player.EndPoint}, room={roomId?.ToString() ?? "none"}, reason={reason}.");
        }

        public ServerSnapshot GetSnapshot()
        {
            lock (_lock)
            {
                var raceStarted = _rooms.Values.Any(r => r.RaceStarted);
                var trackSelected = _rooms.Values.Any(r => r.TrackSelected);
                var trackName = _rooms.Count == 1 ? _rooms.Values.First().TrackName : (_rooms.Count > 1 ? "multiple" : string.Empty);
                return new ServerSnapshot(_config.Name ?? "TopSpeed Server", _config.Port, _config.MaxPlayers, _players.Count, raceStarted, trackSelected, trackName);
            }
        }

        public void Dispose()
        {
            _transport.Dispose();
        }
    }
}
