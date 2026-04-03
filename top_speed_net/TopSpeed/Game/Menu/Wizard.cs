using TopSpeed.Menu;
using TopSpeed.Localization;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void StartSetupWizard()
        {
            var introDialog = new Dialog(
                LocalizationService.Mark("Setup wizard."),
                LocalizationService.Mark("Welcome to Top Speed setup. On the next screens, you will choose your language and then calibrate the screen-reader speaking rate."),
                QuestionId.Ok,
                null,
                _ => ShowLanguageDialog(cancelable: false, onSelected: () => StartCalibrationSequence()),
                new DialogButton(QuestionId.Ok, LocalizationService.Mark("Continue"), flags: DialogButtonFlags.Default));
            introDialog.IsCancelable = false;
            _dialogs.Show(introDialog);
        }
    }
}

