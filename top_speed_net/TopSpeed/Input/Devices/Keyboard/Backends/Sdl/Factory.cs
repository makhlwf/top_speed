using System;
using TopSpeed.Input.Devices.Keyboard;
using TopSpeed.Runtime;
using SdlRuntime = TS.Sdl.Runtime;

namespace TopSpeed.Input.Devices.Keyboard.Backends.Sdl
{
    internal sealed class Factory : IKeyboardBackendFactory
    {
        public string Id => "sdl";
        public int Priority => 10;

        public bool IsSupported()
        {
            return SdlRuntime.IsAvailable;
        }

        public IKeyboardDevice Create(IntPtr windowHandle, IKeyboardEventSource? eventSource)
        {
            return new Device();
        }
    }
}
