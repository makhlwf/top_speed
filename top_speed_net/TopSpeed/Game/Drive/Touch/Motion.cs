using System;
using TopSpeed.Runtime;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private int ReadMotionSteering(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
                return ClampPercent((int)Math.Round(_driveMotionSteering * 100f));

            if (_driveMotionNeedsRecenter)
            {
                MotionSteeringRuntime.Recenter();
                _driveMotionNeedsRecenter = false;
            }

            if (MotionSteeringRuntime.TryGetSteeringAngleRadians(out var angleRadians))
            {
                var target = NormalizeMotionSteering(angleRadians);
                var blend = BlendFactor(DriveMotionSteerSmoothPerSecond, deltaSeconds);
                _driveMotionSteering = Lerp(_driveMotionSteering, target, blend);
                if (target == 0f)
                {
                    _driveMotionSteering = MoveToward(
                        _driveMotionSteering,
                        0f,
                        DriveMotionSteerCenterPerSecond * deltaSeconds);
                }

                if (_driveMotionSteering < -1f)
                    _driveMotionSteering = -1f;
                else if (_driveMotionSteering > 1f)
                    _driveMotionSteering = 1f;

                return ClampPercent((int)Math.Round(_driveMotionSteering * 100f));
            }

            // Fallback path for devices where platform rotation-vector is unavailable.
            EnsureDriveSensors();
            return ReadGyroSteering(deltaSeconds);
        }

        private void EnsureDriveSensors()
        {
            EnsureDriveGyroscope();
            EnsureDriveAccelerometer();
        }

        private static float NormalizeMotionSteering(float angleRadians)
        {
            var magnitude = Math.Abs(angleRadians);
            if (magnitude <= DriveMotionSteerDeadZoneRadians)
                return 0f;

            var span = DriveMotionSteerMaxAngleRadians - DriveMotionSteerDeadZoneRadians;
            if (span <= 0.0001f)
                return 0f;

            var normalized = (magnitude - DriveMotionSteerDeadZoneRadians) / span;
            if (normalized > 1f)
                normalized = 1f;

            return angleRadians < 0f ? -normalized : normalized;
        }
    }
}
