using System;

namespace TopSpeed.Input.Backends.DirectInput
{
    internal sealed class Factory : IControllerBackendFactory
    {
        public string Id => "directinput";
        public int Priority => 100;

        public bool IsSupported()
        {
            return true;
        }

        public IControllerBackend Create(IntPtr windowHandle)
        {
            return new ControllerBackend(windowHandle);
        }
    }
}

