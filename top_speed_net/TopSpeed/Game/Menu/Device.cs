using System;
using System.Collections.Generic;
using TopSpeed.Localization;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void TryShowDeviceChoiceDialog()
        {
            if (!IsMenuState(_state))
                return;
            if (_textInputPromptActive || _inputMapping.IsActive || _shortcutMapping.IsActive)
                return;
            if (_choices.HasActiveChoiceDialog)
                return;
            if (_multiplayerCoordinator.Questions.HasActiveOverlayQuestion || _dialogs.HasActiveOverlayDialog)
                return;
            if (!_input.TryGetPendingControllerChoices(out var discovered) || discovered.Count == 0)
                return;

            var items = new Dictionary<int, string>();
            var guidByChoiceId = new Dictionary<int, Guid>();

            for (var i = 0; i < discovered.Count; i++)
            {
                var choiceId = i + 1;
                var choice = discovered[i];
                var label = choice.IsRacingWheel
                    ? $"{choice.DisplayName} ({LocalizationService.Translate(LocalizationService.Mark("Racing wheel"))})"
                    : choice.DisplayName;
                items[choiceId] = label;
                guidByChoiceId[choiceId] = choice.InstanceGuid;
            }

            ShowChoiceDialog(
                LocalizationService.Mark("Choose controller"),
                LocalizationService.Mark("Multiple game controllers were detected. Select one controller to use."),
                items,
                cancelable: false,
                cancelLabel: null,
                onResult: result =>
                {
                    if (result.IsCanceled)
                        return;

                    if (!guidByChoiceId.TryGetValue(result.ChoiceId, out var instanceGuid))
                        return;

                    if (_input.TrySelectController(instanceGuid))
                    {
                        if (items.TryGetValue(result.ChoiceId, out var label))
                            _speech.Speak(LocalizationService.Format(LocalizationService.Mark("Controller selected. {0}."), label));
                        else
                            _speech.Speak(LocalizationService.Mark("Controller selected."));
                        return;
                    }

                    _speech.Speak(LocalizationService.Mark("Unable to activate the selected controller."));
                });
        }
    }
}

