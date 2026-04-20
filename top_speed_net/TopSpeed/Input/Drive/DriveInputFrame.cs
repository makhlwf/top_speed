using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal readonly struct DriveInputFrame
    {
        public DriveInputFrame(InputState keyboardState, bool hasController, State controllerState, bool controllerIsRacingWheel)
        {
            KeyboardState = keyboardState;
            HasController = hasController;
            ControllerState = controllerState;
            ControllerIsRacingWheel = controllerIsRacingWheel;
        }

        public InputState KeyboardState { get; }
        public bool HasController { get; }
        public State ControllerState { get; }
        public bool ControllerIsRacingWheel { get; }
    }
}
