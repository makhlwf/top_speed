using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Regression")]
public sealed class ProtocolRegressionTests
{
    [Fact]
    public Task PacketShapes_ShouldMatchSnapshot()
    {
        return Verifier.Verify(ProtocolHarness.BuildSnapshot(), SnapshotSettings.Create("Client", "Network"));
    }
}
