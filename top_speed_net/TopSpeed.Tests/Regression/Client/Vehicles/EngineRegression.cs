using System.Linq;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "Regression")]
    public sealed class EngineRegressionTests
    {
        [Fact]
        public Task EngineRuntimeTraces_ShouldMatchSnapshot()
        {
            var traces = new object[]
            {
                EngineHarness.AutomaticVehicles
                    .Select(carType => EngineHarness.SimulateAutomaticLaunch(TopSpeed.Vehicles.OfficialVehicleCatalog.Get((int)carType)))
                    .ToArray(),
                EngineHarness.SimulateDisengagedRevBlip(),
                EngineHarness.SimulateFreeRevShutdown(),
                EngineHarness.SimulateBackDrivenCombustionOff()
            };

            return Verifier.Verify(traces, SnapshotSettings.Create("Client", "Vehicles"));
        }
    }
}
