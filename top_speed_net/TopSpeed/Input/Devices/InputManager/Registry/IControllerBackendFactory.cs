using System;

namespace TopSpeed.Input
{
    internal interface IControllerBackendFactory
    {
        string Id { get; }
        int Priority { get; }

        bool IsSupported();
        IControllerBackend Create(IntPtr windowHandle);
    }
}

