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
                new MenuItem(
                    () => string.IsNullOrWhiteSpace(_settings.DefaultCallSign)
                        ? LocalizationService.Mark("Default call sign: not set")
                        : LocalizationService.Format(
                            LocalizationService.Mark("Default call sign: {0}"),
                            _settings.DefaultCallSign),
                    MenuAction.None,
                    onActivate: _server.BeginDefaultCallSignEntry)
            };
            return BackMenu("options_server", items);
        }
    }
}

