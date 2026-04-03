using TopSpeed.Collision;
using Xunit;

namespace TopSpeed.Tests.Collision
{
    [Trait("Category", "SharedCollision")]
    public sealed class VehicleCollisionResolverTests
    {
        [Fact]
        public void RearEndCollision_TransfersSpeed_FromRearToFront()
        {
            var rear = new VehicleCollisionBody(0f, 100f, 120f, 1.8f, 4.5f, 1500f);
            var front = new VehicleCollisionBody(0f, 101.8f, 90f, 1.8f, 4.5f, 1500f);

            var collided = VehicleCollisionResolver.TryResolve(rear, front, out var response);

            Assert.True(collided);
            Assert.True(response.First.SpeedDeltaKph < 0f);
            Assert.True(response.Second.SpeedDeltaKph > 0f);
        }

        [Fact]
        public void RearEndCollision_UsesMassWeightedExchange()
        {
            var rearLight = new VehicleCollisionBody(0f, 100f, 120f, 1.8f, 4.5f, 1000f);
            var frontHeavy = new VehicleCollisionBody(0f, 101.8f, 90f, 1.8f, 4.5f, 2000f);

            var collided = VehicleCollisionResolver.TryResolve(rearLight, frontHeavy, out var response);

            Assert.True(collided);
            Assert.True(-response.First.SpeedDeltaKph > response.Second.SpeedDeltaKph);
        }

        [Fact]
        public void SideContact_SeparatesVehiclesByLateralDirection()
        {
            var right = new VehicleCollisionBody(0.5f, 100f, 100f, 1.8f, 4.5f, 1500f);
            var left = new VehicleCollisionBody(-0.5f, 100f, 100f, 1.8f, 4.5f, 1500f);

            var collided = VehicleCollisionResolver.TryResolve(right, left, out var response);

            Assert.True(collided);
            Assert.True(response.First.BumpX > 0f);
            Assert.True(response.Second.BumpX < 0f);
        }
    }
}



