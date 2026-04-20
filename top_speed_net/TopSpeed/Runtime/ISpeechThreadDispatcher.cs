using System;

namespace TopSpeed.Runtime
{
    public interface ISpeechThreadDispatcher
    {
        T Invoke<T>(Func<T> action);
    }
}
