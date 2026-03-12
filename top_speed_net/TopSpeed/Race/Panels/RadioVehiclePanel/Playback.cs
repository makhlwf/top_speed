using System;
using System.IO;

namespace TopSpeed.Race.Panels
{
    internal sealed partial class RadioVehiclePanel
    {
        private void TogglePlayback()
        {
            if (!_radio.HasMedia)
            {
                if (_playlist.Count == 0)
                {
                    _announce("No radio media loaded.");
                    return;
                }

                SelectIndexFromCurrentMedia();
                if (_playlistIndex < 0)
                    _playlistIndex = 0;
                if (!LoadPlaylistEntry(_playlistIndex, preservePlaybackState: false, announceLoaded: true))
                    return;
                _radio.SetPlayback(true);
                _announce("Radio playing.");
                _playbackChanged?.Invoke(_radio.HasMedia, _radio.DesiredPlaying, _radio.MediaId);
                return;
            }

            _radio.TogglePlayback();
            _announce(_radio.DesiredPlaying ? "Radio playing." : "Radio paused.");
            _playbackChanged?.Invoke(_radio.HasMedia, _radio.DesiredPlaying, _radio.MediaId);
        }

        private void CycleTrack(int delta)
        {
            if (_playlist.Count == 0)
            {
                _announce("No folder playlist loaded.");
                return;
            }

            SelectIndexFromCurrentMedia();
            if (!StepPlaylistIndex(delta))
                return;

            LoadPlaylistEntry(_playlistIndex, preservePlaybackState: true, announceLoaded: true);
        }

        private void ToggleShuffle()
        {
            _shuffleMode = !_shuffleMode;
            _settings.RadioShuffle = _shuffleMode;
            SaveRadioSettings();

            var lastFolder = _settings.RadioLastFolder ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(lastFolder))
                BuildPlaylistFromFolder(lastFolder, preserveCurrentMedia: true, announceErrors: false);

            _announce($"Shuffle mode {(_shuffleMode ? "on" : "off")}.");
        }

        private void ToggleLoop()
        {
            _loopMode = !_loopMode;
            ApplyLoopMode();
            _announce($"Loop mode {(_loopMode ? "on" : "off")}.");
        }

        private bool LoadPlaylistEntry(int index, bool preservePlaybackState, bool announceLoaded, bool announceNameOnly = false)
        {
            if (index < 0 || index >= _playlist.Count)
                return false;

            var mediaPath = _playlist[index];
            _playlistIndex = index;
            ApplyLoopMode();

            var mediaId = _nextMediaId();
            if (!_radio.TryLoadFromFile(mediaPath, mediaId, preservePlaybackState, out var error))
            {
                _announce($"Failed to load radio media. {error}");
                return false;
            }

            if (announceLoaded)
            {
                var fileName = Path.GetFileName(mediaPath);
                if (announceNameOnly)
                    _announce(fileName);
                else
                    _announce($"Radio loaded {fileName}.");
            }

            _mediaLoaded?.Invoke(mediaId, mediaPath);
            _playbackChanged?.Invoke(_radio.HasMedia, _radio.DesiredPlaying, _radio.MediaId);
            _lastObservedPlaying = _radio.HasMedia && _radio.IsPlaying;
            return true;
        }

        private void SelectIndexFromCurrentMedia()
        {
            if (_playlist.Count == 0)
            {
                _playlistIndex = -1;
                return;
            }

            var currentPath = _radio.MediaPath;
            if (!string.IsNullOrWhiteSpace(currentPath))
            {
                var idx = _playlist.FindIndex(path => string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    _playlistIndex = idx;
                    return;
                }
            }

            if (_playlistIndex < 0 || _playlistIndex >= _playlist.Count)
                _playlistIndex = 0;
        }

        private bool StepPlaylistIndex(int delta)
        {
            if (_playlist.Count == 0)
                return false;

            if (_playlistIndex < 0 || _playlistIndex >= _playlist.Count)
                _playlistIndex = 0;
            else
                _playlistIndex += delta;

            while (_playlistIndex < 0)
                _playlistIndex += _playlist.Count;
            while (_playlistIndex >= _playlist.Count)
                _playlistIndex -= _playlist.Count;

            return true;
        }

        private void ApplyLoopMode()
        {
            _radio.SetLoopPlayback(_loopMode || _playlist.Count <= 1);
        }
    }
}
