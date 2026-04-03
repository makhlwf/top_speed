using System;
using TopSpeed.Input.Devices.Keyboard;
using TopSpeed.Runtime;

namespace TopSpeed.Input.Devices.Keyboard.Backends.Eto
{
    internal sealed class Factory : IKeyboardBackendFactory
    {
        public string Id => "eto";
        public int Priority => 200;

        public bool IsSupported()
        {
#if NETFRAMEWORK
            return false;
#else
            return true;
#endif
        }

        public IKeyboardDevice Create(IntPtr windowHandle, IKeyboardEventSource? eventSource)
        {
            if (eventSource == null)
                throw new InvalidOperationException("Eto keyboard backend requires a keyboard event source.");

            return new Device(eventSource);
        }
    }
}
