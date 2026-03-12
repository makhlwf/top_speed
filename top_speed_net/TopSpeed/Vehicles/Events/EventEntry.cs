using TopSpeed.Input;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Vehicles.Events
{
    internal sealed class EventEntry
    {
        public float Time { get; set; }
        public EventType Type { get; set; }
        public VibrationEffectType? Effect { get; set; }
    }
}
