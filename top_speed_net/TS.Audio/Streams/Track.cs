using System;
using System.Collections.Generic;

namespace TS.Audio
{
    public sealed class TrackStream : IDisposable
    {
        private readonly AudioOutput _output;
        private readonly AudioBus _bus;
        private readonly StreamAsset[] _assets;
        private readonly bool _ownsAssets;
        private AudioSourceHandle? _source;
        private bool _loopSingle;
        private int _currentIndex;
        private bool _disposed;

        internal TrackStream(AudioOutput output, AudioBus bus, bool ownsAssets, params StreamAsset[] assets)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            if (assets == null || assets.Length == 0)
                throw new ArgumentException("At least one stream asset is required.", nameof(assets));

            _assets = assets;
            _ownsAssets = ownsAssets;
        }

        public bool IsPlaying => _source != null && _source.IsPlaying;

        public void Play(bool loop)
        {
            ThrowIfDisposed();
            _loopSingle = loop;
            EnsureCurrentSource();
            if (_source == null)
                return;

            _source.SetOnEnd(OnTrackEnded);
            _source.Play(ShouldLoopIndex(_currentIndex));
            _output.Diagnostics.Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.StreamPlayRequested,
                AudioDiagnosticEntityType.Stream,
                _output.Name,
                _bus.Name,
                null,
                "Audio stream playback requested.",
                new Dictionary<string, object?>
                {
                    ["loop"] = loop,
                    ["index"] = _currentIndex
                });
        }

        public void Stop()
        {
            _source?.Stop();
            _output.Diagnostics.Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.StreamStopped,
                AudioDiagnosticEntityType.Stream,
                _output.Name,
                _bus.Name,
                null,
                "Audio stream stopped.",
                new Dictionary<string, object?>
                {
                    ["index"] = _currentIndex
                });
        }

        public void SetVolume(float volume)
        {
            _source?.SetVolume(volume);
        }

        public void SetPitch(float pitch)
        {
            _source?.SetPitch(pitch);
        }

        public void SetPan(float pan)
        {
            _source?.SetPan(pan);
        }

        internal void Update()
        {
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _source?.Dispose();
            _source = null;

            if (_ownsAssets)
            {
                for (var i = 0; i < _assets.Length; i++)
                    _assets[i].Dispose();
            }

            _output.RemoveStream(this);
        }

        private void EnsureCurrentSource()
        {
            if (_source != null)
                return;

            CreateSourceForIndex(_currentIndex);
        }

        private void CreateSourceForIndex(int index)
        {
            _source?.Dispose();
            _source = _output.CreateSource(_assets[index].Asset, spatialize: false, useHrtf: false, bus: _bus, ownsAsset: false);
        }

        private void OnTrackEnded()
        {
            if (_disposed)
                return;

            if (_assets.Length <= 1)
            {
                if (_loopSingle)
                    _source?.Play(loop: true);
                return;
            }

            if (_currentIndex < _assets.Length - 1)
            {
                _currentIndex++;
                CreateSourceForIndex(_currentIndex);
                _source?.SetOnEnd(OnTrackEnded);
                _source?.Play(ShouldLoopIndex(_currentIndex));
                return;
            }

            if (ShouldLoopIndex(_currentIndex))
                _source?.Play(loop: true);
        }

        private bool ShouldLoopIndex(int index)
        {
            if (_assets.Length <= 1)
                return _loopSingle;

            return index == _assets.Length - 1;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TrackStream));
        }
    }
}
