using System;

namespace TopSpeed.Input.Backends.Sdl
{
    internal sealed class Factory : IControllerBackendFactory
    {
        public string Id => "sdl";
        public int Priority => 10;

        public bool IsSupported()
        {
            return false;
        }

        public IControllerBackend Create(IntPtr windowHandle)
        {
            return new Controller();
        }
    }
}
