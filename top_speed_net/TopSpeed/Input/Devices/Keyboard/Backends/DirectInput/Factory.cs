using System;
using TopSpeed.Input.Devices.Keyboard;
using TopSpeed.Runtime;

namespace TopSpeed.Input.Devices.Keyboard.Backends.DirectInput
{
    internal sealed class Factory : IKeyboardBackendFactory
    {
        public string Id => "directinput";
        public int Priority => 100;

        public bool IsSupported()
        {
            return true;
        }

        public IKeyboardDevice Create(IntPtr windowHandle, IKeyboardEventSource? eventSource)
        {
            return new Device(windowHandle);
        }
    }
}
