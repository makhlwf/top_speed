using TopSpeed.Data;
using TopSpeed.Input;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void SaveMusicVolume(float volume)
        {
            _settings.MusicVolume = volume;
            _settings.SyncAudioCategoriesFromMusicVolume();
            ApplyAudioSettings();
            SaveSettings();
        }

        private void ApplyAudioSettings()
        {
            _settings.AudioVolumes ??= new AudioVolumeSettings();
            _settings.SyncMusicVolumeFromAudioCategories();
            _audio.SetMasterVolume(_settings.GetCategoryScalar(AudioVolumeCategory.Master));
            _menu.SetMenuMusicVolume(_settings.MusicVolume);
        }
    }
}

