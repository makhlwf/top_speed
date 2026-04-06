using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Regression")]
public sealed class CollisionRegressionTests
{
    [Fact]
    public Task RepresentativeScenarios_ShouldMatchSnapshot()
    {
        return Verifier.Verify(CollisionHarness.BuildRepresentativeScenarios(), SnapshotSettings.Create("Shared", "Collision"));
    }
}
