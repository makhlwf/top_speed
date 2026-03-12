using System;
using System.Collections.Generic;
using System.Numerics;
using Concentus.Structs;
using TopSpeed.Audio;
using TopSpeed.Input;
using TS.Audio;

namespace TopSpeed.Vehicles.Live
{
    internal sealed partial class LiveRadio : IDisposable
    {
        private const int MaxBufferedFrames = 12;

        private readonly AudioManager _audio;
        private readonly RaceSettings _settings;
        private readonly object _lock = new object();
        private readonly Queue<float[]> _frames;

        private AudioSourceHandle? _source;
        private OpusDecoder? _decoder;
        private short[] _decodeBuffer;
        private float[]? _activeFrame;
        private int _activeFrameOffset;
        private int _volumePercent;
        private bool _desiredPlaying;
        private bool _pausedByGame;
        private uint _streamId;
        private ushort _sampleRate;
        private byte _channels;
        private byte _frameMs;
        private Vector3 _position;
        private Vector3 _velocity;
        private long _receivedFrames;
        private long _decodedFrames;
        private long _droppedFrames;
        private long _decodeErrors;
        private long _underruns;
        private long _lastFrameUtcTicks;

        public LiveRadio(AudioManager audio, RaceSettings settings)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _frames = new Queue<float[]>(MaxBufferedFrames);
            _decodeBuffer = Array.Empty<short>();
            _volumePercent = 100;
        }

        public bool IsActive
        {
            get
            {
                lock (_lock)
                    return _streamId != 0 && _decoder != null;
            }
        }

        public uint StreamId
        {
            get
            {
                lock (_lock)
                    return _streamId;
            }
        }

        public long ReceivedFrames
        {
            get
            {
                lock (_lock)
                    return _receivedFrames;
            }
        }

        public long DecodedFrames
        {
            get
            {
                lock (_lock)
                    return _decodedFrames;
            }
        }

        public long DroppedFrames
        {
            get
            {
                lock (_lock)
                    return _droppedFrames;
            }
        }

        public long DecodeErrors
        {
            get
            {
                lock (_lock)
                    return _decodeErrors;
            }
        }

        public long Underruns
        {
            get
            {
                lock (_lock)
                    return _underruns;
            }
        }

        public long LastFrameUtcTicks
        {
            get
            {
                lock (_lock)
                    return _lastFrameUtcTicks;
            }
        }
    }
}
