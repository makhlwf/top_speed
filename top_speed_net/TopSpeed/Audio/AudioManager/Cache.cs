using System;
using System.Collections.Generic;
using System.IO;
using TS.Audio;

namespace TopSpeed.Audio
{
    internal sealed partial class AudioManager
    {
        private readonly struct AudioCacheKey
        {
            public readonly string Path;
            public readonly bool StreamFromDisk;
            public readonly bool UseHrtf;

            public AudioCacheKey(string path, bool streamFromDisk, bool useHrtf)
            {
                Path = path;
                StreamFromDisk = streamFromDisk;
                UseHrtf = useHrtf;
            }
        }

        private sealed class AudioCacheKeyComparer : IEqualityComparer<AudioCacheKey>
        {
            public bool Equals(AudioCacheKey x, AudioCacheKey y)
            {
                return x.StreamFromDisk == y.StreamFromDisk
                    && x.UseHrtf == y.UseHrtf
                    && string.Equals(x.Path, y.Path, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(AudioCacheKey obj)
            {
                var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Path);
                hash = (hash * 397) ^ obj.StreamFromDisk.GetHashCode();
                hash = (hash * 397) ^ obj.UseHrtf.GetHashCode();
                return hash;
            }
        }

        private sealed class CachedSource
        {
            public AudioSourceHandle Handle { get; }
            public int RefCount { get; set; }

            public CachedSource(AudioSourceHandle handle)
            {
                Handle = handle;
                RefCount = 1;
            }
        }

        public bool TryResolvePath(string path, out string fullPath)
        {
            fullPath = string.Empty;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            fullPath = Path.GetFullPath(path);
            lock (_pathCacheLock)
            {
                if (_pathExistsCache.TryGetValue(fullPath, out var exists))
                    return exists;
                exists = File.Exists(fullPath);
                _pathExistsCache[fullPath] = exists;
                return exists;
            }
        }

        public AudioSourceHandle AcquireCachedSource(string path, bool streamFromDisk = true, bool useHrtf = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Audio path is required.", nameof(path));

            var fullPath = Path.GetFullPath(path);
            var key = new AudioCacheKey(fullPath, streamFromDisk, useHrtf);
            lock (_cacheLock)
            {
                if (_sourceCache.TryGetValue(key, out var cached))
                {
                    cached.RefCount++;
                    return cached.Handle;
                }

                var handle = CreateSource(fullPath, streamFromDisk, useHrtf);
                _sourceCache[key] = new CachedSource(handle);
                _handleCache[handle] = key;
                return handle;
            }
        }

        public void ReleaseCachedSource(AudioSourceHandle? handle)
        {
            if (handle == null)
                return;
            lock (_cacheLock)
            {
                if (!_handleCache.TryGetValue(handle, out var key))
                    return;

                if (!_sourceCache.TryGetValue(key, out var cached))
                {
                    _handleCache.Remove(handle);
                    return;
                }

                cached.RefCount--;
                if (cached.RefCount > 0)
                    return;

                _sourceCache.Remove(key);
                _handleCache.Remove(handle);
                cached.Handle.Dispose();
            }
        }

        private void ClearCachedSources()
        {
            lock (_cacheLock)
            {
                foreach (var cached in _sourceCache.Values)
                    cached.Handle.Dispose();
                _sourceCache.Clear();
                _handleCache.Clear();
            }

            lock (_pathCacheLock)
            {
                _pathExistsCache.Clear();
            }
        }
    }
}

