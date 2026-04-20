namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private const string DriveTouchInfoLeftZoneId = "drive_info_left";
        private const string DriveTouchInfoRightZoneId = "drive_info_right";
        private const string DriveTouchVehicleZoneId = "drive_vehicle";
        private const float DriveTouchSplitY = 0.5f;
        private const float DriveTouchInfoSplitX = 0.5f;
        private const float DriveTouchAxisTravel = 0.22f;

        private const float DriveMotionSteerDeadZoneRadians = 0.03f;
        private const float DriveMotionSteerMaxAngleRadians = 0.42f;
        private const float DriveMotionSteerSmoothPerSecond = 14.0f;
        private const float DriveMotionSteerCenterPerSecond = 8.0f;

        private const float DriveGyroRateDeadZone = 0.02f;
        private const float DriveGyroRateToSteering = 4.2f;
        private const float DriveGyroBiasTrackPerSecond = 0.45f;
        private const float DriveGyroBiasTrackMaxRate = 0.25f;
        private const float DriveGyroAccelFilterPerSecond = 12.0f;
        private const float DriveGyroBlendPerSecond = 8.0f;
        private const float DriveGyroSteeringMaxAngleRadians = 0.52f;
        private const float DriveGyroSteeringDirection = 1.0f;
    }
}
