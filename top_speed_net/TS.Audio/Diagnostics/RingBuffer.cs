using System.Collections.Generic;

namespace TS.Audio
{
    internal sealed class AudioDiagnosticRingBuffer<T>
    {
        private readonly Queue<T> _items;
        private int _capacity;

        public AudioDiagnosticRingBuffer(int capacity)
        {
            _items = new Queue<T>(capacity > 0 ? capacity : 1);
            _capacity = capacity > 0 ? capacity : 1;
        }

        public void SetCapacity(int capacity)
        {
            _capacity = capacity > 0 ? capacity : 1;
            while (_items.Count > _capacity)
                _items.Dequeue();
        }

        public void Add(T item)
        {
            while (_items.Count >= _capacity)
                _items.Dequeue();
            _items.Enqueue(item);
        }

        public List<T> Snapshot()
        {
            return new List<T>(_items);
        }

        public void Clear()
        {
            _items.Clear();
        }
    }
}
