using System;
using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Menu;
using TopSpeed.Core;

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

        void IMenuUiActions.SpeakMessage(string text) => _speech.Speak(text);
        void IMenuUiActions.ShowMessageDialog(string title, string caption, IReadOnlyList<string> items) => ShowMessageDialog(title, caption, items);
        void IMenuUiActions.SpeakNotImplemented() => _speech.Speak("Not implemented yet.");

        void IMenuSettingsActions.RestoreDefaults() => RestoreDefaults();
        void IMenuSettingsActions.RecalibrateScreenReaderRate() => StartCalibrationSequence("options_game");
        void IMenuSettingsActions.SetDevice(InputDeviceMode mode) => SetDevice(mode);
        void IMenuSettingsActions.UpdateSetting(Action update) => UpdateSetting(update);

        void IMenuMappingActions.BeginMapping(InputMappingMode mode, InputAction action) => _inputMapping.BeginMapping(mode, action);
        string IMenuMappingActions.FormatMappingValue(InputAction action, InputMappingMode mode) => _inputMapping.FormatMappingValue(action, mode);

        private void ShowMessageDialog(string title, string caption, IReadOnlyList<string> items)
        {
            var dialogItems = new List<DialogItem>();
            if (items != null)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    var line = items[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    dialogItems.Add(new DialogItem(line));
                }
            }

            var dialog = new Dialog(
                title ?? string.Empty,
                caption,
                QuestionId.Ok,
                dialogItems,
                onResult: null,
                new DialogButton(QuestionId.Ok, "OK"));
            _dialogs.Show(dialog);
        }
    }
}
