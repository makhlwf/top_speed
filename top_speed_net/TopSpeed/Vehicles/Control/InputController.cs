using TopSpeed.Input;

namespace TopSpeed.Vehicles.Control
{
    internal sealed class RaceInputCarController : ICarController
    {
        private readonly RaceInput _input;

        public RaceInputCarController(RaceInput input)
        {
            _input = input;
        }

        public CarControlIntent ReadIntent(in CarControlContext context)
        {
            return new CarControlIntent(
                _input.GetSteering(),
                _input.GetThrottle(),
                _input.GetBrake(),
                _input.GetClutch(),
                _input.GetHorn(),
                _input.GetGearUp(),
                _input.GetGearDown());
        }
    }
}
