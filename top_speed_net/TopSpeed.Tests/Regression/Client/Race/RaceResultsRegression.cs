using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Regression")]
public sealed class RaceResultsRegressionTests
{
    [Fact]
    public Task DialogPlans_ShouldMatchSnapshot()
    {
        return Verifier.Verify(ResultHarness.BuildSnapshot(), SnapshotSettings.Create("Client", "Race"));
    }
}
