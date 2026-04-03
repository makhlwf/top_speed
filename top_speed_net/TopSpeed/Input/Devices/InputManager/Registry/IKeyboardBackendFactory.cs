using System;
using TopSpeed.Input.Devices.Keyboard;
using TopSpeed.Runtime;

namespace TopSpeed.Input
{
    internal interface IKeyboardBackendFactory
    {
        string Id { get; }
        int Priority { get; }

        bool IsSupported();
        IKeyboardDevice Create(IntPtr windowHandle, IKeyboardEventSource? eventSource);
    }
}
