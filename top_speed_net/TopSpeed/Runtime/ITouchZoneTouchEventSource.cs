using System;
using TS.Sdl.Input;

namespace TopSpeed.Runtime
{
    internal interface ITouchZoneTouchEventSource
    {
        event Action<TouchZoneTouchEvent>? TouchZoneTouchRaised;
    }
}
