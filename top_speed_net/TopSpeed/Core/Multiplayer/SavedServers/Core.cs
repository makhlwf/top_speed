using System.Collections.Generic;
using TopSpeed.Input;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void OpenSavedServersManager()
        {
            RebuildSavedServersMenu();
            _menu.Push(MultiplayerSavedServersMenuId);
        }

        private IReadOnlyList<SavedServerEntry> SavedServers => _settings.SavedServers ?? (_settings.SavedServers = new List<SavedServerEntry>());
    }
}
