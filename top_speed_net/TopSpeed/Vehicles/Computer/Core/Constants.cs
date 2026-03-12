namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
        private const float CallLength = 30.0f;
        private const float BaseLateralSpeed = 7.0f;
        private const float StabilitySpeedRef = 45.0f;
        private const float AutoShiftHysteresis = 0.05f;
        private const float AutoShiftCooldownSeconds = 0.15f;
        private const float AudioLateralBoost = 1.0f;
        private const float RemoteInterpRate = 28.0f;
        private const float RemoteInterpSnapDistance = 120.0f;
        private const float RemoteInterpSnapLateral = 8.0f;
    }
}
