using System;
using System.IO;
using System.Runtime.Serialization;
using TopSpeed.Input;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        public void Save(RaceSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            settings.AudioVolumes ??= new AudioVolumeSettings();
            settings.AudioVolumes.ClampAll();
            settings.SyncMusicVolumeFromAudioCategories();

            var document = BuildDocument(settings);
            try
            {
                WriteDocument(_settingsPath, document);
            }
            catch (IOException)
            {
                // Ignore settings write failures.
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore settings write failures.
            }
            catch (SerializationException)
            {
                // Ignore settings write failures.
            }
            catch (NotSupportedException)
            {
                // Ignore settings write failures.
            }
        }
    }
}

