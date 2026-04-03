using TopSpeed.Input;

namespace TopSpeed.Vehicles.Control
{
    internal sealed class FinishLockInputController : ICarController
    {
        private readonly RaceInput _input;

        public FinishLockInputController(RaceInput input)
        {
            _input = input;
        }

        public CarControlIntent ReadIntent(in CarControlContext context)
        {
            return new CarControlIntent(
                _input.GetSteering(),
                throttle: 0,
                brake: 0,
                clutch: 0,
                horn: _input.GetHorn(),
                gearUp: false,
                gearDown: false);
        }
    }
}
