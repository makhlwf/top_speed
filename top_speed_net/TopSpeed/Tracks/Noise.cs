using System;
using TS.Audio;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        private void CalculateNoiseLength()
        {
            _noiseLength = 0;
            var i = _currentRoad;
            while (i < _segmentCount && _definition[i].Noise == _definition[_currentRoad].Noise)
            {
                _noiseLength += _definition[i].Length;
                i++;
            }

            _noisePlaying = true;
        }

        private void UpdateLoopingNoise(AudioSourceHandle? sound, float position, int? pan = null)
        {
            if (sound == null)
                return;

            if (!_noisePlaying)
            {
                CalculateNoiseLength();
                _noiseStartPos = position;
                _noiseEndPos = position + _noiseLength;
            }

            _factor = (position - _noiseStartPos) * 1.0f / _noiseLength;
            if (_factor < 0.5f)
                _factor *= 2.0f;
            else
                _factor = 2.0f * (1.0f - _factor);

            SetVolumePercent(sound, (int)(80.0f + _factor * 20.0f));
            if (!sound.IsPlaying)
            {
                if (pan.HasValue)
                    sound.SetPan(pan.Value / 100f);
                sound.Play(loop: true);
            }
        }

        private static void PlayIfNotPlaying(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            if (!sound.IsPlaying)
                sound.Play(loop: false);
        }

        private void SetVolumePercent(AudioSourceHandle sound, int volume)
        {
            var clamped = Math.Max(0, Math.Min(100, volume));
            sound.SetVolume((clamped / 100f) * _ambientVolumeScale);
        }
    }
}

