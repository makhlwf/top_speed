using TS.Audio;

namespace TopSpeed.Race.Events
{
    internal sealed class RaceEvent
    {
        public RaceEventType Type { get; set; }
        public float Time { get; set; }
        public AudioSourceHandle? Sound { get; set; }
        public long Sequence { get; set; }
    }
}

