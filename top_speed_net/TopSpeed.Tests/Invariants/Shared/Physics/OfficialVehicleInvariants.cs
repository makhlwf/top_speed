using FsCheck.Fluent;
using FsCheck.Xunit;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "Invariant")]
    public sealed class OfficialVehicleInvariantTests
    {
        [Property(MaxTest = 80, Arbitrary = new[] { typeof(OfficialVehicleArbitraries) })]
        public void GearRatios_ShouldRemainDescending(OfficialVehicleScenario scenario)
        {
            scenario.Spec.GearRatios.Should().BeInDescendingOrder();
        }

        [Property(MaxTest = 80, Arbitrary = new[] { typeof(OfficialVehicleArbitraries) })]
        public void NeutralCoast_ShouldNotIncreaseSpeed(OfficialVehicleScenario scenario)
        {
            var trace = PowertrainHarness.SimulateNeutralCoast(scenario.Spec, scenario.StartSpeedKph, scenario.Seconds);

            trace.FinalSpeedKph.Should().BeLessThanOrEqualTo(trace.StartSpeedKph + 0.001f);
            trace.FinalSpeedKph.Should().BeGreaterThanOrEqualTo(0f);
        }
    }

    public sealed record OfficialVehicleScenario(OfficialVehicleSpec Spec, float StartSpeedKph, float Seconds);

    public static class OfficialVehicleArbitraries
    {
        public static FsCheck.Arbitrary<OfficialVehicleScenario> OfficialVehicleScenario()
        {
            var generator =
                from index in Gen.Choose(1, OfficialVehicleCatalog.Vehicles.Length)
                from startSpeed in Gen.Choose(40, 160)
                from seconds in Gen.Choose(2, 12)
                select new OfficialVehicleScenario(
                    OfficialVehicleCatalog.Get(index),
                    StartSpeedKph: startSpeed,
                    Seconds: seconds);

            return Arb.From(generator);
        }
    }
}
