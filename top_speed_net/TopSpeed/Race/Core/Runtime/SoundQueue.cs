using System;
using System.Collections.Generic;
using TS.Audio;

namespace TopSpeed.Race.Runtime
{
    internal sealed class SoundQueue
    {
        private readonly Queue<AudioSourceHandle> _queue = new Queue<AudioSourceHandle>();
        private readonly object _lock = new object();
        private AudioSourceHandle? _current;

        public void Enqueue(AudioSourceHandle sound)
        {
            lock (_lock)
            {
                _queue.Enqueue(sound);
                if (_current == null)
                    PlayNextLocked();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _queue.Clear();
                _current = null;
            }
        }

        public bool IsIdle
        {
            get
            {
                lock (_lock)
                    return _current == null && _queue.Count == 0;
            }
        }

        private void PlayNextLocked()
        {
            if (_queue.Count == 0)
            {
                _current = null;
                return;
            }

            var next = _queue.Dequeue();
            _current = next;
            next.Stop();
            next.SeekToStart();
            next.SetOnEnd(() => OnEnd(next));
            next.Play(loop: false);
        }

        private void OnEnd(AudioSourceHandle finished)
        {
            lock (_lock)
            {
                if (!ReferenceEquals(_current, finished))
                    return;
                _current = null;
                PlayNextLocked();
            }
        }
    }
}

