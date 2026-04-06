using System.IO;
using VerifyTests;

namespace TopSpeed.Tests
{
    internal static class SnapshotSettings
    {
        public static VerifySettings Create(params string[] parts)
        {
            var settings = new VerifySettings();
            var segments = new string[parts.Length + 1];
            segments[0] = "Snapshots";
            for (var i = 0; i < parts.Length; i++)
                segments[i + 1] = parts[i];
            settings.UseDirectory(Path.Combine(segments));
            return settings;
        }
    }
}
