using TS.Sdl.Input;
using TS.Sdl.Video;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private bool _isAndroidPlatform;
        private bool _driveTouchZonesApplied;
        private bool _driveTouchExitRequested;

        private bool _driveMotionEnabled;
        private bool _driveMotionNeedsRecenter = true;
        private float _driveMotionSteering;

        private bool _driveGyroscopeAttempted;
        private bool _driveAccelerometerAttempted;
        private Sensor? _driveGyroscopeSensor;
        private Sensor? _driveAccelerometerSensor;
        private readonly float[] _driveGyroscopeData = new float[6];
        private readonly float[] _driveAccelerometerData = new float[6];
        private float _driveGyroSteering;
        private float _driveGyroBias;
        private float _driveGyroRate;
        private float _driveGyroGravityX;
        private float _driveGyroGravityY;
        private float _driveGyroGravityZ;
        private float _driveGyroNeutralAngle;
        private bool _driveGyroOrientationReady;
        private DisplayOrientation _driveDisplayOrientation = DisplayOrientation.Unknown;
    }
}
