namespace TopSpeed.Vehicles.Control
{
    internal interface ICarController
    {
        CarControlIntent ReadIntent(in CarControlContext context);
    }
}

