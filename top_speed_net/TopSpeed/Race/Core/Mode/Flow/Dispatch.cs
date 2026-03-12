using TopSpeed.Common;
using TopSpeed.Race.Events;

namespace TopSpeed.Race
{
    internal abstract partial class RaceMode
    {
        private void DispatchRaceEvent(RaceEvent e)
        {
            if (HandleSharedLifecycleEvent(e))
                return;

            switch (e.Type)
            {
                case RaceEventType.PlaySound:
                    QueueSound(e.Sound);
                    break;
                case RaceEventType.PlayRadioSound:
                    _unkeyQueue--;
                    if (_unkeyQueue == 0)
                        Speak(_soundUnkey[Algorithm.RandomInt(MaxUnkeys)]);
                    break;
                default:
                    OnUnhandledRaceEvent(e);
                    break;
            }
        }
    }
}

