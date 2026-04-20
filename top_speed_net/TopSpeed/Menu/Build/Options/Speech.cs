using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Speech;
using TopSpeed.Speech.Prism;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsSpeechSettingsMenu()
        {
            return BackMenu("options_speech", BuildSpeechItems());
        }

        internal void RefreshSpeechSettingsMenu()
        {
            _menu.UpdateItems("options_speech", BuildSpeechItems(), preserveSelection: true);
        }

        private MenuItem BuildSpeechBackendItem()
        {
            var backends = GetSelectableSpeechBackends();
            var hint = HintForPlatform(
                LocalizationService.Mark("Choose which speech backend to prefer. Automatic lets Prism pick the best available backend. Use LEFT or RIGHT to change."),
                LocalizationService.Mark("Choose which speech backend to prefer. Automatic lets Prism pick the best available backend. Swipe left or right with two fingers to change."));

            if (backends.Count == 0)
            {
                return new MenuItem(
                    () => LocalizationService.Format(
                        LocalizationService.Mark("Speech backend: {0}"),
                        LocalizationService.Translate(LocalizationService.Mark("automatic"))),
                    MenuAction.None,
                    hint: hint);
            }

            var values = new List<string>(backends.Count + 1)
            {
                LocalizationService.Mark("automatic")
            };

            for (var i = 0; i < backends.Count; i++)
                values.Add(backends[i].Name);

            return new RadioButton(
                LocalizationService.Mark("Speech backend"),
                values,
                () => GetSpeechBackendIndex(backends),
                value => _settingsActions.SetSpeechBackend(value == 0 ? null : backends[value - 1].Id),
                hint: hint);
        }

        private List<MenuItem> BuildSpeechItems()
        {
            var items = new List<MenuItem>
            {
                BuildSpeechBackendItem(),
                new CheckBox(
                    LocalizationService.Mark("Interrupt screen reader speech"),
                    () => _settings.ScreenReaderInterrupt,
                    value => _settingsActions.SetScreenReaderInterrupt(value),
                    hint: HintForPlatform(
                        LocalizationService.Mark("When checked, new spoken messages interrupt the current screen reader speech. Menu titles and usage hints are not affected. Press ENTER to toggle."),
                        LocalizationService.Mark("When checked, new spoken messages interrupt the current screen reader speech. Menu titles and usage hints are not affected. Swipe up to toggle."))),
                BuildSpeechModeItem()
            };

            if (SupportsVoiceSelection())
                items.Add(BuildSpeechVoiceItem());

            if (SupportsRateSelection())
                items.Add(BuildSpeechRateItem());

            items.Add(new MenuItem(
                LocalizationService.Mark("Recalibrate screen reader rate"),
                MenuAction.None,
                onActivate: _settingsActions.RecalibrateScreenReaderRate,
                hint: HintForPlatform(
                    LocalizationService.Mark("Measure your screen-reader speaking speed again so usage hints and delayed speech timings stay accurate. Press ENTER to start."),
                    LocalizationService.Mark("Measure your screen-reader speaking speed again so usage hints and delayed speech timings stay accurate. Swipe up to start."))));
            return items;
        }

        private List<SpeechBackendInfo> GetSelectableSpeechBackends()
        {
            var source = _settingsActions.GetSpeechBackends();
            var result = new List<SpeechBackendInfo>();
            for (var i = 0; i < source.Count; i++)
            {
                var backend = source[i];
                if (!backend.IsSupported || string.IsNullOrWhiteSpace(backend.Name) || backend.Id == Ids.Uia)
                    continue;

                result.Add(backend);
            }

            return result;
        }

        private MenuItem BuildSpeechModeItem()
        {
            return new RadioButton(
                LocalizationService.Mark("Speech mode"),
                new[]
                {
                    LocalizationService.Mark("speech only"),
                    LocalizationService.Mark("braille"),
                    LocalizationService.Mark("speech with braille")
                },
                () => (int)_settings.SpeechMode,
                value => _settingsActions.SetSpeechMode((SpeechOutputMode)value),
                hint: HintForPlatform(
                    LocalizationService.Mark("Choose whether spoken messages use speech only, braille only, or speech with braille. Use LEFT or RIGHT to change."),
                    LocalizationService.Mark("Choose whether spoken messages use speech only, braille only, or speech with braille. Swipe left or right with two fingers to change.")));
        }

        private MenuItem BuildSpeechVoiceItem()
        {
            var voices = _settingsActions.GetSpeechVoices();
            if (voices.Count <= 1)
            {
                var voiceName = voices.Count == 1 ? FormatVoiceLabel(voices[0]) : LocalizationService.Translate(LocalizationService.Mark("automatic"));
                return new MenuItem(
                    () => LocalizationService.Format(
                        LocalizationService.Mark("Voice: {0}"),
                        voiceName),
                    MenuAction.None,
                    hint: HintForPlatform(
                        LocalizationService.Mark("Select which voice the current speech backend should use. Use LEFT or RIGHT to change."),
                        LocalizationService.Mark("Select which voice the current speech backend should use. Swipe left or right with two fingers to change.")));
            }

            var values = new List<string>(voices.Count);
            for (var i = 0; i < voices.Count; i++)
                values.Add(FormatVoiceLabel(voices[i]));

            return new RadioButton(
                LocalizationService.Mark("Voice"),
                values,
                () => GetSpeechVoiceIndex(voices),
                value => _settingsActions.SetSpeechVoice(voices[value].Index),
                hint: HintForPlatform(
                    LocalizationService.Mark("Select which voice the current speech backend should use. Use LEFT or RIGHT to change."),
                    LocalizationService.Mark("Select which voice the current speech backend should use. Swipe left or right with two fingers to change.")));
        }

        private MenuItem BuildSpeechRateItem()
        {
            return new Slider(
                LocalizationService.Mark("Speech rate"),
                "0-100",
                () => (int)System.Math.Round(_settings.SpeechRate * 100f),
                value => _settingsActions.SetSpeechRate(value / 100f),
                hint: HintForPlatform(
                    LocalizationService.Mark("Adjust the speech rate for the current backend. Use LEFT or RIGHT to change by 1, PAGE UP or PAGE DOWN to change by 10, HOME for maximum, END for minimum."),
                    LocalizationService.Mark("Adjust the speech rate for the current backend. Swipe up or down with two fingers to change by 10, swipe left or right with two fingers to change by 1, and swipe up or down with three fingers for maximum or minimum.")));
        }

        private bool SupportsVoiceSelection()
        {
            var capabilities = _settingsActions.GetSpeechCapabilities();
            if ((capabilities & SpeechCapabilities.SetVoice) != SpeechCapabilities.SetVoice)
                return false;

            return _settingsActions.GetSpeechVoices().Count > 0;
        }

        private bool SupportsRateSelection()
        {
            var capabilities = _settingsActions.GetSpeechCapabilities();
            return (capabilities & SpeechCapabilities.SetRate) == SpeechCapabilities.SetRate;
        }

        private int GetSpeechVoiceIndex(IReadOnlyList<SpeechVoiceInfo> voices)
        {
            if (!_settings.SpeechVoiceIndex.HasValue)
                return 0;

            for (var i = 0; i < voices.Count; i++)
            {
                if (voices[i].Index == _settings.SpeechVoiceIndex.Value)
                    return i;
            }

            return 0;
        }

        private static string FormatVoiceLabel(SpeechVoiceInfo voice)
        {
            if (string.IsNullOrWhiteSpace(voice.Language))
                return voice.Name;

            return LocalizationService.Format(
                LocalizationService.Mark("{0} ({1})"),
                voice.Name,
                voice.Language);
        }

        private int GetSpeechBackendIndex(IReadOnlyList<SpeechBackendInfo> backends)
        {
            if (!_settings.SpeechBackendId.HasValue)
                return 0;

            for (var i = 0; i < backends.Count; i++)
            {
                if (backends[i].Id == _settings.SpeechBackendId.Value)
                    return i + 1;
            }

            return 0;
        }
    }
}
