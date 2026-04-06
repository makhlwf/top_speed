using TopSpeed.Protocol;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "Behavior")]
    public sealed class VehicleCatalogBehaviorTests
    {
        [Theory]
        [InlineData(CarType.Vehicle10)]
        [InlineData(CarType.Vehicle11)]
        [InlineData(CarType.Vehicle12)]
        public void Motorcycles_ShouldUseTightUpperGears_AndReasonableCoastDrag(CarType carType)
        {
            var spec = OfficialVehicleCatalog.Get((int)carType);
            var fifthTop = PowertrainHarness.GearTopSpeedKph(spec, 5);
            var sixthTop = PowertrainHarness.GearTopSpeedKph(spec, 6);

            spec.GearRatios.Length.Should().Be(6);
            spec.GearRatios.Should().BeInDescendingOrder();
            sixthTop.Should().BeInRange(spec.TopSpeed * 0.95f, spec.TopSpeed * 1.10f);
            sixthTop.Should().BeLessThanOrEqualTo(fifthTop * 1.15f);
            spec.CoastDragBaseMps2.Should().BeInRange(0.10f, 0.25f);
            spec.CoastDragLinearPerMps.Should().BeInRange(0.005f, 0.02f);
        }

        [Theory]
        [InlineData(CarType.Vehicle6)]
        [InlineData(CarType.Vehicle8)]
        [InlineData(CarType.Vehicle9)]
        public void AutomaticFamily_ShouldUseUsefulTopGears_AndExplicitCoastDrag(CarType carType)
        {
            var spec = OfficialVehicleCatalog.Get((int)carType);
            var top = PowertrainHarness.GearTopSpeedKph(spec, spec.GearRatios.Length);
            var previous = PowertrainHarness.GearTopSpeedKph(spec, spec.GearRatios.Length - 1);

            top.Should().BeGreaterThan(previous);
            top.Should().BeLessThanOrEqualTo(previous * 1.22f);
            top.Should().BeInRange(spec.TopSpeed * 1.00f, spec.TopSpeed * 1.12f);
            spec.CoastDragBaseMps2.Should().BeInRange(0.18f, 0.28f);
            spec.CoastDragLinearPerMps.Should().BeInRange(0.010f, 0.020f);
        }

        [Theory]
        [InlineData(CarType.Vehicle1)]
        [InlineData(CarType.Vehicle2)]
        [InlineData(CarType.Vehicle7)]
        public void PerformanceFamily_ShouldUsePullingTopGears_AndExplicitCoastDrag(CarType carType)
        {
            var spec = OfficialVehicleCatalog.Get((int)carType);
            var top = PowertrainHarness.GearTopSpeedKph(spec, spec.GearRatios.Length);
            var previous = PowertrainHarness.GearTopSpeedKph(spec, spec.GearRatios.Length - 1);

            top.Should().BeGreaterThan(previous);
            top.Should().BeLessThanOrEqualTo(previous * 1.20f);
            top.Should().BeInRange(spec.TopSpeed * 0.98f, spec.TopSpeed * 1.08f);
            spec.CoastDragBaseMps2.Should().BeInRange(0.18f, 0.24f);
            spec.CoastDragLinearPerMps.Should().BeInRange(0.010f, 0.015f);
        }

        [Theory]
        [InlineData(CarType.Vehicle3)]
        [InlineData(CarType.Vehicle4)]
        [InlineData(CarType.Vehicle5)]
        public void ManualFamily_ShouldUseReasonableTopGears_AndExplicitCoastDrag(CarType carType)
        {
            var spec = OfficialVehicleCatalog.Get((int)carType);
            var top = PowertrainHarness.GearTopSpeedKph(spec, spec.GearRatios.Length);
            var previous = PowertrainHarness.GearTopSpeedKph(spec, spec.GearRatios.Length - 1);

            top.Should().BeGreaterThan(previous);
            top.Should().BeLessThanOrEqualTo(previous * 1.26f);
            top.Should().BeInRange(spec.TopSpeed * 0.98f, spec.TopSpeed * 1.10f);
            spec.CoastDragBaseMps2.Should().BeInRange(0.10f, 0.13f);
            spec.CoastDragLinearPerMps.Should().BeInRange(0.006f, 0.008f);
        }
    }
}
