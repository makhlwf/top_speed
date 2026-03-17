using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TopSpeed.Server.Updates
{
    internal sealed class UpdateManifestDoc
    {
        [JsonPropertyName("serverVersion")]
        public string? ServerVersion { get; set; }

        [JsonPropertyName("serverChanges")]
        public List<string>? ServerChanges { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("changes")]
        public List<string>? Changes { get; set; }
    }

    internal sealed class ReleaseDoc
    {
        [JsonPropertyName("assets")]
        public List<ReleaseAssetDoc>? Assets { get; set; }
    }

    internal sealed class ReleaseAssetDoc
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }
    }
}
