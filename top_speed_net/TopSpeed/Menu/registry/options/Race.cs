using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Input;

using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsRaceSettingsMenu()
        {
            var items = new List<MenuItem>
            {
                new RadioButton(LocalizationService.Mark("Copilot"),
                    new[]
                    {
                        LocalizationService.Mark("off"),
                        LocalizationService.Mark("curves only"),
                        LocalizationService.Mark("all")
                    },
                    () => (int)_settings.Copilot,
                    value => _settingsActions.UpdateSetting(() => _settings.Copilot = (CopilotMode)value),
                    hint: LocalizationService.Mark("Choose what information the copilot reports during the race. Use LEFT or RIGHT to change.")),
                new Switch(LocalizationService.Mark("Curve announcements"),
                    LocalizationService.Mark("speed dependent"),
                    LocalizationService.Mark("fixed distance"),
                    () => _settings.CurveAnnouncement == CurveAnnouncementMode.SpeedDependent,
                    value => _settingsActions.UpdateSetting(() => _settings.CurveAnnouncement = value ? CurveAnnouncementMode.SpeedDependent : CurveAnnouncementMode.FixedDistance),
                    hint: LocalizationService.Mark("Switch between fixed distance and speed dependent curve announcements. Press ENTER to change.")),
                new RadioButton(LocalizationService.Mark("Automatic race information"),
                    new[]
                    {
                        LocalizationService.Mark("off"),
                        LocalizationService.Mark("laps only"),
                        LocalizationService.Mark("on")
                    },
                    () => (int)_settings.AutomaticInfo,
                    value => _settingsActions.UpdateSetting(() => _settings.AutomaticInfo = (AutomaticInfoMode)value),
                    hint: LocalizationService.Mark("Choose how much automatic race information is spoken, such as lap numbers and player positions. Use LEFT or RIGHT to change.")),
                new Slider(LocalizationService.Mark("Number of laps"),
                    "1-16",
                    () => _settings.NrOfLaps,
                    value => _settingsActions.UpdateSetting(() => _settings.NrOfLaps = value),
                    hint: LocalizationService.Mark("Sets how many laps the session will be for single race, time trial, and multiplayer. Use LEFT or RIGHT to change by 1, PAGE UP or PAGE DOWN to change by 10, HOME for maximum, END for minimum.")),
                new Slider(LocalizationService.Mark("Number of computer players"),
                    "1-7",
                    () => _settings.NrOfComputers,
                    value => _settingsActions.UpdateSetting(() => _settings.NrOfComputers = value),
                    hint: LocalizationService.Mark("Sets how many computer-controlled cars will race against you. Use LEFT or RIGHT to change by 1, PAGE UP or PAGE DOWN to change by 10, HOME for maximum, END for minimum.")),
                new RadioButton(LocalizationService.Mark("Single race difficulty"),
                    new[]
                    {
                        LocalizationService.Mark("easy"),
                        LocalizationService.Mark("normal"),
                        LocalizationService.Mark("hard")
                    },
                    () => (int)_settings.Difficulty,
                    value => _settingsActions.UpdateSetting(() => _settings.Difficulty = (RaceDifficulty)value),
                    hint: LocalizationService.Mark("Choose the difficulty level for single races. Use LEFT or RIGHT to change.")),
                BackItem()
            };
            return _menu.CreateMenu("options_race", items);
        }

        private MenuScreen BuildOptionsLapsMenu()
        {
            var items = new List<MenuItem>();
            for (var laps = 1; laps <= 16; laps++)
            {
                var value = laps;
                items.Add(new MenuItem(laps.ToString(), MenuAction.Back, onActivate: () => _settingsActions.UpdateSetting(() => _settings.NrOfLaps = value)));
            }

            items.Add(BackItem());
            return _menu.CreateMenu("options_race_laps", items, LocalizationService.Mark("How many labs should the session be. This applys to single race, time trial and multiPlayer modes."));
        }

        private MenuScreen BuildOptionsComputersMenu()
        {
            var items = new List<MenuItem>();
            for (var count = 1; count <= 7; count++)
            {
                var value = count;
                items.Add(new MenuItem(count.ToString(), MenuAction.Back, onActivate: () => _settingsActions.UpdateSetting(() => _settings.NrOfComputers = value)));
            }

            items.Add(BackItem());
            return _menu.CreateMenu("options_race_computers", items, LocalizationService.Mark("Number of computer players"));
        }
    }
}

