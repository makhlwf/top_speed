using System;
using System.Diagnostics;
using TopSpeed.Menu;

using TopSpeed.Localization;
namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void StartCalibrationSequence(string? returnMenuId = null)
        {
            _calibrationReturnMenuId = returnMenuId;
            _calibrationStopwatch = null;
            EnsureCalibrationMenus();
            _calibrationOverlay = !string.IsNullOrWhiteSpace(returnMenuId) && _menu.HasActiveMenu;
            if (_calibrationOverlay)
                _menu.Push(CalibrationIntroMenuId);
            else
                _menu.ShowRoot(CalibrationIntroMenuId);
            _state = AppState.Calibration;
        }

        private void EnsureCalibrationMenus()
        {
            if (_calibrationMenusRegistered)
                return;

            var introItems = new[]
            {
                new MenuItem(LocalizationService.Mark("Ok"), MenuAction.None, onActivate: BeginCalibrationSample)
            };
            var sampleItems = new[]
            {
                new MenuItem(LocalizationService.Mark("Ok"), MenuAction.None, onActivate: CompleteCalibration)
            };

            _menu.Register(_menu.CreateMenu(CalibrationIntroMenuId, introItems, CalibrationInstructions));
            _menu.Register(_menu.CreateMenu(CalibrationSampleMenuId, sampleItems, CalibrationSampleText));
            _calibrationMenusRegistered = true;
        }

        private void BeginCalibrationSample()
        {
            _calibrationStopwatch = Stopwatch.StartNew();
            if (_calibrationOverlay)
                _menu.ReplaceTop(CalibrationSampleMenuId);
            else
                _menu.ShowRoot(CalibrationSampleMenuId);
        }

        private void CompleteCalibration()
        {
            if (_calibrationStopwatch == null)
                return;

            var elapsedMs = _calibrationStopwatch.ElapsedMilliseconds;
            var words = CalibrationSampleText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var rate = words > 0 ? (float)elapsedMs / words : 0f;
            _settings.ScreenReaderRateMs = rate;
            _speech.ScreenReaderRateMs = rate;
            SaveSettings();

            _needsCalibration = false;
            var returnMenu = _calibrationReturnMenuId ?? "main";
            _calibrationReturnMenuId = null;
            if (_calibrationOverlay && _menu.CanPop)
                _menu.PopToPrevious();
            else
                _menu.ShowRoot(returnMenu);

            _calibrationOverlay = false;
            _menu.FadeInMenuMusic(force: true);
            _state = AppState.Menu;
            if (_autoUpdateAfterCalibration)
            {
                _autoUpdateAfterCalibration = false;
                StartAutoUpdateCheck();
            }
        }

        private static bool IsCalibrationMenu(string? id)
        {
            return id == CalibrationIntroMenuId || id == CalibrationSampleMenuId;
        }
    }
}




