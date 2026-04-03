using TS.Audio;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        public void Dispose()
        {
            FinalizeTrack();
            DisposeSound(_soundCrowd);
            DisposeSound(_soundOcean);
            DisposeSound(_soundRain);
            DisposeSound(_soundWind);
            DisposeSound(_soundStorm);
            DisposeSound(_soundDesert);
            DisposeSound(_soundAirport);
            DisposeSound(_soundAirplane);
            DisposeSound(_soundClock);
            DisposeSound(_soundJet);
            DisposeSound(_soundThunder);
            DisposeSound(_soundPile);
            DisposeSound(_soundConstruction);
            DisposeSound(_soundRiver);
            DisposeSound(_soundHelicopter);
            DisposeSound(_soundOwl);

            for (var i = 0; i < _allTrackSounds.Count; i++)
                _allTrackSounds[i].Dispose();

            DisposePendingHandleStops();
        }

        private static void DisposeSound(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            sound.Stop();
            sound.Dispose();
        }
    }
}

