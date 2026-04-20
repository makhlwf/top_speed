namespace TopSpeed.Runtime
{
    public static class SpeechThreadRuntime
    {
        private static readonly object Sync = new object();
        private static ISpeechThreadDispatcher? _dispatcher;

        public static void SetDispatcher(ISpeechThreadDispatcher? dispatcher)
        {
            lock (Sync)
                _dispatcher = dispatcher;
        }

        public static ISpeechThreadDispatcher? GetDispatcher()
        {
            lock (Sync)
                return _dispatcher;
        }
    }
}
