using TopSpeed.Collision;

namespace TopSpeed.Tests;

internal static class CollisionHarness
{
    public static object BuildRepresentativeScenarios()
    {
        return new[]
        {
            Project("RearEnd", new VehicleCollisionBody(0f, 100f, 120f, 1.8f, 4.5f, 1500f), new VehicleCollisionBody(0f, 101.8f, 90f, 1.8f, 4.5f, 1500f)),
            Project("SideSwipe", new VehicleCollisionBody(0.5f, 100f, 100f, 1.8f, 4.5f, 1500f), new VehicleCollisionBody(-0.5f, 100f, 100f, 1.8f, 4.5f, 1500f)),
            Project("HeavyFront", new VehicleCollisionBody(0f, 100f, 120f, 1.8f, 4.5f, 1000f), new VehicleCollisionBody(0f, 101.8f, 90f, 1.8f, 4.5f, 2000f))
        };
    }

    private static object Project(string scenario, VehicleCollisionBody first, VehicleCollisionBody second)
    {
        var collided = VehicleCollisionResolver.TryResolve(first, second, out var response);
        return new
        {
            Scenario = scenario,
            Collided = collided,
            Response = new
            {
                response.ContactType,
                ImpactSeverity = Rounding.F(response.ImpactSeverity),
                RelativeSpeedKph = Rounding.F(response.RelativeSpeedKph),
                First = new
                {
                    BumpX = Rounding.F(response.First.BumpX),
                    BumpY = Rounding.F(response.First.BumpY),
                    SpeedDeltaKph = Rounding.F(response.First.SpeedDeltaKph)
                },
                Second = new
                {
                    BumpX = Rounding.F(response.Second.BumpX),
                    BumpY = Rounding.F(response.Second.BumpY),
                    SpeedDeltaKph = Rounding.F(response.Second.SpeedDeltaKph)
                }
            }
        };
    }
}
