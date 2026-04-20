using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Runtime;
using TopSpeed.Speech.Playback;
using TopSpeed.Speech.ScreenReaders;

namespace TopSpeed.Speech
{
    internal sealed class ScreenReaderWorker : IDisposable
    {
        private readonly IMode _mode;
        private bool _disposed;

        public ScreenReaderWorker(IScreenReader screenReader, IPlayer player)
        {
            if (screenReader == null)
                throw new ArgumentNullException(nameof(screenReader));
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            var dispatcher = SpeechThreadRuntime.GetDispatcher();
            _mode = dispatcher == null
                ? new ThreadMode(screenReader, player)
                : new DispatcherMode(screenReader, player, dispatcher);
        }

        public T Invoke<T>(Func<IScreenReader, T> operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScreenReaderWorker));

            return _mode.Invoke(operation);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _mode.Dispose();
        }

        private interface IMode : IDisposable
        {
            T Invoke<T>(Func<IScreenReader, T> operation);
        }

        private sealed class DispatcherMode : IMode
        {
            private readonly IScreenReader _screenReader;
            private readonly ISpeechThreadDispatcher _dispatcher;
            private bool _disposed;

            public DispatcherMode(IScreenReader screenReader, IPlayer player, ISpeechThreadDispatcher dispatcher)
            {
                _screenReader = screenReader;
                _dispatcher = dispatcher;
                _dispatcher.Invoke(() =>
                {
                    _screenReader.BindPlayer(player);
                    return 0;
                });
            }

            public T Invoke<T>(Func<IScreenReader, T> operation)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(DispatcherMode));

                return _dispatcher.Invoke(() => operation(_screenReader));
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                try
                {
                    _dispatcher.Invoke(() =>
                    {
                        try
                        {
                            _screenReader.Close();
                        }
                        catch
                        {
                        }

                        return 0;
                    });
                }
                catch
                {
                }
            }
        }

        private sealed class ThreadMode : IMode
        {
            private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
            private readonly ManualResetEventSlim _startupReady = new ManualResetEventSlim(false);
            private readonly Thread _thread;
            private readonly IScreenReader _screenReader;
            private readonly IPlayer _player;
            private Exception? _startupError;
            private int _threadId;
            private bool _disposed;

            public ThreadMode(IScreenReader screenReader, IPlayer player)
            {
                _screenReader = screenReader;
                _player = player;
                _thread = new Thread(Run)
                {
                    IsBackground = true,
                    Name = "TopSpeed.Speech.ScreenReaderWorker"
                };
                _thread.Start();
                _startupReady.Wait();
                if (_startupError != null)
                {
                    Dispose();
                    throw new InvalidOperationException("Failed to initialize speech worker.", _startupError);
                }
            }

            public T Invoke<T>(Func<IScreenReader, T> operation)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ThreadMode));

                if (Thread.CurrentThread.ManagedThreadId == _threadId)
                    return operation(_screenReader);

                var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    _queue.Add(() =>
                    {
                        try
                        {
                            completion.TrySetResult(operation(_screenReader));
                        }
                        catch (Exception ex)
                        {
                            completion.TrySetException(ex);
                        }
                    });
                }
                catch (InvalidOperationException ex)
                {
                    throw new ObjectDisposedException(nameof(ThreadMode), ex.Message);
                }

                return completion.Task.GetAwaiter().GetResult();
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _queue.CompleteAdding();
                _thread.Join();
                _startupReady.Dispose();
                _queue.Dispose();
            }

            private void Run()
            {
                _threadId = Thread.CurrentThread.ManagedThreadId;
                try
                {
                    _screenReader.BindPlayer(_player);
                }
                catch (Exception ex)
                {
                    _startupError = ex;
                }
                finally
                {
                    _startupReady.Set();
                }

                if (_startupError != null)
                {
                    CloseReaderQuietly();
                    return;
                }

                foreach (var work in _queue.GetConsumingEnumerable())
                    work();

                CloseReaderQuietly();
            }

            private void CloseReaderQuietly()
            {
                try
                {
                    _screenReader.Close();
                }
                catch
                {
                }
            }
        }
    }
}
