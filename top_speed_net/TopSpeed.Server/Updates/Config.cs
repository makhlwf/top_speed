using System;
using System.Runtime.InteropServices;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Updates
{
    internal sealed class ServerUpdateConfig
    {
        private const string RepoOwner = "diamondStar35";
        private const string RepoName = "top_speed";

        public ServerUpdateConfig(
            string infoUrl,
            string latestReleaseApiUrl,
            string assetTemplate,
            string runtimeAssetTag,
            string updaterExeName,
            string serverExeName)
        {
            InfoUrl = infoUrl ?? throw new ArgumentNullException(nameof(infoUrl));
            LatestReleaseApiUrl = latestReleaseApiUrl ?? throw new ArgumentNullException(nameof(latestReleaseApiUrl));
            AssetTemplate = assetTemplate ?? throw new ArgumentNullException(nameof(assetTemplate));
            RuntimeAssetTag = runtimeAssetTag ?? throw new ArgumentNullException(nameof(runtimeAssetTag));
            UpdaterExeName = updaterExeName ?? throw new ArgumentNullException(nameof(updaterExeName));
            ServerExeName = serverExeName ?? throw new ArgumentNullException(nameof(serverExeName));
        }

        public string InfoUrl { get; }
        public string LatestReleaseApiUrl { get; }
        public string AssetTemplate { get; }
        public string RuntimeAssetTag { get; }
        public string UpdaterExeName { get; }
        public string ServerExeName { get; }

        public static ServerUpdateConfig Default { get; } = new ServerUpdateConfig(
            $"https://raw.githubusercontent.com/{RepoOwner}/{RepoName}/main/info.json",
            $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest",
            "TopSpeed.Server-{runtime}-Release-v-{version}.zip",
            DetectRuntimeAssetTag(),
            "Updater.exe",
            DetectServerExecutableName());

        public static ServerVersion CurrentVersion =>
            new ServerVersion(
                ReleaseVersionInfo.ServerYear,
                ReleaseVersionInfo.ServerMonth,
                ReleaseVersionInfo.ServerDay,
                ReleaseVersionInfo.ServerRevision);

        public string BuildExpectedAssetName(string versionText)
        {
            return AssetTemplate
                .Replace("{runtime}", RuntimeAssetTag)
                .Replace("{version}", versionText ?? string.Empty);
        }

        private static string DetectRuntimeAssetTag()
        {
            var runtimeIdentifier = (RuntimeInformation.RuntimeIdentifier ?? string.Empty).ToLowerInvariant();
            if (runtimeIdentifier.Contains("linux-musl-x64"))
                return "linux-musl-x64";
            if (runtimeIdentifier.Contains("linux-musl-arm64"))
                return "linux-musl-arm64";
            if (runtimeIdentifier.Contains("linux-x64"))
                return "linux-x64";
            if (runtimeIdentifier.Contains("linux-arm64"))
                return "linux-arm64";
            if (runtimeIdentifier.Contains("linux-arm"))
                return "linux-arm32";
            if (runtimeIdentifier.Contains("linux-x86"))
                return "linux-x86-fdd";
            if (runtimeIdentifier.Contains("win-x64"))
                return "win-x64";

            if (OperatingSystem.IsWindows())
                return "win-x64";

            if (OperatingSystem.IsLinux())
            {
                return RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => "linux-x64",
                    Architecture.Arm64 => "linux-arm64",
                    Architecture.Arm => "linux-arm32",
                    Architecture.X86 => "linux-x86-fdd",
                    _ => "linux-x64"
                };
            }

            return "win-x64";
        }

        private static string DetectServerExecutableName()
        {
            return OperatingSystem.IsWindows()
                ? "TopSpeed.Server.exe"
                : "TopSpeed.Server";
        }
    }
}
