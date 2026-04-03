using System;
using TopSpeed.Input;
using TopSpeed.Localization;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void SaveSettings()
        {
            _settingsManager.Save(_settings);
        }

        private void RestoreDefaults()
        {
            _settings.RestoreDefaults();
            ApplyLanguage(_settings.Language, saveSettings: false, announceChange: false);
            _raceInput.SetDevice(_settings.DeviceMode);
            _input.SetDeviceMode(_settings.DeviceMode);
            _speech.ScreenReaderRateMs = _settings.ScreenReaderRateMs;
            _needsCalibration = _settings.UsageHints && _settings.ScreenReaderRateMs <= 0f;
            _menu.SetWrapNavigation(_settings.MenuWrapNavigation);
            _menu.SetMenuSoundPreset(_settings.MenuSoundPreset);
            _menu.SetMenuNavigatePanning(_settings.MenuNavigatePanning);
            _menu.SetMenuAutoFocus(_settings.MenuAutoFocus);
            _menu.ResetShortcutBindings();
            ApplyAudioSettings();
            SaveSettings();
            _speech.Speak(LocalizationService.Mark("Defaults restored."));
        }

        private void SetDevice(InputDeviceMode mode)
        {
            _settings.DeviceMode = mode;
            _raceInput.SetDevice(mode);
            _input.SetDeviceMode(mode);
            SaveSettings();
        }

        private void UpdateSetting(Action update)
        {
            update();
            SaveSettings();
        }
    }
}

