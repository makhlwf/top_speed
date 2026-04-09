using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TopSpeed.Data;

namespace TopSpeed.Core
{
    internal static class TrackId
    {
        public static string FromSelection(string nameOrPath)
        {
            if (string.IsNullOrWhiteSpace(nameOrPath))
                return "builtin:america";

            if (TrackCatalog.BuiltIn.ContainsKey(nameOrPath))
                return $"builtin:{nameOrPath}";

            var normalized = NormalizePath(nameOrPath);
            var bytes = Encoding.UTF8.GetBytes(normalized);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                for (var i = 0; i < hash.Length; i++)
                    builder.Append(hash[i].ToString("x2"));
                return $"custom:{builder}";
            }
        }

        private static string NormalizePath(string path)
        {
            var full = Path.GetFullPath(path.Trim());
            full = full.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return Environment.OSVersion.Platform == PlatformID.Win32NT
                ? full.ToUpperInvariant()
                : full;
        }
    }
}
