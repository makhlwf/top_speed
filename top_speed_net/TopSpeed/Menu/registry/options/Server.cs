using System.Collections.Generic;
using TopSpeed.Localization;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsServerSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(
                    () => LocalizationService.Format(
                        LocalizationService.Mark("Default server port: {0}"),
                        FormatServerPort(_settings.DefaultServerPort)),
                    MenuAction.None,
                    onActivate: _server.BeginServerPortEntry),
                BackItem()
            };
            return _menu.CreateMenu("options_server", items);
        }
    }
}

