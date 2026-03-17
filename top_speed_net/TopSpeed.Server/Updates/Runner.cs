using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using TopSpeed.Server.Logging;

namespace TopSpeed.Server.Updates
{
    internal sealed class ServerUpdateRunner
    {
        private readonly ServerUpdateConfig _config;
        private readonly ServerUpdateService _service;
        private readonly Logger _logger;

        private int _lastProgressPercent = -1;
        private int _lastProgressLineLength;

        public ServerUpdateRunner(ServerUpdateConfig config, Logger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _service = new ServerUpdateService(_config);
        }

        public bool RunInteractiveCheck()
        {
            ConsoleSink.WriteLine("Checking for update...");
            var result = _service
                .CheckAsync(ServerUpdateConfig.CurrentVersion, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            if (!result.IsSuccess)
            {
                var message = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "Update check failed."
                    : result.ErrorMessage;
                _logger.Warning($"Server update check failed: {message}");
                ConsoleSink.WriteLine(message);
                return false;
            }

            if (result.Update == null)
            {
                ConsoleSink.WriteLine("Server is up-to-date.");
                return false;
            }

            var update = result.Update;
            var currentVersion = ServerUpdateConfig.CurrentVersion.ToMachineString();
            ConsoleSink.WriteLine(
                $"A new update is available for the server. Your current server version is {currentVersion}. Available version: {update.VersionText}.");
            ConsoleSink.WriteLine("Changes:");
            if (update.Changes.Count == 0)
            {
                ConsoleSink.WriteLine("No changes were listed for this update.");
            }
            else
            {
                for (var i = 0; i < update.Changes.Count; i++)
                {
                    var change = update.Changes[i];
                    if (string.IsNullOrWhiteSpace(change))
                        continue;
                    ConsoleSink.WriteLine(change.Trim());
                }
            }

            if (!TryPromptYesNo("Would you like to download the update? (y/n)", out var shouldDownload))
            {
                var message = "Standard input is not available. Update download was skipped.";
                _logger.Warning(message);
                ConsoleSink.WriteLine(message);
                return false;
            }

            if (!shouldDownload)
                return false;

            ConsoleSink.WriteLine("Downloading...");
            ResetProgress();
            var download = _service
                .DownloadAsync(update, AppContext.BaseDirectory, RenderProgress, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            CompleteProgressLine();

            if (!download.IsSuccess)
            {
                var message = string.IsNullOrWhiteSpace(download.ErrorMessage)
                    ? "Download failed."
                    : download.ErrorMessage;
                _logger.Warning($"Server update download failed: {message}");
                ConsoleSink.WriteLine(message);
                return false;
            }

            if (!StartUpdater(download.ZipPath))
                return false;

            return true;
        }

        private bool StartUpdater(string zipPath)
        {
            var root = AppContext.BaseDirectory;
            var updaterPath = Path.Combine(root, _config.UpdaterExeName);
            if (!File.Exists(updaterPath))
            {
                ConsoleSink.WriteLine($"Updater not found: {_config.UpdaterExeName}");
                return false;
            }

            try
            {
                var process = Process.GetCurrentProcess();
                var args =
                    $"--pid {process.Id} --zip \"{zipPath}\" --dir \"{root}\" --game \"{_config.ServerExeName}\" --skip \"{_config.UpdaterExeName}\"";
                var startInfo = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = args,
                    WorkingDirectory = root,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Could not launch updater: {ex.Message}");
                ConsoleSink.WriteLine($"Could not launch updater: {ex.Message}");
                return false;
            }
        }

        private static bool TryPromptYesNo(string prompt, out bool value)
        {
            value = false;
            while (true)
            {
                if (!ConsoleSink.WriteLine(prompt))
                    return false;

                string? line;
                try
                {
                    line = Console.ReadLine();
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
                catch (IOException)
                {
                    return false;
                }

                if (line == null)
                    return false;

                var text = line.Trim();
                if (text.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                    return true;
                }

                if (text.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
                    return true;
                }

                ConsoleSink.WriteLine("Invalid input. Enter y or n.");
            }
        }

        private void RenderProgress(ServerDownloadProgress progress)
        {
            var percent = Math.Clamp(progress.Percent, 0, 100);
            if (Console.IsOutputRedirected)
            {
                if (percent == _lastProgressPercent)
                    return;

                _lastProgressPercent = percent;
                ConsoleSink.WriteLine($"{percent}%");
                return;
            }

            var barWidth = 40;
            var filled = (percent * barWidth) / 100;
            var remaining = barWidth - filled;
            var bar = $"[{new string('#', filled)}{new string('-', remaining)}]";
            var downloadedText = FormatBytes(progress.DownloadedBytes);
            var totalText = progress.TotalBytes > 0
                ? FormatBytes(progress.TotalBytes)
                : "?";
            var line = $"{bar} {percent,3}% {downloadedText}/{totalText}";

            try
            {
                var padded = line.PadRight(_lastProgressLineLength);
                Console.Write('\r');
                Console.Write(padded);
                _lastProgressLineLength = Math.Max(_lastProgressLineLength, padded.Length);
            }
            catch (InvalidOperationException)
            {
                ConsoleSink.WriteLine($"{percent}%");
            }
            catch (IOException)
            {
                ConsoleSink.WriteLine($"{percent}%");
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 0)
                bytes = 0;

            const double kilobyte = 1024d;
            const double megabyte = 1024d * 1024d;
            const double gigabyte = 1024d * 1024d * 1024d;

            if (bytes >= gigabyte)
                return $"{bytes / gigabyte:0.00} GB";
            if (bytes >= megabyte)
                return $"{bytes / megabyte:0.00} MB";
            if (bytes >= kilobyte)
                return $"{bytes / kilobyte:0.00} KB";
            return $"{bytes} B";
        }

        private void ResetProgress()
        {
            _lastProgressPercent = -1;
            _lastProgressLineLength = 0;
        }

        private void CompleteProgressLine()
        {
            if (Console.IsOutputRedirected || _lastProgressLineLength <= 0)
                return;

            try
            {
                Console.WriteLine();
            }
            catch (InvalidOperationException)
            {
            }
            catch (IOException)
            {
            }
        }
    }
}
