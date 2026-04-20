using System;
using System.Threading.Tasks;
using Android.OS;
using Java.Lang;
using TopSpeed.Runtime;

namespace TopSpeed.Android;

internal sealed class AndroidSpeechThreadDispatcher : Java.Lang.Object, ISpeechThreadDispatcher, IDisposable
{
    private readonly HandlerThread _thread;
    private readonly Handler _handler;
    private bool _disposed;

    public AndroidSpeechThreadDispatcher()
    {
        _thread = new HandlerThread("TopSpeed.Speech");
        _thread.Start();
        _handler = new Handler(_thread.Looper!);
    }

    public T Invoke<T>(Func<T> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        if (_disposed)
            throw new ObjectDisposedException(nameof(AndroidSpeechThreadDispatcher));

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var posted = _handler.Post(new Runnable(() =>
        {
            try
            {
                completion.TrySetResult(action());
            }
            catch (System.Exception ex)
            {
                completion.TrySetException(ex);
            }
        }));

        if (!posted)
            throw new InvalidOperationException("Failed to post speech action to Android handler thread.");

        return completion.Task.GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _handler.RemoveCallbacksAndMessages(null);
        _handler.Dispose();
        _thread.QuitSafely();
        _thread.Join();
        _thread.Dispose();
    }
}
