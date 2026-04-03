using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Menu;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private string CurrentLanguageName()
        {
            return ClientLanguages.ResolveSettingsLabel(_settings.Language, _clientLanguages);
        }

        private void ChangeLanguage()
        {
            ShowLanguageDialog(cancelable: true, onSelected: null);
        }

        private void ShowLanguageDialog(bool cancelable, Action? onSelected)
        {
            var items = new Dictionary<int, string>();
            var languagesByChoiceId = new Dictionary<int, ClientLanguage>();
            for (var i = 0; i < _clientLanguages.Count; i++)
            {
                var choiceId = i + 1;
                var language = _clientLanguages[i];
                items[choiceId] = language.ListLabel;
                languagesByChoiceId[choiceId] = language;
            }

            if (items.Count == 0)
            {
                _speech.Speak(LocalizationService.Mark("No languages are available."));
                return;
            }

            var flags = cancelable ? ChoiceDialogFlags.Cancelable : ChoiceDialogFlags.None;
            var dialog = new ChoiceDialog(
                LocalizationService.Mark("Choose the language."),
                null,
                items,
                result =>
                {
                    if (result.IsCanceled)
                        return;
                    if (!languagesByChoiceId.TryGetValue(result.ChoiceId, out var language))
                        return;

                    ApplyLanguage(language.Code, saveSettings: true, announceChange: true);
                    onSelected?.Invoke();
                },
                flags,
                LocalizationService.Mark("Cancel"));
            _choices.Show(dialog);
        }

        private void ApplyLanguage(string? languageCode, bool saveSettings, bool announceChange)
        {
            var resolvedCode = ClientLanguages.ResolveCode(languageCode, _clientLanguages);
            var changed = !string.Equals(_settings.Language, resolvedCode, StringComparison.OrdinalIgnoreCase);
            _settings.Language = resolvedCode;
            LocalizationBootstrap.Configure(_settings.Language, LocalizationBootstrap.ClientCatalogGroup);

            if (saveSettings)
                SaveSettings();

            if (!announceChange)
                return;

            var languageName = CurrentLanguageName();
            if (changed)
            {
                _speech.Speak(LocalizationService.Format(LocalizationService.Mark("Language set to {0}."), languageName));
                return;
            }

            _speech.Speak(LocalizationService.Format(LocalizationService.Mark("Language remains {0}."), languageName));
        }
    }
}

