using System;
using System.Collections.Generic;
using TS.Sdl.Input;

namespace TopSpeed.Runtime
{
    internal interface ITouchZoneGestureEventSource
    {
        event Action<TouchZoneGestureEvent>? TouchZoneGestureRaised;

        void SetTouchZones(IReadOnlyList<TouchZone> zones);
        void ClearTouchZones();
    }
}

