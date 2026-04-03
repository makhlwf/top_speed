namespace TopSpeed.Vehicles.Control
{
    internal sealed class FixedIntentCarController : ICarController
    {
        private readonly CarControlIntent _intent;

        public FixedIntentCarController(CarControlIntent intent)
        {
            _intent = intent;
        }

        public CarControlIntent ReadIntent(in CarControlContext context)
        {
            return _intent;
        }
    }
}

