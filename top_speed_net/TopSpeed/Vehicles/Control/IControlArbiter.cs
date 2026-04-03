namespace TopSpeed.Vehicles.Control
{
    internal interface IControlArbiter
    {
        CarControlIntent ResolveIntent(
            ICarController primaryController,
            ICarController? overrideController,
            in CarControlContext context);
    }
}

