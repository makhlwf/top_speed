using System.Linq;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "Regression")]
    public sealed class PowertrainRegressionTests
    {
        [Fact]
        public Task NeutralCoastMatrix_ShouldMatchSnapshot()
        {
            var traces = TopSpeed.Vehicles.OfficialVehicleCatalog.Vehicles
                .Select(spec => PowertrainHarness.SimulateNeutralCoast(spec))
                .ToArray();

            return Verifier.Verify(traces, SnapshotSettings.Create("Shared", "Physics"));
        }

        [Fact]
        public Task OfficialVehicleCatalog_ShouldMatchSnapshot()
        {
            return Verifier.Verify(PowertrainHarness.BuildCatalogSnapshot(), SnapshotSettings.Create("Shared", "Physics"));
        }
    }
}
