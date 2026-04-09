using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TopSpeed.Core
{
    internal static class AppData
    {
        private const string AppName = "Top Speed";

        public static string Root()
        {
            var path = ResolveRoot();
            Directory.CreateDirectory(path);
            return path;
        }

        public static string File(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name is required.", nameof(fileName));

            return Path.Combine(Root(), fileName);
        }

        private static string ResolveRoot()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(roaming, AppName);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                return Path.Combine(home, "Library", "Application Support", AppName);
            }

            var xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrWhiteSpace(xdg))
                return Path.Combine(xdg!, AppName);

            var profile = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            return Path.Combine(profile, ".config", AppName);
        }
    }
}
