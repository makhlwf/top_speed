using System;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Speech;

namespace TopSpeed.Shortcuts
{
    internal sealed class ShortcutMappingHandler
    {
        private readonly IInputService _input;
        private readonly MenuManager _menu;
        private readonly RaceSettings _settings;
        private readonly SpeechService _speech;
        private readonly Action _saveSettings;

        private bool _isActive;
        private bool _needsInstruction;
        private string _groupId = string.Empty;
        private string _actionId = string.Empty;
        private string _displayName = string.Empty;

        public ShortcutMappingHandler(
            IInputService input,
            MenuManager menu,
            RaceSettings settings,
            SpeechService speech,
            Action saveSettings)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _speech = speech ?? throw new ArgumentNullException(nameof(speech));
            _saveSettings = saveSettings ?? throw new ArgumentNullException(nameof(saveSettings));
        }

        public bool IsActive => _isActive;

        public void BeginMapping(string groupId, string actionId, string displayName)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return;
            if (string.IsNullOrWhiteSpace(actionId))
                return;

            _groupId = groupId.Trim();
            _actionId = actionId.Trim();
            _displayName = string.IsNullOrWhiteSpace(displayName) ? _actionId : displayName.Trim();
            _needsInstruction = true;
            _isActive = true;
        }

        public void Update()
        {
            if (!_isActive)
                return;

            if (_needsInstruction)
            {
                _needsInstruction = false;
                _speech.Speak(LocalizationService.Format(
                    LocalizationService.Mark("Press the new key for {0}."),
                    _displayName.ToLowerInvariant()));
            }

            if (_input.WasPressed(Key.Escape))
            {
                _isActive = false;
                _speech.Speak(LocalizationService.Mark("Shortcut mapping cancelled."));
                return;
            }

            for (var i = 1; i < 256; i++)
            {
                var key = (Key)i;
                if (!_input.WasPressed(key))
                    continue;

                if (_menu.IsShortcutKeyInUse(_groupId, key, _actionId))
                {
                    _speech.Speak(LocalizationService.Mark("That key is already in use in this shortcut group."));
                    return;
                }

                try
                {
                    _menu.SetShortcutBinding(_actionId, key);
                }
                catch (InvalidOperationException)
                {
                    _isActive = false;
                    _speech.Speak(LocalizationService.Mark("Unable to apply the new shortcut key."));
                    return;
                }
                catch (ArgumentException)
                {
                    _isActive = false;
                    _speech.Speak(LocalizationService.Mark("Unable to apply the new shortcut key."));
                    return;
                }

                _settings.ShortcutKeyBindings[_actionId] = key;
                _saveSettings();
                _isActive = false;
                _speech.Speak(LocalizationService.Format(
                    LocalizationService.Mark("{0} set to {1}."),
                    _displayName,
                    FormatKey(key)));
                return;
            }
        }

        private static string FormatKey(Key key)
        {
            return (int)key <= 0
                ? LocalizationService.Translate(LocalizationService.Mark("none"))
                : key.ToString();
        }
    }
}



