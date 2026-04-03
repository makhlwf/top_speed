using System.IO;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Input;
using TopSpeed.Race;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void ShowRaceResultDialog(RaceResultSummary summary)
        {
            _resultShow.Show(summary);
        }

        private void PlayRaceWinSound()
        {
            if (_raceWinSound == null)
            {
                if (!TryLoadRaceWinSound(out var handle))
                    return;
                _raceWinSound = handle;
            }

            try
            {
                var handle = _raceWinSound;
                if (handle == null)
                    return;
                handle.SetVolumePercent(_settings, AudioVolumeCategory.OnlineServerEvents, 100);
                handle.Restart(loop: false);
            }
            catch
            {
            }
        }

        private bool TryLoadRaceWinSound(out TS.Audio.AudioSourceHandle? handle)
        {
            handle = null;
            var audio = _audio as AudioManager;
            if (audio == null)
                return false;

            var path = Path.Combine(AssetPaths.SoundsRoot, "network", "win.ogg");
            if (!audio.TryResolvePath(path, out var fullPath))
                return false;

            try
            {
                handle = audio.AcquireCachedSource(fullPath, streamFromDisk: false);
                return handle != null;
            }
            catch
            {
                return false;
            }
        }
    }
}

