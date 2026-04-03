using System;
using System.Collections.Generic;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Menu;
using TopSpeed.Localization;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        void IMenuAudioActions.SaveMusicVolume(float volume) => SaveMusicVolume(volume);
        void IMenuAudioActions.ApplyAudioSettings() => ApplyAudioSettings();

        void IMenuRaceActions.QueueRaceStart(RaceMode mode) => QueueRaceStart(mode);

        void IMenuServerActions.StartServerDiscovery() => _multiplayerCoordinator.StartServerDiscovery();
        void IMenuServerActions.OpenSavedServersManager() => _multiplayerCoordinator.OpenSavedServersManager();
        void IMenuServerActions.BeginManualServerEntry() => _multiplayerCoordinator.BeginManualServerEntry();
        void IMenuServerActions.BeginServerPortEntry() => _multiplayerCoordinator.BeginServerPortEntry();
        void IMenuServerActions.NextChatCategory() => _multiplayerCoordinator.NextChatCategory();
        void IMenuServerActions.PreviousChatCategory() => _multiplayerCoordinator.PreviousChatCategory();

        void IMenuUiActions.SpeakMessage(string text) => _speech.Speak(text);
        void IMenuUiActions.ShowMessageDialog(string title, string caption, IReadOnlyList<string> items) => ShowMessageDialog(title, caption, items);
        void IMenuUiActions.ShowChoiceDialog(string title, string? caption, IReadOnlyDictionary<int, string> items, bool cancelable, string? cancelLabel, Action<ChoiceDialogResult>? onResult)
            => ShowChoiceDialog(title, caption, items, cancelable, cancelLabel, onResult);
        void IMenuUiActions.SpeakNotImplemented() => _speech.Speak(LocalizationService.Mark("Not implemented yet."));

        string IMenuSettingsActions.GetLanguageName() => CurrentLanguageName();
        void IMenuSettingsActions.ChangeLanguage() => ChangeLanguage();
        void IMenuSettingsActions.RestoreDefaults() => RestoreDefaults();
        void IMenuSettingsActions.RecalibrateScreenReaderRate() => StartCalibrationSequence("options_game");
        void IMenuSettingsActions.CheckForUpdates() => StartManualUpdateCheck();
        void IMenuSettingsActions.SetDevice(InputDeviceMode mode) => SetDevice(mode);
        void IMenuSettingsActions.UpdateSetting(Action update) => UpdateSetting(update);

        void IMenuMappingActions.BeginMapping(InputMappingMode mode, InputAction action) => _inputMapping.BeginMapping(mode, action);
        void IMenuMappingActions.BeginShortcutMapping(string groupId, string actionId, string displayName) => _shortcutMapping.BeginMapping(groupId, actionId, displayName);
        string IMenuMappingActions.FormatMappingValue(InputAction action, InputMappingMode mode) => _inputMapping.FormatMappingValue(action, mode);
    }
}

