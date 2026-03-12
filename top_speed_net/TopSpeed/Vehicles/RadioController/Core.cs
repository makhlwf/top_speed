using System;
using TopSpeed.Audio;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal sealed partial class VehicleRadioController : IDisposable
    {
        private readonly AudioManager _audio;
        private AudioSourceHandle? _source;
        private bool _desiredPlaying;
        private bool _pausedByGame;
        private string? _mediaPath;
        private string? _ownedTempFile;
        private uint _mediaId;
        private int _volumePercent = 100;
        private bool _loopPlayback = true;

        public VehicleRadioController(AudioManager audio)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        public uint MediaId => _mediaId;
        public bool HasMedia => _source != null;
        public bool IsPlaying => _source != null && _source.IsPlaying;
        public bool DesiredPlaying => _desiredPlaying;
        public string? MediaPath => _mediaPath;
        public int VolumePercent => _volumePercent;
        public bool LoopPlayback => _loopPlayback;

        public void SetVolumePercent(int volumePercent)
        {
            if (volumePercent < 0)
                volumePercent = 0;
            if (volumePercent > 100)
                volumePercent = 100;

            _volumePercent = volumePercent;
            _source?.SetVolumePercent(_volumePercent);
        }

        public void SetLoopPlayback(bool loopPlayback)
        {
            _loopPlayback = loopPlayback;
            if (_source == null || !_desiredPlaying || _pausedByGame || !_source.IsPlaying)
                return;

            _source.Stop();
            _source.Play(loop: _loopPlayback);
        }
    }
}
