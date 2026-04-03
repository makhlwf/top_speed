using System;
using System.Diagnostics;
using System.IO;
using TopSpeed.Localization;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void LaunchUpdaterAndExit()
        {
            var root = Directory.GetCurrentDirectory();
            var updaterPath = Path.Combine(root, _updateConfig.UpdaterExeName);
            if (!File.Exists(updaterPath))
            {
                ShowMessageDialog(
                    LocalizationService.Mark("Updater not found"),
                    LocalizationService.Mark("The update could not be installed automatically."),
                    new[]
                    {
                        LocalizationService.Format(
                            LocalizationService.Mark("Missing file: {0}"),
                            _updateConfig.UpdaterExeName)
                    });
                return;
            }

            if (string.IsNullOrWhiteSpace(_updateZipPath) || !File.Exists(_updateZipPath))
            {
                ShowMessageDialog(
                    LocalizationService.Mark("Update package missing"),
                    LocalizationService.Mark("The update package file was not found."),
                    new[] { LocalizationService.Mark("You can download the update again or install manually.") });
                return;
            }

            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var args =
                    $"--pid {currentProcess.Id} --zip \"{_updateZipPath}\" --dir \"{root}\" --game \"{_updateConfig.GameExeName}\" --skip \"{_updateConfig.UpdaterExeName}\"";
                var startInfo = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = args,
                    WorkingDirectory = root,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(startInfo);
                ExitRequested?.Invoke();
            }
            catch (Exception ex)
            {
                ShowMessageDialog(
                    LocalizationService.Mark("Updater launch failed"),
                    LocalizationService.Mark("The updater could not be started."),
                    new[] { ex.Message });
            }
        }
    }
}

