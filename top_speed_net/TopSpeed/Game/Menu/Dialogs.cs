using System;
using System.Collections.Generic;
using TopSpeed.Menu;

using TopSpeed.Localization;
namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
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
                new DialogButton(QuestionId.Ok, LocalizationService.Mark("OK")));
            _dialogs.Show(dialog);
        }

        private void ShowChoiceDialog(
            string title,
            string? caption,
            IReadOnlyDictionary<int, string> items,
            bool cancelable,
            string? cancelLabel,
            Action<ChoiceDialogResult>? onResult)
        {
            var flags = cancelable ? ChoiceDialogFlags.Cancelable : ChoiceDialogFlags.None;
            var dialog = new ChoiceDialog(title, caption, items, onResult, flags, cancelLabel);
            _choices.Show(dialog);
        }
    }
}




