using System;
using TopSpeed.Input;

namespace TopSpeed.Runtime
{
    internal interface IKeyboardEventSource
    {
        event Action<InputKey>? KeyDown;
        event Action<InputKey>? KeyUp;
    }
}
