using TopSpeed.Collision;
using Xunit;

namespace TopSpeed.Tests.Collision
{
    [Trait("Category", "SharedCollision")]
    public sealed class CollisionContextTests
    {
        [Fact]
        public void RearEndCollision_ReportsImpactContext()
        {
            var rear = new VehicleCollisionBody(0f, 100f, 130f, 1.8f, 4.5f, 1500f);
            var front = new VehicleCollisionBody(0f, 101.8f, 80f, 1.8f, 4.5f, 1500f);

            var collided = VehicleCollisionResolver.TryResolve(rear, front, out var response);

            Assert.True(collided);
            Assert.Equal(VehicleCollisionContactType.RearEnd, response.ContactType);
            Assert.True(response.ImpactSeverity > 0f);
            Assert.True(response.RelativeSpeedKph > 0f);
        }

        [Fact]
        public void WallConsequence_ConstrainBumpInsideWall_AndApplyPenalty()
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

            Assert.True(predictedX <= 5.0001f);
            Assert.True(adjusted.SpeedDeltaKph < impulse.SpeedDeltaKph);
        }
    }
}



