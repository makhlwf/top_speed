using TopSpeed.Collision;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class CollisionBehaviorTests
{
    [Fact]
    public void RearEndCollision_TransfersSpeed_FromRearToFront()
    {
        var rear = new VehicleCollisionBody(0f, 100f, 120f, 1.8f, 4.5f, 1500f);
        var front = new VehicleCollisionBody(0f, 101.8f, 90f, 1.8f, 4.5f, 1500f);

        var collided = VehicleCollisionResolver.TryResolve(rear, front, out var response);

        collided.Should().BeTrue();
        response.First.SpeedDeltaKph.Should().BeLessThan(0f);
        response.Second.SpeedDeltaKph.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void RearEndCollision_UsesMassWeightedExchange()
    {
        var rearLight = new VehicleCollisionBody(0f, 100f, 120f, 1.8f, 4.5f, 1000f);
        var frontHeavy = new VehicleCollisionBody(0f, 101.8f, 90f, 1.8f, 4.5f, 2000f);

        var collided = VehicleCollisionResolver.TryResolve(rearLight, frontHeavy, out var response);

        collided.Should().BeTrue();
        (-response.First.SpeedDeltaKph).Should().BeGreaterThan(response.Second.SpeedDeltaKph);
    }

    [Fact]
    public void SideContact_SeparatesVehiclesByLateralDirection()
    {
        var right = new VehicleCollisionBody(0.5f, 100f, 100f, 1.8f, 4.5f, 1500f);
        var left = new VehicleCollisionBody(-0.5f, 100f, 100f, 1.8f, 4.5f, 1500f);

        var collided = VehicleCollisionResolver.TryResolve(right, left, out var response);

        collided.Should().BeTrue();
        response.First.BumpX.Should().BeGreaterThan(0f);
        response.Second.BumpX.Should().BeLessThan(0f);
    }

    [Fact]
    public void RearEndCollision_ReportsImpactContext()
    {
        var rear = new VehicleCollisionBody(0f, 100f, 130f, 1.8f, 4.5f, 1500f);
        var front = new VehicleCollisionBody(0f, 101.8f, 80f, 1.8f, 4.5f, 1500f);

        var collided = VehicleCollisionResolver.TryResolve(rear, front, out var response);

        collided.Should().BeTrue();
        response.ContactType.Should().Be(VehicleCollisionContactType.RearEnd);
        response.ImpactSeverity.Should().BeGreaterThan(0f);
        response.RelativeSpeedKph.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void WallConsequence_ConstrainsBumpInsideWall_AndAppliesPenalty()
    {
        var body = new VehicleCollisionBody(4.9f, 100f, 120f, 1.8f, 4.5f, 1500f);
        var impulse = new VehicleCollisionImpulse(0.4f, 0f, 0f);
        var response = new VehicleCollisionResponse(
            impulse,
            new VehicleCollisionImpulse(-0.4f, 0f, 0f),
            VehicleCollisionContactType.RearEnd,
            impactSeverity: 0.9f,
            relativeSpeedKph: 65f);

        var adjusted = CollisionWallConsequence.Apply(body, impulse, response, wallHalfWidthM: 5f);
        var predictedX = body.PositionX + (2f * adjusted.BumpX);

        predictedX.Should().BeLessThanOrEqualTo(5.0001f);
        adjusted.SpeedDeltaKph.Should().BeLessThan(impulse.SpeedDeltaKph);
    }
}
