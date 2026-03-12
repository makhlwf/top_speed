using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Speech;
using TopSpeed.Windowing;
using TS.Audio;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private const string MultiplayerLobbyMenuId = "multiplayer_lobby";
        private const string MultiplayerRoomControlsMenuId = "multiplayer_room_controls";
        private const string MultiplayerRoomOptionsMenuId = "multiplayer_room_options";
        private const string MultiplayerRoomTrackTypeMenuId = "multiplayer_room_track_type";
        private const string MultiplayerRoomTrackRaceMenuId = "multiplayer_room_tracks_race";
        private const string MultiplayerRoomTrackAdventureMenuId = "multiplayer_room_tracks_adventure";
        private const string MultiplayerRoomPlayersMenuId = "multiplayer_room_players";
        private const string MultiplayerRoomBrowserMenuId = "multiplayer_rooms";
        private const string MultiplayerCreateRoomMenuId = "multiplayer_create_room";
        private const string MultiplayerLoadoutVehicleMenuId = "multiplayer_loadout_vehicle";
        private const string MultiplayerLoadoutTransmissionMenuId = "multiplayer_loadout_transmission";
        private const string MultiplayerSavedServersMenuId = "multiplayer_saved_servers";
        private const string MultiplayerSavedServerFormMenuId = "multiplayer_saved_server_form";
        private const string SharedLobbyChatScreenId = "shared_lobby_chat";
        private const int MaxChatMessages = 100;
        private static readonly string[] RoomTypeOptions = { "Race with bots", "Race without bots", "One-on-one without bots" };
        private static readonly string[] RoomCapacityOptions = BuildNumericOptions(2, ProtocolConstants.MaxRoomPlayersToStart, "players");
        private static readonly string[] LapCountOptions = BuildNumericOptions(1, 16, "laps");
        private static readonly TrackInfo[] RoomTrackOptions = BuildRoomTrackOptions();
        private const int ConnectingPulseIntervalMs = 500;

        private readonly MenuManager _menu;
        private readonly QuestionDialog _questions;
        private readonly DialogManager _dialogs;
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly RaceSettings _settings;
        private readonly MultiplayerConnector _connector;
        private readonly Action<string, string?, SpeechService.SpeakFlag, bool, Action<TextInputResult>> _promptTextInput;
        private readonly Action _saveSettings;
        private readonly Action _enterMenuState;
        private readonly Action<MultiplayerSession> _setSession;
        private readonly Func<MultiplayerSession?> _getSession;
        private readonly Action _clearSession;
        private readonly Action _resetPendingState;
        private readonly Action<int, bool> _setLocalMultiplayerLoadout;

        private Task<IReadOnlyList<ServerInfo>>? _discoveryTask;
        private CancellationTokenSource? _discoveryCts;
        private Task<ConnectResult>? _connectTask;
        private CancellationTokenSource? _connectCts;
        private string _pendingServerAddress = string.Empty;
        private int _pendingServerPort;
        private string _pendingCallSign = string.Empty;

        private PacketRoomList _roomList = new PacketRoomList();
        private PacketRoomState _roomState = new PacketRoomState { InRoom = false, Players = Array.Empty<PacketRoomPlayer>() };
        private bool _wasInRoom;
        private uint _lastRoomId;
        private bool _wasHost;
        private bool _roomBrowserOpenPending;
        private GameRoomType _createRoomType = GameRoomType.BotsRace;
        private byte _createRoomPlayersToStart = 2;
        private string _createRoomName = string.Empty;
        private int _pendingLoadoutVehicleIndex;
        private bool _roomOptionsDraftActive;
        private string _roomOptionsTrackName = string.Empty;
        private bool _roomOptionsTrackRandom;
        private byte _roomOptionsLaps = 1;
        private byte _roomOptionsPlayersToStart = 2;
        private CancellationTokenSource? _connectingSoundCts;
        private AudioSourceHandle? _connectingSound;
        private AudioSourceHandle? _connectedSound;
        private AudioSourceHandle? _onlineSound;
        private AudioSourceHandle? _offlineSound;
        private AudioSourceHandle? _pingStartSound;
        private AudioSourceHandle? _pingSound;
        private AudioSourceHandle? _roomCreatedSound;
        private AudioSourceHandle? _roomJoinSound;
        private AudioSourceHandle? _roomLeaveSound;
        private AudioSourceHandle? _chatSound;
        private AudioSourceHandle? _roomChatSound;
        private AudioSourceHandle? _bufferSwitchSound;
        private bool _pingPending;
        private long _pingStartedAtMs;
        private readonly Chat.HistoryBuffers _historyBuffers = new Chat.HistoryBuffers(MaxChatMessages);
        private SavedServerEntry _savedServerDraft = new SavedServerEntry();
        private SavedServerEntry? _savedServerOriginal;
        private int _savedServerEditIndex = -1;
        private int _pendingDeleteServerIndex = -1;
        private bool _hasPendingCompatibilityResult;
        private ConnectResult _pendingCompatibilityResult;
        public QuestionDialog Questions => _questions;

        public MultiplayerCoordinator(
            MenuManager menu,
            DialogManager dialogs,
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            MultiplayerConnector connector,
            Action<string, string?, SpeechService.SpeakFlag, bool, Action<TextInputResult>> promptTextInput,
            Action saveSettings,
            Action enterMenuState,
            Action<MultiplayerSession> setSession,
            Func<MultiplayerSession?> getSession,
            Action clearSession,
            Action resetPendingState,
            Action<int, bool> setLocalMultiplayerLoadout)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _questions = new QuestionDialog(_menu);
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _speech = speech ?? throw new ArgumentNullException(nameof(speech));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _connector = connector ?? throw new ArgumentNullException(nameof(connector));
            _promptTextInput = promptTextInput ?? throw new ArgumentNullException(nameof(promptTextInput));
            _saveSettings = saveSettings ?? throw new ArgumentNullException(nameof(saveSettings));
            _enterMenuState = enterMenuState ?? throw new ArgumentNullException(nameof(enterMenuState));
            _setSession = setSession ?? throw new ArgumentNullException(nameof(setSession));
            _getSession = getSession ?? throw new ArgumentNullException(nameof(getSession));
            _clearSession = clearSession ?? throw new ArgumentNullException(nameof(clearSession));
            _resetPendingState = resetPendingState ?? throw new ArgumentNullException(nameof(resetPendingState));
            _setLocalMultiplayerLoadout = setLocalMultiplayerLoadout ?? throw new ArgumentNullException(nameof(setLocalMultiplayerLoadout));
        }
    }
}
