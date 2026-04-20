using TopSpeed.Input;

namespace TopSpeed.Vehicles.Control
{
    internal sealed class DriveInputCarController : ICarController
    {
        private readonly DriveInput _input;

        public DriveInputCarController(DriveInput input)
        {
            _input = input;
        }

        public CarControlIntent ReadIntent(in CarControlContext context)
        {
            return new CarControlIntent(
                _input.Intents.GetAxisPercent(DriveIntent.Steering),
                _input.Intents.GetAxisPercent(DriveIntent.Throttle),
                _input.Intents.GetAxisPercent(DriveIntent.Brake),
                _input.Intents.GetAxisPercent(DriveIntent.Clutch),
                _input.Intents.IsTriggered(DriveIntent.Horn),
                _input.Intents.IsTriggered(DriveIntent.GearUp),
                _input.Intents.IsTriggered(DriveIntent.GearDown));
        }
    }
}


