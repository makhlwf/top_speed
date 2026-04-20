using System.Numerics;
using TopSpeed.Audio;

namespace TopSpeed.Vehicles
{
    internal sealed partial class VehicleRadioController
    {
        public void SetPlayback(bool playing)
        {
            _desiredPlaying = playing;
            if (_source == null || _pausedByGame)
                return;

            if (playing)
            {
                if (_source.IsPaused)
                    _source.Resume();
                else if (!_source.IsPlaying)
                    _source.Play(loop: _loopPlayback);
            }
            else if (_source.IsPlaying || _source.IsPaused)
            {
                _source.Pause();
            }
        }

        public void TogglePlayback()
        {
            SetPlayback(!_desiredPlaying);
        }

        public void PauseForGame()
        {
            _pausedByGame = true;
            if (_source != null)
                _source.Pause();
        }

        public void ResumeFromGame()
        {
            _pausedByGame = false;
            if (_source == null || !_desiredPlaying)
                return;

            if (_source.IsPaused)
                _source.Resume();
            else if (!_source.IsPlaying)
                _source.Play(loop: _loopPlayback);
        }

        public void ClearMedia()
        {
            _desiredPlaying = false;
            _mediaId = 0;
            _mediaPath = null;
            DisposeSource();
        }

        public void UpdateSpatial(float worldX, float worldZ, Vector3 worldVelocity)
        {
            if (_source == null)
                return;

            _source.SetPosition(AudioWorld.Position(worldX, worldZ));
            _source.SetVelocity(AudioWorld.ToMeters(worldVelocity));
        }
    }
}

