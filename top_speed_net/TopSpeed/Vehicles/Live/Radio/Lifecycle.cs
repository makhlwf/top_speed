using System;

namespace TopSpeed.Vehicles.Live
{
    internal sealed partial class LiveRadio
    {
        public void Dispose()
        {
            lock (_lock)
                StopLocked();
        }

        private void StopLocked()
        {
            if (_source != null)
            {
                _source.Stop();
                _source.Dispose();
                _source = null;
            }

            _decoder = null;
            _streamId = 0;
            _sampleRate = 0;
            _channels = 0;
            _frameMs = 0;
            _decodeBuffer = Array.Empty<short>();
            _activeFrame = null;
            _activeFrameOffset = 0;
            _frames.Clear();
        }
    }
}

