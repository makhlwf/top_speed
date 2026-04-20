using System;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Views;

namespace TopSpeed.Android;

internal sealed class AndroidMotionSteeringSource : Java.Lang.Object, ISensorEventListener, global::TopSpeed.Runtime.IMotionSteeringSource
{
    private readonly object _sync = new object();
    private readonly SensorManager? _sensorManager;
    private readonly Sensor? _sensor;
    private readonly Activity _activity;
    private float[] _rotationVector = new float[5];
    private readonly float[] _rotation = new float[9];
    private readonly float[] _remapped = new float[9];
    private readonly float[] _orientation = new float[3];
    private bool _hasReading;
    private bool _hasNeutral;
    private bool _disposed;
    private float _neutralRoll;
    private float _currentRoll;

    public AndroidMotionSteeringSource(Activity activity)
    {
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
        _sensorManager = activity.GetSystemService(Context.SensorService) as SensorManager;
        _sensor = _sensorManager?.GetDefaultSensor(SensorType.GameRotationVector)
            ?? _sensorManager?.GetDefaultSensor(SensorType.RotationVector);

        if (_sensor != null)
        {
            _sensorManager?.RegisterListener(
                this,
                _sensor,
                SensorDelay.Game);
        }
    }

    public bool IsAvailable => !_disposed && _sensorManager != null && _sensor != null;

    public void Recenter()
    {
        lock (_sync)
        {
            _hasNeutral = false;
            _hasReading = false;
            _currentRoll = 0f;
        }
    }

    public bool TryGetSteeringAngleRadians(out float angleRadians)
    {
        lock (_sync)
        {
            if (!_hasReading)
            {
                angleRadians = 0f;
                return false;
            }

            angleRadians = _currentRoll;
            return true;
        }
    }

    public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy)
    {
        // Not used.
    }

    public void OnSensorChanged(SensorEvent? value)
    {
        if (_disposed || value == null || _sensor == null || value.Sensor == null)
            return;

        if (value.Sensor.Type != _sensor.Type)
            return;

        var values = value.Values;
        if (values == null || values.Count < 3)
            return;

        if (_rotationVector.Length != values.Count)
            _rotationVector = new float[values.Count];
        for (var i = 0; i < values.Count; i++)
            _rotationVector[i] = values[i];

        SensorManager.GetRotationMatrixFromVector(_rotation, _rotationVector);
        var remapped = RemapToDisplay(_rotation, _remapped, GetDisplayRotation());
        SensorManager.GetOrientation(remapped, _orientation);

        // Orientation uses radians. roll is index 2 after display remap.
        var roll = _orientation[2];
        lock (_sync)
        {
            if (!_hasNeutral)
            {
                _neutralRoll = roll;
                _hasNeutral = true;
            }

            _currentRoll = WrapAngle(roll - _neutralRoll);
            _hasReading = true;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _sensorManager?.UnregisterListener(this);
    }

    private SurfaceOrientation GetDisplayRotation()
    {
        try
        {
            return _activity.Display?.Rotation ?? SurfaceOrientation.Rotation0;
        }
        catch
        {
            return SurfaceOrientation.Rotation0;
        }
    }

    private static float[] RemapToDisplay(float[] input, float[] output, SurfaceOrientation rotation)
    {
        switch (rotation)
        {
            case SurfaceOrientation.Rotation90:
                SensorManager.RemapCoordinateSystem(input, global::Android.Hardware.Axis.Y, global::Android.Hardware.Axis.MinusX, output);
                break;

            case SurfaceOrientation.Rotation180:
                SensorManager.RemapCoordinateSystem(input, global::Android.Hardware.Axis.MinusX, global::Android.Hardware.Axis.MinusY, output);
                break;

            case SurfaceOrientation.Rotation270:
                SensorManager.RemapCoordinateSystem(input, global::Android.Hardware.Axis.MinusY, global::Android.Hardware.Axis.X, output);
                break;

            default:
                Array.Copy(input, output, input.Length);
                break;
        }

        return output;
    }

    private static float WrapAngle(float value)
    {
        var twoPi = 2f * (float)Math.PI;
        while (value > Math.PI)
            value -= twoPi;
        while (value < -Math.PI)
            value += twoPi;
        return value;
    }
}
