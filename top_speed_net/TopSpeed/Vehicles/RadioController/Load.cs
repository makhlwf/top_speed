using System;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Vehicles
{
    internal sealed partial class VehicleRadioController
    {
        public bool TryLoadFromFile(string path, uint mediaId, bool preservePlaybackState, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                error = LocalizationService.Mark("No media file selected.");
                return false;
            }

            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                error = LocalizationService.Mark("The selected media file does not exist.");
                return false;
            }

            try
            {
                var wasPlaying = preservePlaybackState ? _desiredPlaying : false;
                DisposeSource();
                _source = _audio.CreateSpatialSource(fullPath, streamFromDisk: true, allowHrtf: true);
                _source.SetDopplerFactor(0f);
                _source.SetVolumePercent(_volumePercent);
                _mediaPath = fullPath;
                _mediaId = mediaId;
                _desiredPlaying = wasPlaying;
                if (_desiredPlaying && !_pausedByGame)
                    _source.Play(loop: _loopPlayback);
                return true;
            }
            catch (IOException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (ArgumentException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (NotSupportedException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (ObjectDisposedException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (InvalidOperationException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryLoadFromBytes(byte[] data, string extension, uint mediaId, bool preservePlaybackState, out string error)
        {
            error = string.Empty;
            if (data == null || data.Length == 0)
            {
                error = LocalizationService.Mark("Received media is empty.");
                return false;
            }

            try
            {
                var folder = Path.Combine(Path.GetTempPath(), "TopSpeed", "Radio");
                Directory.CreateDirectory(folder);
                var normalizedExtension = NormalizeExtension(extension);
                var path = Path.Combine(folder, $"radio_{mediaId}_{Guid.NewGuid():N}{normalizedExtension}");
                File.WriteAllBytes(path, data);
                if (TryLoadFromFile(path, mediaId, preservePlaybackState, out error))
                {
                    ReplaceOwnedTempFile(path);
                    return true;
                }

                SafeDelete(path);
                return false;
            }
            catch (IOException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (ArgumentException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (NotSupportedException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (ObjectDisposedException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (InvalidOperationException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string NormalizeExtension(string extension)
        {
            var trimmed = (extension ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return ".bin";
            if (!trimmed.StartsWith(".", StringComparison.Ordinal))
                trimmed = "." + trimmed;
            if (trimmed.Length > ProtocolConstants.MaxMediaFileExtensionLength + 1)
                trimmed = trimmed.Substring(0, ProtocolConstants.MaxMediaFileExtensionLength + 1);
            return trimmed;
        }
    }
}

