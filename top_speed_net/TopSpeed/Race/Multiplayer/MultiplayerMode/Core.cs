using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Network;
using TopSpeed.Network.Live;
using TopSpeed.Protocol;
using TopSpeed.Race.Multiplayer;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TS.Audio;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Race
{
    internal sealed partial class MultiplayerMode : RaceMode
    {
        private const int MaxPlayers = ProtocolConstants.MaxPlayers;
        private const float SendIntervalSeconds = 1f / 60f;
        private const float StartLineY = 140.0f;
        private const float ServerTickRate = 125.0f;
        private const float SnapshotDelayTicks = 4.0f;
        private const int SnapshotBufferMax = 8;

        private readonly MultiplayerSession _session;
        private readonly Func<byte, string> _resolvePlayerName;
        private readonly uint _playerId;
        private readonly byte _playerNumber;
        private readonly Dictionary<byte, RemotePlayer> _remotePlayers;
        private readonly Dictionary<byte, MediaTransfer> _remoteMediaTransfers;
        private readonly Dictionary<byte, Multiplayer.LiveState> _remoteLiveStates;
        private readonly List<byte> _expiredLivePlayers;
        private readonly List<SnapshotFrame> _snapshotFrames;
        private readonly AudioSourceHandle?[] _soundPosition;
        private readonly AudioSourceHandle?[] _soundPlayerNr;
        private readonly AudioSourceHandle?[] _soundFinished;
        private readonly bool[] _disconnectedPlayerSlots;
        private readonly Tx _liveTx;

        private AudioSourceHandle? _soundYouAre;
        private AudioSourceHandle? _soundPlayer;
        private float _lastComment;
        private bool _infoKeyReleased;
        private int _positionFinish;
        private int _position;
        private int _positionComment;
        private bool _pauseKeyReleased = true;
        private float _sendAccumulator;
        private bool _sentStart;
        private bool _sentFinish;
        private bool _serverStopReceived;
        private PlayerState _currentState;
        private CarState _lastCarState;
        private uint _lastRaceSnapshotSequence;
        private uint _lastRaceSnapshotTick;
        private bool _hasRaceSnapshotSequence;
        private float _snapshotTickNow;
        private bool _hasSnapshotTickNow;
        private bool _sendFailureAnnounced;
        private bool _liveFailureAnnounced;

        public MultiplayerMode(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            RaceInput input,
            TrackData trackData,
            string trackName,
            bool automaticTransmission,
            int nrOfLaps,
            int vehicle,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice,
            MultiplayerSession session,
            uint playerId,
            byte playerNumber,
            Func<byte, string> resolvePlayerName)
            : base(audio, speech, settings, input, trackName, automaticTransmission, nrOfLaps, vehicle, vehicleFile, vibrationDevice, trackData, trackData.UserDefined)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _resolvePlayerName = resolvePlayerName ?? throw new ArgumentNullException(nameof(resolvePlayerName));
            _playerId = playerId;
            _playerNumber = playerNumber;
            _remotePlayers = new Dictionary<byte, RemotePlayer>();
            _remoteMediaTransfers = new Dictionary<byte, MediaTransfer>();
            _remoteLiveStates = new Dictionary<byte, Multiplayer.LiveState>();
            _expiredLivePlayers = new List<byte>();
            _snapshotFrames = new List<SnapshotFrame>(SnapshotBufferMax);
            _soundPosition = new AudioSourceHandle?[MaxPlayers];
            _soundPlayerNr = new AudioSourceHandle?[MaxPlayers];
            _soundFinished = new AudioSourceHandle?[MaxPlayers];
            _disconnectedPlayerSlots = new bool[MaxPlayers];
            _liveTx = new Tx(_session);
            _currentState = PlayerState.NotReady;
        }
    }
}

