namespace TopSpeed.Runtime
{
    public static class MotionSteeringRuntime
    {
        private static readonly object Sync = new object();
        private static IMotionSteeringSource? _source;

        public static void SetSource(IMotionSteeringSource? source)
        {
            IMotionSteeringSource? previous;
            lock (Sync)
            {
                previous = _source;
                _source = source;
            }

            previous?.Dispose();
        }

        public static bool TryGetSteeringAngleRadians(out float angleRadians)
        {
            IMotionSteeringSource? source;
            lock (Sync)
                source = _source;

            if (source == null || !source.IsAvailable)
            {
                angleRadians = 0f;
                return false;
            }

            return source.TryGetSteeringAngleRadians(out angleRadians);
        }

        public static void Recenter()
        {
            IMotionSteeringSource? source;
            lock (Sync)
                source = _source;

            source?.Recenter();
        }
    }
}
