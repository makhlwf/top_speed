using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TopSpeed.Core.Updates
{
    [DataContract]
    internal sealed class InfoDoc
    {
        [DataMember(Name = "version")]
        public string? Version { get; set; }

        [DataMember(Name = "changes")]
        public List<string>? Changes { get; set; }
    }

    [DataContract]
    internal sealed class ReleaseDoc
    {
        [DataMember(Name = "assets")]
        public List<ReleaseAssetDoc>? Assets { get; set; }
    }

    [DataContract]
    internal sealed class ReleaseAssetDoc
    {
        [DataMember(Name = "name")]
        public string? Name { get; set; }

        [DataMember(Name = "browser_download_url")]
        public string? DownloadUrl { get; set; }

        [DataMember(Name = "size")]
        public long? Size { get; set; }
    }
}

