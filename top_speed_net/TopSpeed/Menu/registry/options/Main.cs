using System.Collections.Generic;

using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Game settings"),
                    MenuAction.None,
                    nextMenuId: "options_game",
                    hint: LocalizationService.Mark("Configure general gameplay and interface behavior, including units, usage hints, menu behavior, and update checking.")),
                new MenuItem(LocalizationService.Mark("Audio"),
                    MenuAction.None,
                    nextMenuId: "options_audio",
                    hint: LocalizationService.Mark("Configure spatial audio behavior, including HRTF processing, stereo widening, and automatic device format detection.")),
                new MenuItem(LocalizationService.Mark("Volume settings"), MenuAction.None, nextMenuId: "options_volume",
                    onActivate: () =>
                    {
                        _settings.SyncAudioCategoriesFromMusicVolume();
                        _audio.ApplyAudioSettings();
                    },
                    hint: LocalizationService.Mark("Adjust category-based volume balance for engine sounds, effects, ambience, radio, music, and online events.")),
                new MenuItem(LocalizationService.Mark("Controls"),
                    MenuAction.None,
                    nextMenuId: "options_controls",
                    hint: LocalizationService.Mark("Configure input devices, force feedback, progressive keyboard behavior, key mappings, and menu shortcut mappings.")),
                new MenuItem(LocalizationService.Mark("Race settings"),
                    MenuAction.None,
                    nextMenuId: "options_race",
                    hint: LocalizationService.Mark("Set race behavior defaults such as copilot callouts, curve announcements, automatic info, laps, computer opponents, and difficulty.")),
                new MenuItem(LocalizationService.Mark("Server settings"),
                    MenuAction.None,
                    nextMenuId: "options_server",
                    hint: LocalizationService.Mark("Configure default multiplayer hosting settings, including the server port used by the game.")),
                new MenuItem(LocalizationService.Mark("Restore default settings"),
                    MenuAction.None,
                    nextMenuId: "options_restore",
                    hint: LocalizationService.Mark("Reset all configurable settings back to their default values.")),
                BackItem()
            };
            return _menu.CreateMenu("options_main", items);
        }
    }
}





