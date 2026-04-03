using System;

namespace TopSpeed.Runtime
{
    internal interface IWindowHost : IDisposable
    {
        event Action? Loaded;
        event Action? Closed;

        IntPtr NativeHandle { get; }

        void Run();
        void RequestClose();
    }
}



