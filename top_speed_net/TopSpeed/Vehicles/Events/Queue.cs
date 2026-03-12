using System;
using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Vehicles.Events
{
    internal sealed class EventQueue
    {
        private readonly List<EventEntry> _items = new List<EventEntry>();

        public void Push(float dueTime, EventType type, VibrationEffectType? effect = null)
        {
            _items.Add(new EventEntry
            {
                Type = type,
                Time = dueTime,
                Effect = effect
            });
        }

        public void DrainDue(float now, Action<EventEntry> onDue)
        {
            for (var i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (item.Time >= now)
                    continue;
                onDue(item);
                _items.RemoveAt(i);
            }
        }
    }
}
