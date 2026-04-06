using System.Linq;
using System.Threading.Tasks;
using TopSpeed.Data;
using TopSpeed.Protocol;
using VerifyXunit;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Regression")]
public sealed class BotPhysicsRegressionTests
{
    [Fact]
    public Task OfficialLaunchMatrix_ShouldMatchSnapshot()
    {
        var traces = BotPhysicsHarness.OfficialCars()
            .Select(carType => BotPhysicsHarness.SimulateLaunch(carType))
            .ToArray();

        return Verifier.Verify(traces, SnapshotSettings.Create("Shared", "Bots"));
    }

    [Fact]
    public Task RepresentativeHandlingScenarios_ShouldMatchSnapshot()
    {
        var traces = new object[]
        {
            BotPhysicsHarness.SimulateScenario("GT-R gravel drift-correction", CarType.Vehicle1, TrackSurface.Gravel, throttle: 72, brake: 0, steering: 28, steps: 45, elapsedSeconds: 0.1f, initialSpeedKph: 95f),
            BotPhysicsHarness.SimulateScenario("Sprinter wet braking", CarType.Vehicle9, TrackSurface.Water, throttle: 0, brake: -100, steering: 10, steps: 35, elapsedSeconds: 0.1f, initialSpeedKph: 82f),
            BotPhysicsHarness.SimulateScenario("Panigale sand acceleration", CarType.Vehicle11, TrackSurface.Sand, throttle: 100, brake: 0, steering: -18, steps: 45, elapsedSeconds: 0.1f, initialSpeedKph: 40f),
            BotPhysicsHarness.BuildCatalogSnapshot()
        };

        return Verifier.Verify(traces, SnapshotSettings.Create("Shared", "Bots"));
    }
}
