using TopSpeed.Vehicles.Control;

namespace TopSpeed.Vehicles.Core
{
    internal interface ICarModifier
    {
        CarControlIntent ApplyIntent(in CarControlContext context, in CarControlIntent intent);
    }
}

