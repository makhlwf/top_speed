using System;
using System.IO;
using TopSpeed.Localization;

namespace TopSpeed.Drive.Panels
{
    internal sealed partial class RadioVehiclePanel
    {
        private void OpenRadioMedia()
        {
            if (_pickerInProgress)
                return;

            _pickerInProgress = true;
            _fileDialogs.PickAudioFile(selectedPath =>
            {
                lock (_pendingPathLock)
                    _pendingSelectedPath = selectedPath;

                _pickerInProgress = false;
            });
        }

        private void OpenRadioFolder()
        {
            if (_folderPickerInProgress)
                return;

            _folderPickerInProgress = true;
            _fileDialogs.PickFolder(_playlistFolder, selectedFolder =>
            {
                lock (_pendingPathLock)
                    _pendingSelectedFolder = selectedFolder;

                _folderPickerInProgress = false;
            });
        }

        private void ProcessPendingSelection()
        {
            string? selectedPath;
            lock (_pendingPathLock)
            {
                selectedPath = _pendingSelectedPath;
                _pendingSelectedPath = null;
            }

            if (string.IsNullOrWhiteSpace(selectedPath))
                return;

            var fullPath = Path.GetFullPath(selectedPath);
            if (!File.Exists(fullPath))
            {
                _announce(LocalizationService.Translate(LocalizationService.Mark("The selected media file does not exist.")));
                return;
            }

            _playlist.Clear();
            _playlist.Add(fullPath);
            _playlistIndex = 0;
            _playlistFolder = string.Empty;
            ApplyLoopMode();

            LoadPlaylistEntry(_playlistIndex, preservePlaybackState: true, announceLoaded: true);
        }

        private void ProcessPendingFolderSelection()
        {
            string? selectedFolder;
            lock (_pendingPathLock)
            {
                selectedFolder = _pendingSelectedFolder;
                _pendingSelectedFolder = null;
            }

            if (string.IsNullOrWhiteSpace(selectedFolder))
                return;

            var folderPath = selectedFolder!;
            if (!BuildPlaylistFromFolder(folderPath, preserveCurrentMedia: false, announceErrors: true))
                return;

            if (!LoadPlaylistEntry(_playlistIndex, preservePlaybackState: true, announceLoaded: true))
                return;

            _announce(_shuffleMode
                ? LocalizationService.Translate(LocalizationService.Mark("Shuffle mode on."))
                : LocalizationService.Translate(LocalizationService.Mark("Shuffle mode off.")));
        }

        private void HandlePlaybackEndAdvance()
        {
            var isPlaying = _radio.HasMedia && _radio.IsPlaying;
            if (_radio.HasMedia && _radio.DesiredPlaying && !_radio.IsPaused && _lastObservedPlaying && !isPlaying)
            {
                if (_playlist.Count > 1 && !_radio.LoopPlayback)
                {
                    if (StepPlaylistIndex(1))
                        LoadPlaylistEntry(_playlistIndex, preservePlaybackState: true, announceLoaded: true);
                }
            }

            _lastObservedPlaying = isPlaying;
        }
    }
}


