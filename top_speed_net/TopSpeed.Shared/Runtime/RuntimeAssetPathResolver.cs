using System;
using System.IO;

namespace TopSpeed.Runtime
{
    public static class RuntimeAssetPathResolver
    {
        public static string? ResolveExistingPath(string rootPath, params string[] segments)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || segments == null || segments.Length == 0)
                return null;
            if (!Directory.Exists(rootPath))
                return null;

            var currentPath = rootPath;
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (string.IsNullOrWhiteSpace(segment))
                    return null;

                var normalized = segment
                    .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                    .Replace(Path.DirectorySeparatorChar == '\\' ? '/' : '\\', Path.DirectorySeparatorChar);
                var parts = normalized.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    return null;

                for (var j = 0; j < parts.Length; j++)
                {
                    var resolvedPath = ResolveExistingChild(currentPath, parts[j]);
                    if (resolvedPath == null)
                        return null;
                    currentPath = resolvedPath;
                }
            }

            return currentPath;
        }

        private static string? ResolveExistingChild(string parentPath, string childName)
        {
            if (!Directory.Exists(parentPath))
                return null;

            var exactPath = Path.Combine(parentPath, childName);
            if (Directory.Exists(exactPath) || File.Exists(exactPath))
                return exactPath;

            foreach (var entryPath in Directory.EnumerateFileSystemEntries(parentPath))
            {
                if (string.Equals(Path.GetFileName(entryPath), childName, StringComparison.OrdinalIgnoreCase))
                    return entryPath;
            }

            return null;
        }
    }
}
