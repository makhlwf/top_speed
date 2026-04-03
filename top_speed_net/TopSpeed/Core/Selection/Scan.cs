using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TopSpeed.Core
{
    internal static class Scan
    {
        public static List<string> Find(string rootFolder, string pattern)
        {
            var root = Path.Combine(AssetPaths.Root, rootFolder);
            if (!Directory.Exists(root))
                return new List<string>();

            var files = new List<string>();
            IEnumerable<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories);
            }
            catch (IOException)
            {
                return files;
            }
            catch (UnauthorizedAccessException)
            {
                return files;
            }
            catch (ArgumentException)
            {
                return files;
            }
            catch (NotSupportedException)
            {
                return files;
            }

            foreach (var directory in directories)
            {
                string? first;
                try
                {
                    first = Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly)
                        .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault();
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (ArgumentException)
                {
                    continue;
                }
                catch (NotSupportedException)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(first))
                    files.Add(first);
            }

            return files;
        }

        public static bool TryCached<T>(
            string file,
            Dictionary<string, (DateTime LastWriteUtc, T Value)> cache,
            Func<string, (bool Success, T Value)> parse,
            out T value)
        {
            value = default!;

            var hasStamp = false;
            var lastWriteUtc = DateTime.MinValue;
            try
            {
                lastWriteUtc = File.GetLastWriteTimeUtc(file);
                hasStamp = true;
            }
            catch (IOException)
            {
                hasStamp = false;
            }
            catch (UnauthorizedAccessException)
            {
                hasStamp = false;
            }
            catch (ArgumentException)
            {
                hasStamp = false;
            }
            catch (NotSupportedException)
            {
                hasStamp = false;
            }

            if (hasStamp &&
                cache.TryGetValue(file, out var entry) &&
                entry.LastWriteUtc == lastWriteUtc)
            {
                value = entry.Value;
                return true;
            }

            var parsed = parse(file);
            if (!parsed.Success)
                return false;
            value = parsed.Value;
            if (hasStamp)
                cache[file] = (lastWriteUtc, value);
            return true;
        }

        public static void Prune<T>(
            Dictionary<string, (DateTime LastWriteUtc, T Value)> cache,
            HashSet<string> known)
        {
            var staleKeys = cache.Keys.Where(key => !known.Contains(key)).ToList();
            for (var i = 0; i < staleKeys.Count; i++)
                cache.Remove(staleKeys[i]);
        }
    }
}

