using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Core.Updates;
using TopSpeed.Localization;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void BeginUpdateDownload(UpdateInfo update)
        {
            if (update == null)
                return;
            if (_updateDownloadTask != null)
                return;

            _updateTotalBytes = update.AssetSizeBytes;
            _updateDownloadedBytes = 0;
            _updatePercent = 0;
            _updateTonePercent = 0;
            _lastSpokenUpdatePercent = 0;
            _updateProgressOpen = true;
            _updateCompleteOpen = false;
            _updateZipPath = string.Empty;

            ShowUpdateProgressDialog();

            _updateDownloadCts?.Cancel();
            _updateDownloadCts?.Dispose();
            _updateDownloadCts = new CancellationTokenSource();
            _updateDownloadTask = Task.Run(() =>
                _updateService.DownloadAsync(
                    update,
                    Directory.GetCurrentDirectory(),
                    OnUpdateProgress,
                    _updateDownloadCts.Token));
        }

        private void OnUpdateProgress(DownloadProgress progress)
        {
            if (progress == null)
                return;

            Interlocked.Exchange(ref _updateDownloadedBytes, Math.Max(0, progress.DownloadedBytes));
            if (progress.TotalBytes > 0)
                Interlocked.Exchange(ref _updateTotalBytes, progress.TotalBytes);

            var percent = progress.Percent;
            if (percent < 0)
                percent = 0;
            if (percent > 100)
                percent = 100;
            Interlocked.Exchange(ref _updatePercent, percent);
        }

        private void HandleUpdateDownload()
        {
            if (_updateDownloadTask == null)
                return;

            HandleUpdateProgressEffects();
            if (_updateProgressOpen)
                ShowUpdateProgressDialog();

            if (!_updateDownloadTask.IsCompleted)
                return;

            DownloadResult result;
            if (_updateDownloadTask.IsFaulted || _updateDownloadTask.IsCanceled)
            {
                result = new DownloadResult
                {
                    IsSuccess = false,
                    ErrorMessage = LocalizationService.Mark("Update download failed.")
                };
            }
            else
            {
                result = _updateDownloadTask.GetAwaiter().GetResult();
            }

            _updateDownloadTask = null;
            _updateDownloadCts?.Dispose();
            _updateDownloadCts = null;
            _updateProgressOpen = false;
            _dialogs.CloseActive();

            if (!result.IsSuccess)
            {
                ShowMessageDialog(
                    LocalizationService.Mark("Download failed"),
                    LocalizationService.Mark("The update package could not be downloaded."),
                    new[] { result.ErrorMessage });
                return;
            }

            _updateZipPath = result.ZipPath;
            ShowUpdateCompleteDialog();
        }

        private void HandleUpdateProgressEffects()
        {
            var target = Volatile.Read(ref _updatePercent);
            if (target < 0)
                target = 0;
            if (target > 100)
                target = 100;

            while (_updateTonePercent < target)
            {
                _updateTonePercent++;
                var frequency = 110d * Math.Pow(2d, _updateTonePercent / 25d);
                _audio.PlayTriangleTone(frequency, 40);
                if (_updateTonePercent % 10 == 0 && _updateTonePercent != _lastSpokenUpdatePercent)
                {
                    _lastSpokenUpdatePercent = _updateTonePercent;
                    _speech.Speak(FormatProgressPercent(_updateTonePercent));
                }
            }
        }

        private static string FormatProgressPercent(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture) + CultureInfo.InvariantCulture.NumberFormat.PercentSymbol;
        }
    }
}

