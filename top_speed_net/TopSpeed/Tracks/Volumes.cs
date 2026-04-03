using TS.Audio;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        public void SetAmbientVolumePercent(int percent)
        {
            if (percent < 0)
                percent = 0;
            if (percent > 100)
                percent = 100;
            if (_ambientVolumePercent == percent)
                return;

            _ambientVolumePercent = percent;
            _ambientVolumeScale = percent / 100f;
            ApplyAmbientVolumeToLegacyHandles();
            ApplyAmbientVolumeToTrackSounds();
        }

        private void ApplyAmbientVolumeToLegacyHandles()
        {
            ApplyAmbientVolume(_soundCrowd);
            ApplyAmbientVolume(_soundOcean);
            ApplyAmbientVolume(_soundRain);
            ApplyAmbientVolume(_soundWind);
            ApplyAmbientVolume(_soundStorm);
            ApplyAmbientVolume(_soundDesert);
            ApplyAmbientVolume(_soundAirport);
            ApplyAmbientVolume(_soundAirplane);
            ApplyAmbientVolume(_soundClock);
            ApplyAmbientVolume(_soundJet);
            ApplyAmbientVolume(_soundThunder);
            ApplyAmbientVolume(_soundPile);
            ApplyAmbientVolume(_soundConstruction);
            ApplyAmbientVolume(_soundRiver);
            ApplyAmbientVolume(_soundHelicopter);
            ApplyAmbientVolume(_soundOwl);
        }

        private void ApplyAmbientVolumeToTrackSounds()
        {
            for (var i = 0; i < _allTrackSounds.Count; i++)
                _allTrackSounds[i].ApplyCategoryVolume(_ambientVolumeScale);
        }

        private void ApplyAmbientVolume(AudioSourceHandle? handle)
        {
            handle?.SetVolume(_ambientVolumeScale);
        }
    }
}

