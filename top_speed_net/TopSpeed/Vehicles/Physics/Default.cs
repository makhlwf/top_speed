using TopSpeed.Vehicles.Control;

namespace TopSpeed.Vehicles.Physics
{
    internal sealed class Default : IModel
    {
        public void Step(Car car, float elapsed, in CarControlIntent intent)
        {
            car.RunDynamics(elapsed, intent);
        }
    }
}

