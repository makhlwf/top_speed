using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace TS.Audio
{
    public static class AudioDebugLog
    {
        private static readonly object Sync = new object();
        private static StreamWriter? _writer;
        private static bool _enabled;
        private static bool _verbose;
        private static int _intervalMs = 500;
        private static long _intervalTicks;
        private static long _nextLogTicks;
        private static readonly Dictionary<string, string> LastValues = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, long> NextLogTicksByKey = new Dictionary<string, long>(StringComparer.Ordinal);

        public static bool Enabled => _enabled;
        public static bool Verbose => _verbose;
        public static int Interval => _intervalMs;

        public static void Configure(string? path, bool verbose, int intervalMs)
        {
            if (intervalMs < 1)
                intervalMs = 1;

            var resolved = path;
            if (string.IsNullOrWhiteSpace(resolved))
            {
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                resolved = Path.Combine(Directory.GetCurrentDirectory(), $"steam_audio_debug_{stamp}.log");
            }

            lock (Sync)
            {
                _writer?.Dispose();
                _writer = new StreamWriter(resolved, append: false) { AutoFlush = true };
                _enabled = true;
                _verbose = verbose;
                _intervalMs = intervalMs;
                _intervalTicks = Math.Max(1, intervalMs * Stopwatch.Frequency / 1000);
                _nextLogTicks = Stopwatch.GetTimestamp();
                LastValues.Clear();
                NextLogTicksByKey.Clear();
                _writer.WriteLine($"{DateTime.Now:O} SteamAudio debug log started");
                _writer.WriteLine($"{DateTime.Now:O} Path={resolved} Verbose={_verbose} IntervalMs={_intervalMs}");
            }
        }

        public static void Write(string message)
        {
            if (!_enabled)
                return;

            lock (Sync)
            {
                _writer?.WriteLine($"{DateTime.Now:O} {message}");
            }
        }

        public static void WriteIfChanged(string key, string message)
        {
            if (!_enabled)
                return;

            lock (Sync)
            {
                if (LastValues.TryGetValue(key, out var last) && string.Equals(last, message, StringComparison.Ordinal))
                    return;
                LastValues[key] = message;
                _writer?.WriteLine($"{DateTime.Now:O} {message}");
            }
        }

        public static bool BeginTick()
        {
            if (!_enabled)
                return false;
            if (_verbose)
                return true;

            var now = Stopwatch.GetTimestamp();
            var next = Interlocked.Read(ref _nextLogTicks);
            if (now < next)
                return false;

            var updated = now + _intervalTicks;
            var original = Interlocked.CompareExchange(ref _nextLogTicks, updated, next);
            return original == next;
        }

        public static bool BeginTick(string key)
        {
            if (!_enabled)
                return false;
            if (_verbose)
                return true;
            if (string.IsNullOrEmpty(key))
                return BeginTick();

            var now = Stopwatch.GetTimestamp();
            lock (Sync)
            {
                if (NextLogTicksByKey.TryGetValue(key, out var next) && now < next)
                    return false;
                NextLogTicksByKey[key] = now + _intervalTicks;
                return true;
            }
        }
    }
}
