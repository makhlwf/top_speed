using System;
using TopSpeed.Localization;
using TopSpeed.Menu;
using Key = TopSpeed.Input.InputKey;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public bool IsInRoom => _roomsFlow.IsInRoom;
        internal bool IsInRoomCore => _state.Rooms.CurrentRoom.InRoom;

        private const string MultiplayerPingShortcutActionId = "multiplayer_ping";
        private const string MultiplayerChatShortcutActionId = "multiplayer_chat";
        private const string MultiplayerRoomChatShortcutActionId = "multiplayer_room_chat";
        private const string MultiplayerRoomRulesShortcutActionId = "multiplayer_room_rules";
        private const string MultiplayerShortcutScopeId = "multiplayer";

        private static readonly string[] MultiplayerScopeMenus =
        {
            MultiplayerMenuKeys.Lobby,
            MultiplayerMenuKeys.RoomBrowser,
            MultiplayerMenuKeys.CreateRoom,
            MultiplayerMenuKeys.RoomControls,
            MultiplayerMenuKeys.RoomPlayers,
            MultiplayerMenuKeys.OnlinePlayers,
            MultiplayerMenuKeys.RoomOptions,
            MultiplayerMenuKeys.RoomGameRules,
            MultiplayerMenuKeys.RoomTrackType,
            MultiplayerMenuKeys.RoomTrackRace,
            MultiplayerMenuKeys.RoomTrackAdventure,
            MultiplayerMenuKeys.LoadoutVehicle,
            MultiplayerMenuKeys.LoadoutTransmission
        };

        public void ConfigureMenuCloseHandlers()
        {
            _roomsFlow.ConfigureMenuCloseHandlers();
        }

        internal void ConfigureMenuCloseHandlersCore()
        {
            _menu.RegisterShortcutAction(
                MultiplayerPingShortcutActionId,
                LocalizationService.Mark("Check ping"),
                LocalizationService.Mark("Speaks your current ping while you are in multiplayer menus."),
                Key.F1,
                CheckCurrentPing);

            _menu.RegisterShortcutAction(
                MultiplayerChatShortcutActionId,
                LocalizationService.Mark("Open global chat"),
                LocalizationService.Mark("Opens chat input for the global multiplayer lobby chat."),
                Key.Slash,
                OpenGlobalChatInput);

            _menu.RegisterShortcutAction(
                MultiplayerRoomChatShortcutActionId,
                LocalizationService.Mark("Open room chat"),
                LocalizationService.Mark("Opens chat input for the current room chat when you are inside a room."),
                Key.Backslash,
                OpenRoomChatInput,
                () => IsInRoomCore);

            _menu.RegisterShortcutAction(
                MultiplayerRoomRulesShortcutActionId,
                LocalizationService.Mark("View game rules"),
                LocalizationService.Mark("Speaks currently active game rules for the current game room."),
                Key.R,
                AnnounceCurrentRoomGameRules,
                () => IsInRoomCore);

            _menu.SetScopeShortcutActions(
                MultiplayerShortcutScopeId,
                new[]
                {
                    MultiplayerPingShortcutActionId,
                    MultiplayerChatShortcutActionId,
                    MultiplayerRoomChatShortcutActionId
                },
                LocalizationService.Mark("Multiplayer shortcuts"));

            for (var i = 0; i < MultiplayerScopeMenus.Length; i++)
            {
                _menu.SetMenuShortcutScopes(
                    MultiplayerScopeMenus[i],
                    new[] { MultiplayerShortcutScopeId });
            }

            _menu.SetCloseHandler(MultiplayerMenuKeys.Lobby, _ =>
            {
                OpenDisconnectConfirmation();
                return true;
            });

            _menu.SetCloseHandler(MultiplayerMenuKeys.RoomControls, _ =>
            {
                OpenLeaveRoomConfirmation();
                return true;
            });

            _menu.SetCloseHandler(MultiplayerMenuKeys.SavedServerForm, _ =>
            {
                CloseSavedServerForm();
                return true;
            });

            _menu.SetCloseHandler(MultiplayerMenuKeys.RoomOptions, _ =>
            {
                CancelRoomOptionsChanges();
                return false;
            });

            _menu.SetMenuShortcutActions(
                MultiplayerMenuKeys.RoomControls,
                new[] { MultiplayerRoomRulesShortcutActionId },
                LocalizationService.Mark("Room controls"));

            _menu.SetCloseHandler(MultiplayerMenuKeys.LoadoutTransmission, _ =>
            {
                OpenLoadoutExitConfirmation();
                return true;
            });

            _menu.SetCloseHandler(MultiplayerMenuKeys.LoadoutVehicle, _ =>
            {
                OpenLoadoutExitConfirmation();
                return true;
            });
        }

        public void ShowMultiplayerMenuAfterRace()
        {
            _roomsFlow.ShowMultiplayerMenuAfterRace();
        }

        internal void ShowMultiplayerMenuAfterRaceCore()
        {
            if (_state.Rooms.CurrentRoom.InRoom)
                _menu.ShowRoot(MultiplayerMenuKeys.RoomControls);
            else
                _menu.ShowRoot(MultiplayerMenuKeys.Lobby);
        }

        public void BeginRaceLoadoutSelection()
        {
            _roomsFlow.BeginRaceLoadoutSelection();
        }

        internal void BeginRaceLoadoutSelectionCore()
        {
            if (!_state.Rooms.CurrentRoom.InRoom)
                return;

            _state.RoomDrafts.PendingLoadoutVehicleIndex = 0;
            RebuildLoadoutVehicleMenu();
            RebuildLoadoutTransmissionMenu();
            _menu.ShowRoot(MultiplayerMenuKeys.LoadoutVehicle);
            _enterMenuState();
        }
    }
}





