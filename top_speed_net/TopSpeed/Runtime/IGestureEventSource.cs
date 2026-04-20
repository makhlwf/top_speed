using System;
using TS.Sdl.Input;

namespace TopSpeed.Runtime
{
    internal interface IGestureEventSource
    {
        event Action<GestureEvent>? GestureRaised;
    }
}

