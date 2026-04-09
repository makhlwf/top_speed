using System;
using System.Diagnostics;
using System.IO;
using TopSpeed.Localization;
using TopSpeed.Runtime;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void LaunchUpdaterAndExit()
        {
            var root = AppContext.BaseDirectory;
            var updaterDir = TrimTrailingDirectorySeparator(root);
            var updaterPath = ResolveExecutablePath(root, _updateConfig.UpdaterEntryName);
            if (!File.Exists(updaterPath))
            {
                var expectedUpdaterFileName = RuntimeAssetResolver.ResolveExecutableFileName(_updateConfig.UpdaterEntryName);
                ShowMessageDialog(
                    LocalizationService.Mark("Updater not found"),
                    LocalizationService.Mark("The update could not be installed automatically."),
                    new[]
                    {
                        LocalizationService.Format(
                            LocalizationService.Mark("Missing file: {0}"),
                            expectedUpdaterFileName)
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
                    $"--pid {currentProcess.Id} --zip \"{_updateZipPath}\" --dir \"{updaterDir}\" --game \"{_updateConfig.GameEntryName}\" --skip \"{_updateConfig.UpdaterEntryName}\"";
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

        private static string ResolveExecutablePath(string root, string executableStem)
        {
            var fileName = RuntimeAssetResolver.ResolveExecutableFileName(executableStem);
            var directPath = Path.Combine(root, fileName);
            if (File.Exists(directPath))
                return directPath;

            var matches = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);
            if (matches.Length == 0)
                return directPath;
            if (matches.Length == 1)
                return matches[0];

            Array.Sort(matches, StringComparer.OrdinalIgnoreCase);
            return matches[0];
        }

        private static string TrimTrailingDirectorySeparator(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            var fullPath = Path.GetFullPath(path);
            var root = Path.GetPathRoot(fullPath) ?? string.Empty;
            if (string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
                return fullPath;

            return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}

