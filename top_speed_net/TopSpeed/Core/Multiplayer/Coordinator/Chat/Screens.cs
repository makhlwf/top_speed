using System;
using System.Collections.Generic;
using TopSpeed.Menu;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void UpdateHistoryScreens()
        {
            var items = _historyBuffers.GetCurrentItems();
            TryUpdateChatScreen(MultiplayerLobbyMenuId, items);
            TryUpdateChatScreen(MultiplayerRoomControlsMenuId, items);
        }

        private void TryUpdateChatScreen(string menuId, IEnumerable<MenuItem> items)
        {
            try
            {
                _menu.UpdateItems(menuId, SharedLobbyChatScreenId, items, preserveSelection: true);
            }
            catch (InvalidOperationException)
            {
                // Menus may not be registered yet during startup.
            }
        }

        private static string? NormalizeChatMessage(string message)
        {
            var text = (message ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
    }
}
