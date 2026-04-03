using System.Collections.Generic;

using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsRestoreMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Yes"), MenuAction.Back, onActivate: _settingsActions.RestoreDefaults),
                new MenuItem(LocalizationService.Mark("No"), MenuAction.Back),
                BackItem()
            };
            return _menu.CreateMenu("options_restore", items, LocalizationService.Mark("Are you sure you would like to restore all settings to their default values?"));
        }
    }
}





