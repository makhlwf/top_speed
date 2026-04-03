using TopSpeed.Vehicles.Control;

namespace TopSpeed.Vehicles.Physics
{
    internal interface IModel
    {
        void Step(Car car, float elapsed, in CarControlIntent intent);
    }
}

