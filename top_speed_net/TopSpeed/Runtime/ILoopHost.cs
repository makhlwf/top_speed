using System;

namespace TopSpeed.Runtime
{
    internal interface ILoopHost : IDisposable
    {
        bool IsRunning { get; }

        void Start(Action<float> onTick, Func<int> resolveIntervalMs);
        void Stop();
    }
}



