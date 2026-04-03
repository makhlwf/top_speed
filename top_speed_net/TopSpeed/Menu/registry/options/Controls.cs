using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Shortcuts;

using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private const string ShortcutGroupsMenuId = "options_controls_shortcuts";
        private const string ShortcutBindingsMenuId = "options_controls_shortcut_bindings";

        private string _activeShortcutGroupId = string.Empty;

        private MenuScreen BuildOptionsControlsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(
                    () => LocalizationService.Format(
                        LocalizationService.Mark("Select device: {0}"),
                        DeviceLabel(_settings.DeviceMode)),
                    MenuAction.None,
                    nextMenuId: "options_controls_device"),
                new CheckBox(LocalizationService.Mark("Force feedback"),
                    () => _settings.ForceFeedback,
                    value => _settingsActions.UpdateSetting(() => _settings.ForceFeedback = value),
                    hint: LocalizationService.Mark("Enables force feedback or vibration if your controller supports it. Press ENTER to toggle.")),
                new RadioButton(LocalizationService.Mark("Progressive keyboard input"),
                    new[]
                    {
                        LocalizationService.Mark("Off"),
                        LocalizationService.Mark("Fastest (0.25 seconds)"),
                        LocalizationService.Mark("Fast (0.50 seconds)"),
                        LocalizationService.Mark("Moderate (0.75 seconds)"),
                        LocalizationService.Mark("Slowest (1.00 second)")
                    },
                    () => (int)_settings.KeyboardProgressiveRate,
                    value => _settingsActions.UpdateSetting(() => _settings.KeyboardProgressiveRate = (KeyboardProgressiveRate)value),
                    hint: LocalizationService.Mark("When enabled, throttle, brake, and steering ramp in over time instead of jumping instantly to full value. Press LEFT or RIGHT to change.")),
                new MenuItem(LocalizationService.Mark("Map keyboard keys"), MenuAction.None, nextMenuId: "options_controls_keyboard"),
                new MenuItem(LocalizationService.Mark("Map controller keys"), MenuAction.None, nextMenuId: "options_controls_controller"),
                new MenuItem(LocalizationService.Mark("Map menu shortcuts"),
                    MenuAction.None,
                    nextMenuId: ShortcutGroupsMenuId,
                    onActivate: RebuildShortcutGroupsMenu),
                BackItem()
            };
            return _menu.CreateMenu("options_controls", items);
        }

        private MenuScreen BuildOptionsControlsDeviceMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Keyboard"), MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Keyboard)),
                new MenuItem(LocalizationService.Mark("Controller"), MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Controller)),
                new MenuItem(LocalizationService.Mark("Both"), MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Both)),
                BackItem()
            };
            return _menu.CreateMenu("options_controls_device", items, LocalizationService.Mark("Select input device"));
        }

        private MenuScreen BuildOptionsControlsKeyboardMenu()
        {
            return _menu.CreateMenu("options_controls_keyboard", BuildMappingItems(InputMappingMode.Keyboard));
        }

        private MenuScreen BuildOptionsControlsControllerMenu()
        {
            var items = new List<MenuItem>
            {
                new RadioButton(LocalizationService.Mark("Throttle pedal direction"),
                    new[]
                    {
                        LocalizationService.Mark("Auto"),
                        LocalizationService.Mark("Normal"),
                        LocalizationService.Mark("Inverted")
                    },
                    () => (int)_settings.ControllerThrottleInvertMode,
                    value => _settingsActions.UpdateSetting(() => _settings.ControllerThrottleInvertMode = (PedalInvertMode)value),
                    hint: LocalizationService.Mark("Auto detects wheel pedal direction from resting position. Use LEFT or RIGHT to change.")),
                new RadioButton(LocalizationService.Mark("Brake pedal direction"),
                    new[]
                    {
                        LocalizationService.Mark("Auto"),
                        LocalizationService.Mark("Normal"),
                        LocalizationService.Mark("Inverted")
                    },
                    () => (int)_settings.ControllerBrakeInvertMode,
                    value => _settingsActions.UpdateSetting(() => _settings.ControllerBrakeInvertMode = (PedalInvertMode)value),
                    hint: LocalizationService.Mark("Auto detects wheel pedal direction from resting position. Use LEFT or RIGHT to change.")),
                new RadioButton(LocalizationService.Mark("Steering dead zone"),
                    new[]
                    {
                        LocalizationService.Mark("Default (1 degree)"),
                        LocalizationService.Mark("2 degrees"),
                        LocalizationService.Mark("3 degrees"),
                        LocalizationService.Mark("4 degrees"),
                        LocalizationService.Mark("5 degrees")
                    },
                    () =>
                    {
                        var deadZone = _settings.ControllerSteeringDeadZone;
                        if (deadZone < 1 || deadZone > 5)
                            deadZone = 1;
                        return deadZone - 1;
                    },
                    value =>
                    {
                        var deadZone = value + 1;
                        if (deadZone < 1 || deadZone > 5)
                            deadZone = 1;
                        _settingsActions.UpdateSetting(() => _settings.ControllerSteeringDeadZone = deadZone);
                    },
                    hint: LocalizationService.Mark("Sets how much small steering movement is ignored around center. Default is 1 degree. Use LEFT or RIGHT to change."))
            };

            items.AddRange(BuildMappingItems(InputMappingMode.Controller, includeBack: false));
            items.Add(BackItem());
            return _menu.CreateMenu("options_controls_controller", items);
        }

        private MenuScreen BuildOptionsControlsShortcutGroupsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Global shortcuts"), MenuAction.None),
                BackItem()
            };
            return _menu.CreateMenu(ShortcutGroupsMenuId, items, title: string.Empty);
        }

        private MenuScreen BuildOptionsControlsShortcutBindingsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("No shortcuts in this group."), MenuAction.None),
                BackItem()
            };
            return _menu.CreateMenu(ShortcutBindingsMenuId, items, title: string.Empty);
        }

        private List<MenuItem> BuildMappingItems(InputMappingMode mode, bool includeBack = true)
        {
            var items = new List<MenuItem>();
            foreach (var action in _raceInput.KeyMap.Actions)
            {
                var definition = action;
                items.Add(new MenuItem(
                    () => $"{definition.Label}: {_mapping.FormatMappingValue(definition.Action, mode)}",
                    MenuAction.None,
                    onActivate: () => _mapping.BeginMapping(mode, definition.Action)));
            }

            if (includeBack)
                items.Add(BackItem());
            return items;
        }

        private void RebuildShortcutGroupsMenu()
        {
            var groups = _menu.GetShortcutGroups();
            var items = new List<MenuItem>();
            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                items.Add(new MenuItem(
                    group.Name,
                    MenuAction.None,
                    onActivate: () => OpenShortcutGroup(group)));
            }

            items.Add(BackItem());
            _menu.UpdateItems(ShortcutGroupsMenuId, items, preserveSelection: true);
        }

        private void OpenShortcutGroup(ShortcutGroup group)
        {
            _activeShortcutGroupId = group.Id;
            if (!RebuildShortcutBindingsMenu())
            {
                _ui.SpeakMessage(LocalizationService.Format(LocalizationService.Mark("{0} has no shortcuts."), group.Name));
                return;
            }

            _menu.Push(ShortcutBindingsMenuId);
        }

        private bool RebuildShortcutBindingsMenu()
        {
            var items = new List<MenuItem>();
            if (string.IsNullOrWhiteSpace(_activeShortcutGroupId))
            {
                items.Add(new MenuItem(LocalizationService.Mark("No shortcut group selected."), MenuAction.None));
                items.Add(BackItem());
                _menu.UpdateItems(ShortcutBindingsMenuId, items, preserveSelection: true);
                return false;
            }

            var bindings = _menu.GetShortcutBindings(_activeShortcutGroupId);
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                var actionId = binding.ActionId;
                var displayName = binding.DisplayName;
                var description = binding.Description;
                items.Add(new MenuItem(
                    () => $"{displayName}: {GetShortcutKeyText(actionId, binding.Key)}",
                    MenuAction.None,
                    onActivate: () => _mapping.BeginShortcutMapping(_activeShortcutGroupId, actionId, displayName),
                    hint: description));
            }

            if (items.Count == 0)
                return false;

            items.Add(BackItem());
            _menu.UpdateItems(ShortcutBindingsMenuId, items, preserveSelection: true);
            return true;
        }

        private string GetShortcutKeyText(string actionId, InputKey fallbackKey)
        {
            if (_menu.TryGetShortcutBinding(actionId, out var binding))
                return FormatShortcutKey(binding.Key);
            return FormatShortcutKey(fallbackKey);
        }

        private static string FormatShortcutKey(InputKey key)
        {
            return (int)key <= 0
                ? "none"
                : key.ToString();
        }
    }
}

