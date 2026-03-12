namespace TopSpeed.Race
{
    internal abstract partial class RaceMode
    {
        public void Dispose()
        {
            _soundQueue.Clear();
            _panelManager.Dispose();
            _localRadio.Dispose();
            _car.Dispose();
            _track.Dispose();
            DisposeSound(_soundStart);
            DisposeSound(_soundBestTime);
            DisposeSound(_soundNewTime);
            DisposeSound(_soundYourTime);
            DisposeSound(_soundMinute);
            DisposeSound(_soundMinutes);
            DisposeSound(_soundSecond);
            DisposeSound(_soundSeconds);
            DisposeSound(_soundPoint);
            DisposeSound(_soundPercent);
            DisposeSound(_soundTheme4);
            DisposeSound(_soundPause);
            DisposeSound(_soundUnpause);
            DisposeSound(_soundTrackName);

            for (var i = 0; i < _soundNumbers.Length; i++)
                DisposeSound(_soundNumbers[i]);

            for (var i = 0; i < _soundUnkey.Length; i++)
                DisposeSound(_soundUnkey[i]);

            for (var i = 0; i < _soundLaps.Length; i++)
                DisposeSound(_soundLaps[i]);

            for (var i = 0; i < _randomSounds.Length; i++)
            {
                var count = _totalRandomSounds[i];
                for (var j = 0; j < count && j < _randomSounds[i].Length; j++)
                    DisposeSound(_randomSounds[i][j]);
            }
        }

        protected static void DisposeSound(TS.Audio.AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            sound.Stop();
            sound.Dispose();
        }
    }
}

