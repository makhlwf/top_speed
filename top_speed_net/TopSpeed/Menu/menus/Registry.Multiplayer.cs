using System.Collections.Generic;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildMultiplayerMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Join a game on the local network", MenuAction.None, onActivate: _server.StartServerDiscovery),
                new MenuItem("Manage saved servers", MenuAction.None, onActivate: _server.OpenSavedServersManager),
                new MenuItem("Enter the IP address or domain manually", MenuAction.None, onActivate: _server.BeginManualServerEntry),
                BackItem()
            };
            return _menu.CreateMenu("multiplayer", items);
        }

        private MenuScreen BuildMultiplayerServersMenu()
        {
            var items = new List<MenuItem>
            {
                BackItem()
            };
            return _menu.CreateMenu("multiplayer_servers", items, "Available servers");
        }

        private MenuScreen BuildMultiplayerLobbyMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Create a new game", MenuAction.None, onActivate: _ui.SpeakNotImplemented),
                new MenuItem("Join an existing game", MenuAction.None, onActivate: _ui.SpeakNotImplemented),
                new MenuItem("Options", MenuAction.None, nextMenuId: "options_main"),
                new MenuItem("Disconnect", MenuAction.None, flags: MenuItemFlags.Close)
            };
            return _menu.CreateMenu("multiplayer_lobby", items, string.Empty);
        }

        private MenuScreen BuildMultiplayerSavedServersMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Add a new server", MenuAction.None),
                BackItem()
            };
            return _menu.CreateMenu("multiplayer_saved_servers", items, string.Empty);
        }

        private MenuScreen BuildMultiplayerSavedServerFormMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Server form is loading", MenuAction.None),
                BackItem()
            };
            return _menu.CreateMenu("multiplayer_saved_server_form", items, "Server details");
        }

        private MenuScreen BuildMultiplayerRoomsMenu()
        {
            var items = new List<MenuItem>
            {
                BackItem()
            };
            return _menu.CreateMenu("multiplayer_rooms", items, "Available game rooms");
        }

        private MenuScreen BuildMultiplayerCreateRoomMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Create room controls are loading", MenuAction.None),
                BackItem()
            };
            return _menu.CreateMenu("multiplayer_create_room", items);
        }

        private MenuScreen BuildMultiplayerRoomControlsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Join a game room first", MenuAction.None),
                BackItem()
            };
            return _menu.CreateMenu("multiplayer_room_controls", items, "Room controls");
        }

        private MenuScreen BuildMultiplayerRoomPlayersMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Join a game room first", MenuAction.None),
                BackItem()
            };
            return _menu.CreateMenu("multiplayer_room_players", items, "Players in room");
        }

        private MenuScreen BuildMultiplayerRoomOptionsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Join a game room first", MenuAction.None),
                BackItem()
            };
            return _menu.CreateMenu("multiplayer_room_options", items, "Change game options");
        }

        private MenuScreen BuildMultiplayerLoadoutVehicleMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Vehicle selection is loading", MenuAction.None)
            };
            return _menu.CreateMenu("multiplayer_loadout_vehicle", items, "Choose your vehicle");
        }

        private MenuScreen BuildMultiplayerLoadoutTransmissionMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Transmission selection is loading", MenuAction.None),
                BackItem()
            };
            return _menu.CreateMenu("multiplayer_loadout_transmission", items, "Choose your transmission mode");
        }
    }
}
