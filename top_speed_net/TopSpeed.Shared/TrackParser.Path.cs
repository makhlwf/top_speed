using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TopSpeed.Data
{
    public static partial class TrackTsmParser
    {
        public static string? ResolveTrackPath(string nameOrPath)
        {
            if (string.IsNullOrWhiteSpace(nameOrPath))
                return null;
            var trimmed = nameOrPath.Trim();

            if (Directory.Exists(trimmed))
            {
                var fromDirectory = TryResolveFirstTrackFile(trimmed);
                if (fromDirectory != null)
                    return fromDirectory;
            }

            if (File.Exists(trimmed))
            {
                var asFile = Path.GetFullPath(trimmed);
                return IsFolderTrackPath(asFile) ? asFile : null;
            }

            var tracksRoot = Path.Combine(AppContext.BaseDirectory, "Tracks");
            var candidates = new List<string>
            {
                Path.Combine(tracksRoot, trimmed)
            };

            if (Path.HasExtension(trimmed))
                candidates.Add(Path.Combine(tracksRoot, Path.GetFileName(trimmed)));

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (Directory.Exists(fullPath))
                {
                    var fromDirectory = TryResolveFirstTrackFile(fullPath);
                    if (fromDirectory != null)
                        return fromDirectory;
                }
                else if (IsFolderTrackPath(fullPath))
                    return fullPath;
            }

            return null;
        }

        private static bool IsFolderTrackPath(string path)
        {
            if (!File.Exists(path))
                return false;

            if (!string.Equals(Path.GetExtension(path), ".tsm", StringComparison.OrdinalIgnoreCase))
                return false;

            var directory = Path.GetDirectoryName(path);
            return !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory);
        }

        private static string? TryResolveFirstTrackFile(string directory)
        {
            if (!Directory.Exists(directory))
                return null;

            var first = Directory.EnumerateFiles(directory, "*.tsm", SearchOption.TopDirectoryOnly)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(first))
                return null;

            var fullPath = Path.GetFullPath(first);
            return IsFolderTrackPath(fullPath) ? fullPath : null;
        }
    }
}
