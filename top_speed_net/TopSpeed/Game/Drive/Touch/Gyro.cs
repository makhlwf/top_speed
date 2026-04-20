using System;
using TS.Sdl.Input;
using SdlWindow = TS.Sdl.Video.Window;
using TS.Sdl.Video;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void EnsureDriveGyroscope()
        {
            if (_driveGyroscopeSensor != null && _driveGyroscopeSensor.IsOpen)
                return;
            if (_driveGyroscopeAttempted)
                return;

            _driveGyroscopeAttempted = true;
            var ids = Sensor.GetIds();
            for (var i = 0; i < ids.Length; i++)
            {
                var type = Sensor.GetTypeForId(ids[i]);
                if (type != SensorType.Gyroscope &&
                    type != SensorType.GyroscopeLeft &&
                    type != SensorType.GyroscopeRight)
                {
                    continue;
                }

                var sensor = Sensor.Open(ids[i]);
                if (sensor == null || !sensor.IsOpen)
                    continue;

                _driveGyroscopeSensor = sensor;
                return;
            }
        }

        private void EnsureDriveAccelerometer()
        {
            if (_driveAccelerometerSensor != null && _driveAccelerometerSensor.IsOpen)
                return;
            if (_driveAccelerometerAttempted)
                return;

            _driveAccelerometerAttempted = true;
            var ids = Sensor.GetIds();
            for (var i = 0; i < ids.Length; i++)
            {
                var type = Sensor.GetTypeForId(ids[i]);
                if (type != SensorType.Accelerometer &&
                    type != SensorType.AccelerometerLeft &&
                    type != SensorType.AccelerometerRight)
                {
                    continue;
                }

                var sensor = Sensor.Open(ids[i]);
                if (sensor == null || !sensor.IsOpen)
                    continue;

                _driveAccelerometerSensor = sensor;
                return;
            }
        }

        private int ReadGyroSteering(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
                return ClampPercent((int)Math.Round(_driveGyroSteering * 100f));

            var gyroscope = _driveGyroscopeSensor;
            var accelerometer = _driveAccelerometerSensor;
            if (gyroscope == null || !gyroscope.IsOpen)
                return ClampPercent((int)Math.Round(_driveGyroSteering * 100f));
            if (accelerometer == null || !accelerometer.IsOpen)
                return ClampPercent((int)Math.Round(_driveGyroSteering * 100f));

            Sensor.Update();
            if (!gyroscope.TryGetData(_driveGyroscopeData) || !accelerometer.TryGetData(_driveAccelerometerData))
                return ClampPercent((int)Math.Round(_driveGyroSteering * 100f));

            RefreshDriveDisplayOrientation();

            var accelBlend = BlendFactor(DriveGyroAccelFilterPerSecond, deltaSeconds);
            _driveGyroGravityX = Lerp(_driveGyroGravityX, _driveAccelerometerData[0], accelBlend);
            _driveGyroGravityY = Lerp(_driveGyroGravityY, _driveAccelerometerData[1], accelBlend);
            _driveGyroGravityZ = Lerp(_driveGyroGravityZ, _driveAccelerometerData[2], accelBlend);

            var gravityLength = Math.Sqrt(
                (_driveGyroGravityX * _driveGyroGravityX) +
                (_driveGyroGravityY * _driveGyroGravityY) +
                (_driveGyroGravityZ * _driveGyroGravityZ));
            if (gravityLength <= 0.0001)
                return ClampPercent((int)Math.Round(_driveGyroSteering * 100f));

            var invGravityLength = (float)(1.0 / gravityLength);
            var nx = _driveGyroGravityX * invGravityLength;
            var ny = _driveGyroGravityY * invGravityLength;
            RemapToDisplayAxes(nx, ny, _driveDisplayOrientation, out var displayX, out var displayY);
            if (!_driveGyroOrientationReady)
            {
                _driveGyroNeutralAngle = ComputeDisplayAngle(displayX, displayY);
                _driveGyroOrientationReady = true;
            }

            // Angle model uses atan2(displayX, displayY); its derivative is -gyro.z.
            var rawRate = -_driveGyroscopeData[2];
            if (Math.Abs(rawRate) <= DriveGyroBiasTrackMaxRate)
            {
                _driveGyroBias = MoveToward(
                    _driveGyroBias,
                    rawRate,
                    DriveGyroBiasTrackPerSecond * deltaSeconds);
            }

            var correctedRate = (rawRate - _driveGyroBias) * DriveGyroSteeringDirection;
            if (Math.Abs(correctedRate) < DriveGyroRateDeadZone)
                correctedRate = 0f;

            _driveGyroRate = correctedRate;
            var gyroIntegrated = _driveGyroSteering + (_driveGyroRate * DriveGyroRateToSteering * deltaSeconds);

            var accelerometerAngle = ComputeDisplayAngle(displayX, displayY);
            var relativeAccelerometerAngle = WrapAngle(accelerometerAngle - _driveGyroNeutralAngle) * DriveGyroSteeringDirection;

            var accelToSteering = relativeAccelerometerAngle / DriveGyroSteeringMaxAngleRadians;
            if (accelToSteering < -1f)
                accelToSteering = -1f;
            else if (accelToSteering > 1f)
                accelToSteering = 1f;

            var fusionBlend = BlendFactor(DriveGyroBlendPerSecond, deltaSeconds);
            _driveGyroSteering = Lerp(gyroIntegrated, accelToSteering, fusionBlend);

            if (_driveGyroSteering < -1f)
                _driveGyroSteering = -1f;
            else if (_driveGyroSteering > 1f)
                _driveGyroSteering = 1f;

            return ClampPercent((int)Math.Round(_driveGyroSteering * 100f));
        }

        private void RefreshDriveDisplayOrientation()
        {
            var orientation = ResolveDriveDisplayOrientation();
            if (orientation == DisplayOrientation.Unknown)
                return;

            if (_driveDisplayOrientation == orientation)
                return;

            _driveDisplayOrientation = orientation;
            _driveGyroOrientationReady = false;
            _driveGyroSteering = 0f;
            _driveGyroBias = 0f;
            _driveGyroRate = 0f;
        }

        private DisplayOrientation ResolveDriveDisplayOrientation()
        {
            var windowHandle = _window.NativeHandle;
            var displayId = windowHandle != IntPtr.Zero
                ? SdlWindow.GetDisplayForWindow(windowHandle)
                : 0u;
            if (displayId == 0)
                displayId = SdlWindow.GetPrimaryDisplay();
            if (displayId == 0)
                return DisplayOrientation.Unknown;
            return SdlWindow.GetCurrentDisplayOrientation(displayId);
        }

        private static void RemapToDisplayAxes(
            float x,
            float y,
            DisplayOrientation orientation,
            out float displayX,
            out float displayY)
        {
            switch (orientation)
            {
                case DisplayOrientation.Landscape:
                    displayX = y;
                    displayY = -x;
                    return;

                case DisplayOrientation.LandscapeFlipped:
                    displayX = -y;
                    displayY = x;
                    return;

                case DisplayOrientation.PortraitFlipped:
                    displayX = -x;
                    displayY = -y;
                    return;

                default:
                    displayX = x;
                    displayY = y;
                    return;
            }
        }

        private static float ComputeDisplayAngle(float displayX, float displayY)
        {
            return (float)Math.Atan2(displayX, displayY);
        }

        private static float WrapAngle(float value)
        {
            const float pi = (float)Math.PI;
            while (value > pi)
                value -= 2f * pi;
            while (value < -pi)
                value += 2f * pi;
            return value;
        }
    }
}
