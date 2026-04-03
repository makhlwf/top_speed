using System;
using TopSpeed.Input.Devices.Keyboard;
using TopSpeed.Runtime;

namespace TopSpeed.Input
{
    internal interface IBackendRegistry
    {
        IKeyboardDevice CreateKeyboard(IntPtr windowHandle, IKeyboardEventSource? eventSource);
        IControllerBackend CreateController(IntPtr windowHandle);
    }
}
