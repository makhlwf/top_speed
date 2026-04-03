using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopSpeed.Localization;

namespace TopSpeed.Race.Panels
{
    internal sealed partial class RadioVehiclePanel
    {
        private bool BuildPlaylistFromFolder(string folderPath, bool preserveCurrentMedia, bool announceErrors)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                if (announceErrors)
                    _announce(LocalizationService.Mark("No folder was selected."));
                return false;
            }

            string fullFolder;
            try
            {
                fullFolder = Path.GetFullPath(folderPath);
            }
            catch
            {
                if (announceErrors)
                    _announce(LocalizationService.Mark("The selected folder path is invalid."));
                return false;
            }

            if (!Directory.Exists(fullFolder))
            {
                if (announceErrors)
                    _announce(LocalizationService.Mark("The selected folder does not exist."));
                return false;
            }

            List<string> files;
            try
            {
                files = Directory
                    .EnumerateFiles(fullFolder, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(IsSupportedAudioFile)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                if (announceErrors)
                    _announce(LocalizationService.Mark("Could not read files from the selected folder."));
                return false;
            }

            if (files.Count == 0)
            {
                if (announceErrors)
                    _announce(LocalizationService.Mark("No supported audio files were found in the selected folder."));
                return false;
            }

            if (_shuffleMode)
                Shuffle(files);

            var currentPath = preserveCurrentMedia ? _radio.MediaPath : null;

            _playlist.Clear();
            _playlist.AddRange(files);
            _playlistFolder = fullFolder;
            _playlistIndex = 0;
            if (!string.IsNullOrWhiteSpace(currentPath))
            {
                var idx = _playlist.FindIndex(path => string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                    _playlistIndex = idx;
            }

            _settings.RadioLastFolder = _playlistFolder;
            _settings.RadioShuffle = _shuffleMode;
            SaveRadioSettings();
            ApplyLoopMode();
            return true;
        }

        private static bool IsSupportedAudioFile(string path)
        {
            var extension = Path.GetExtension(path);
            if (string.IsNullOrWhiteSpace(extension))
                return false;

            for (var i = 0; i < SupportedExtensions.Length; i++)
            {
                if (string.Equals(extension, SupportedExtensions[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void Shuffle(List<string> files)
        {
            for (var i = files.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                var tmp = files[i];
                files[i] = files[j];
                files[j] = tmp;
            }
        }

        private void TryRestoreFolderPlaylist()
        {
            if (string.IsNullOrWhiteSpace(_settings.RadioLastFolder))
                return;

            BuildPlaylistFromFolder(_settings.RadioLastFolder, preserveCurrentMedia: false, announceErrors: false);
        }
    }
}

