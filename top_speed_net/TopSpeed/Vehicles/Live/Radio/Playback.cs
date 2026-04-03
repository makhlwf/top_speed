using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Input;

namespace TopSpeed.Vehicles.Live
{
    internal sealed partial class LiveRadio
    {
        public void SetPlayback(bool playing)
        {
            lock (_lock)
            {
                _desiredPlaying = playing;
                UpdatePlaybackLocked();
            }
        }

        public void PauseForGame()
        {
            lock (_lock)
            {
                _pausedByGame = true;
                UpdatePlaybackLocked();
            }
        }

        public void ResumeFromGame()
        {
            lock (_lock)
            {
                _pausedByGame = false;
                UpdatePlaybackLocked();
            }
        }

        public void SetVolumePercent(int volumePercent)
        {
            if (volumePercent < 0)
                volumePercent = 0;
            if (volumePercent > 100)
                volumePercent = 100;

            lock (_lock)
            {
                _volumePercent = volumePercent;
                _source?.SetVolumePercent(_settings, AudioVolumeCategory.Radio, _volumePercent);
            }
        }

        public void UpdateSpatial(Vector3 position, Vector3 velocity)
        {
            lock (_lock)
            {
                _position = position;
                _velocity = velocity;
                _source?.SetPosition(position);
                _source?.SetVelocity(velocity);
            }
        }

        private void UpdatePlaybackLocked()
        {
            if (_source == null)
                return;

            if (_desiredPlaying && !_pausedByGame)
            {
                if (!_source.IsPlaying)
                    _source.Play(loop: true);
            }
            else if (_source.IsPlaying)
            {
                _source.Stop();
            }
        }
    }
}

