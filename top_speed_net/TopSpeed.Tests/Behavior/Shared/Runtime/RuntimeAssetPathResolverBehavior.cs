using System;
using System.IO;
using TopSpeed.Runtime;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "Behavior")]
    public sealed class RuntimeAssetPathResolverBehaviorTests
    {
        [Fact]
        public void ResolveExistingPath_ShouldMatchSegmentsCaseInsensitively()
        {
            using var fixture = new AssetFixture();

            var resolved = RuntimeAssetPathResolver.ResolveExistingPath(
                fixture.RootPath,
                "Sounds",
                "en",
                "music\\theme4.ogg");

            resolved.Should().Be(fixture.SoundPath);
        }

        private sealed class AssetFixture : IDisposable
        {
            public AssetFixture()
            {
                RootPath = Path.Combine(Path.GetTempPath(), "topspeed-asset-paths-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path.Combine(RootPath, "Sounds", "En", "Music"));
                SoundPath = Path.Combine(RootPath, "Sounds", "En", "Music", "theme4.ogg");
                File.WriteAllBytes(SoundPath, Array.Empty<byte>());
            }

            public string RootPath { get; }

            public string SoundPath { get; }

            public void Dispose()
            {
                if (Directory.Exists(RootPath))
                    Directory.Delete(RootPath, recursive: true);
            }
        }
    }
}
