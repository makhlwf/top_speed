using FsCheck.Fluent;
using FsCheck.Xunit;
using TopSpeed.Collision;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Invariant")]
public sealed class CollisionInvariantTests
{
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(CollisionArbitraries) })]
    public void OverlappingBodies_ShouldProduceFiniteResponse(CollisionScenario scenario)
    {
        var collided = VehicleCollisionResolver.TryResolve(scenario.First, scenario.Second, out var response);

        collided.Should().BeTrue();
        IsFinite(response.First.BumpX).Should().BeTrue();
        IsFinite(response.First.BumpY).Should().BeTrue();
        IsFinite(response.First.SpeedDeltaKph).Should().BeTrue();
        IsFinite(response.Second.BumpX).Should().BeTrue();
        IsFinite(response.Second.BumpY).Should().BeTrue();
        IsFinite(response.Second.SpeedDeltaKph).Should().BeTrue();
        IsFinite(response.ImpactSeverity).Should().BeTrue();
        IsFinite(response.RelativeSpeedKph).Should().BeTrue();
        response.ImpactSeverity.Should().BeInRange(0f, 1f);
        response.RelativeSpeedKph.Should().BeGreaterThanOrEqualTo(0f);
    }

    [Property(MaxTest = 100, Arbitrary = new[] { typeof(CollisionArbitraries) })]
    public void SwappingBodies_ShouldPreserveCollisionAndRelativeSpeed(CollisionScenario scenario)
    {
        var collidedForward = VehicleCollisionResolver.TryResolve(scenario.First, scenario.Second, out var forward);
        var collidedSwapped = VehicleCollisionResolver.TryResolve(scenario.Second, scenario.First, out var swapped);

        collidedForward.Should().Be(collidedSwapped);
        if (!collidedForward)
            return;

        swapped.RelativeSpeedKph.Should().BeApproximately(forward.RelativeSpeedKph, 0.001f);
        swapped.ImpactSeverity.Should().BeApproximately(forward.ImpactSeverity, 0.001f);
    }

    [Property(MaxTest = 100, Arbitrary = new[] { typeof(CollisionArbitraries) })]
    public void SeparatedBodies_ShouldNotCollide(SeparatedCollisionScenario scenario)
    {
        VehicleCollisionResolver.TryResolve(scenario.First, scenario.Second, out _).Should().BeFalse();
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}

public sealed record CollisionScenario(VehicleCollisionBody First, VehicleCollisionBody Second);

public sealed record SeparatedCollisionScenario(VehicleCollisionBody First, VehicleCollisionBody Second);

public static class CollisionArbitraries
{
    public static FsCheck.Arbitrary<CollisionScenario> CollisionScenario()
    {
        var generator =
            from baseX in Gen.Choose(-200, 200)
            from baseY in Gen.Choose(0, 500)
            from width1 in Gen.Choose(12, 24)
            from width2 in Gen.Choose(12, 24)
            from length1 in Gen.Choose(35, 60)
            from length2 in Gen.Choose(35, 60)
            from dx in Gen.Choose(-15, 15)
            from dy in Gen.Choose(-35, 35)
            from speed1 in Gen.Choose(0, 180)
            from speed2 in Gen.Choose(0, 180)
            from mass1 in Gen.Choose(600, 3000)
            from mass2 in Gen.Choose(600, 3000)
            select new CollisionScenario(
                new VehicleCollisionBody(baseX / 10f, baseY, speed1, width1 / 10f, length1 / 10f, mass1),
                new VehicleCollisionBody(
                    (baseX / 10f) + (dx / 10f),
                    baseY + (dy / 10f),
                    speed2,
                    width2 / 10f,
                    length2 / 10f,
                    mass2));

        return Arb.From(generator.Where(x => Overlaps(x.First, x.Second)));
    }

    public static FsCheck.Arbitrary<SeparatedCollisionScenario> SeparatedCollisionScenario()
    {
        var generator =
            from width1 in Gen.Choose(12, 24)
            from width2 in Gen.Choose(12, 24)
            from length1 in Gen.Choose(35, 60)
            from length2 in Gen.Choose(35, 60)
            from speed1 in Gen.Choose(0, 180)
            from speed2 in Gen.Choose(0, 180)
            from mass1 in Gen.Choose(600, 3000)
            from mass2 in Gen.Choose(600, 3000)
            from gap in Gen.Choose(1, 50)
            select new SeparatedCollisionScenario(
                new VehicleCollisionBody(0f, 100f, speed1, width1 / 10f, length1 / 10f, mass1),
                new VehicleCollisionBody(10f + gap, 120f + gap, speed2, width2 / 10f, length2 / 10f, mass2));

        return Arb.From(generator);
    }

    private static bool Overlaps(in VehicleCollisionBody first, in VehicleCollisionBody second)
    {
        var halfWidthSum = (first.WidthM * 0.5f) + (second.WidthM * 0.5f);
        var halfLengthSum = (first.LengthM * 0.5f) + (second.LengthM * 0.5f);
        return System.Math.Abs(first.PositionX - second.PositionX) < halfWidthSum
            && System.Math.Abs(first.PositionY - second.PositionY) < halfLengthSum;
    }
}
