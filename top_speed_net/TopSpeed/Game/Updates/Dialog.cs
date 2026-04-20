using System.Collections.Generic;
using System.Globalization;
using TopSpeed.Menu;

using TopSpeed.Localization;
namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void ShowUpdateProgressDialog()
        {
            var total = System.Threading.Volatile.Read(ref _updateTotalBytes);
            var downloaded = System.Threading.Volatile.Read(ref _updateDownloadedBytes);
            var percent = System.Threading.Volatile.Read(ref _updatePercent);
            var items = new List<DialogItem>
            {
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("File size: {0}"), FormatBytes(total))),
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("Downloaded size: {0}"), FormatBytes(downloaded))),
                new DialogItem(LocalizationService.Format(LocalizationService.Mark("Percentage: {0}%"), percent))
            };

            var dialog = new Dialog(LocalizationService.Mark("Downloading update..."),
                null,
                QuestionId.Close,
                items,
                onResult: _ => CancelUpdateDownload(),
                new DialogButton(QuestionId.Close, LocalizationService.Mark("Cancel")));
            _dialogs.Show(dialog);
        }

        private void ShowUpdateCompleteDialog()
        {
            if (_updateCompleteOpen)
                return;

            _updateCompleteOpen = true;
            var items = new List<DialogItem>
            {
                new DialogItem(LocalizationService.Mark("The update download is complete.")),
                new DialogItem(LocalizationService.Mark("Press OK to close the game and install the update."))
            };

            var dialog = new Dialog(LocalizationService.Mark("Update download complete."),
                null,
                QuestionId.Cancel,
                items,
                onResult: resultId =>
                {
                    _updateCompleteOpen = false;
                    if (resultId == QuestionId.Ok)
                        LaunchUpdaterAndExit();
                },
                new DialogButton(QuestionId.Ok, LocalizationService.Mark("OK")));
            _dialogs.Show(dialog);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0)
                return LocalizationService.Translate(LocalizationService.Mark("Unknown"));
            var units = new[]
            {
                LocalizationService.Mark("B"),
                LocalizationService.Mark("KB"),
                LocalizationService.Mark("MB"),
                LocalizationService.Mark("GB")
            };
            var index = 0;
            var value = (double)bytes;
            while (value >= 1024d && index < units.Length - 1)
            {
                value /= 1024d;
                index++;
            }

            return value.ToString("0.##", CultureInfo.InvariantCulture) + " " + LocalizationService.Translate(units[index]);
        }
    }
}





