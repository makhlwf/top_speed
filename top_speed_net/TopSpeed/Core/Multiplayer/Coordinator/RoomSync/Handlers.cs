using System;
using TopSpeed.Core.Multiplayer.Chat;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void HandleRoomList(PacketRoomList roomList)
        {
            _roomList = roomList ?? new PacketRoomList();
            if (!_roomBrowserOpenPending)
                return;

            _roomBrowserOpenPending = false;
            if (!string.Equals(_menu.CurrentId, MultiplayerLobbyMenuId, StringComparison.Ordinal))
                return;

            var rooms = _roomList.Rooms ?? Array.Empty<PacketRoomSummary>();
            if (rooms.Length == 0)
            {
                _speech.Speak("No game rooms are currently available.");
                return;
            }

            UpdateRoomBrowserMenu();
            _menu.Push(MultiplayerRoomBrowserMenuId);
        }

        public void HandleRoomState(PacketRoomState roomState)
        {
            var wasInRoom = _wasInRoom;
            var previousRoomId = _lastRoomId;
            var previousIsHost = _wasHost;
            var previousRoomType = _roomState.RoomType;
            _roomState = roomState ?? new PacketRoomState { InRoom = false, Players = Array.Empty<PacketRoomPlayer>() };

            if (_roomState.InRoom)
            {
                if (!wasInRoom || previousRoomId != _roomState.RoomId)
                {
                    PlayNetworkSound("room_join.ogg");
                    AddRoomEventMessage(HistoryText.JoinedRoom(_roomState.RoomName));
                }
            }
            else if (wasInRoom)
            {
                PlayNetworkSound("room_leave.ogg");
                var leaveText = HistoryText.LeftRoom();
                _speech.Speak(leaveText);
                AddRoomEventMessage(leaveText);
            }

            _wasInRoom = _roomState.InRoom;
            _lastRoomId = _roomState.RoomId;
            _wasHost = _roomState.IsHost;
            if (!_roomState.InRoom || !_roomState.IsHost)
                CancelRoomOptionsChanges();

            if (_roomState.InRoom && (!wasInRoom || previousRoomId != _roomState.RoomId))
            {
                _menu.ShowRoot(MultiplayerRoomControlsMenuId);
            }
            else if (!_roomState.InRoom && wasInRoom)
            {
                _menu.ShowRoot(MultiplayerLobbyMenuId);
            }

            var roomControlsChanged =
                wasInRoom != _roomState.InRoom ||
                previousIsHost != _roomState.IsHost ||
                previousRoomType != _roomState.RoomType;
            if (roomControlsChanged)
            {
                RebuildRoomControlsMenu();
                RebuildRoomOptionsMenu();
            }

            RebuildRoomPlayersMenu();
        }

        public void HandleRoomEvent(PacketRoomEvent roomEvent)
        {
            if (roomEvent == null)
                return;

            if (roomEvent.Kind == RoomEventKind.RoomCreated)
            {
                var session = SessionOrNull();
                var isCreator = session != null && roomEvent.HostPlayerId == session.PlayerId;
                if (!isCreator)
                    PlayNetworkSound("room_created.ogg");
            }

            var roomEventText = HistoryText.FromRoomEvent(roomEvent);
            if (!string.IsNullOrWhiteSpace(roomEventText))
                AddRoomEventMessage(roomEventText);

            ApplyRoomListEvent(roomEvent);

            ApplyCurrentRoomEvent(roomEvent, out var beginLoadout, out var localHostChanged);
            if (localHostChanged)
            {
                RebuildRoomControlsMenu();
                RebuildRoomOptionsMenu();
            }
            if (_roomState.InRoom)
                RebuildRoomPlayersMenu();

            if (beginLoadout)
                BeginRaceLoadoutSelection();
        }

        public void HandleProtocolMessage(PacketProtocolMessage message)
        {
            if (message == null)
                return;

            if (message.Code == ProtocolMessageCode.ServerPlayerConnected)
            {
                PlayNetworkSound("online.ogg");
                AddConnectionMessage(message.Message);
            }
            else if (message.Code == ProtocolMessageCode.ServerPlayerDisconnected)
            {
                PlayNetworkSound("offline.ogg");
                AddConnectionMessage(message.Message);
            }
            else if (message.Code == ProtocolMessageCode.Chat)
            {
                PlayNetworkSound("chat.ogg");
                AddGlobalChatMessage(message.Message);
            }
            else if (message.Code == ProtocolMessageCode.RoomChat)
            {
                PlayNetworkSound("room_chat.ogg");
                AddRoomChatMessage(message.Message);
            }
            else
            {
                AddRoomEventMessage(message.Message);
            }

            if (!string.IsNullOrWhiteSpace(message.Message))
                _speech.Speak(message.Message);
        }
    }
}
