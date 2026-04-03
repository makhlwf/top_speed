using System;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Input.Devices.Vibration
{
    internal enum VibrationFeature
    {
        EffectFilePlayback = 0
    }

    internal enum VibrationFeatureAvailability
    {
        Supported = 0,
        Unsupported = 1,
        Unavailable = 2
    }

    internal enum VibrationOperationResult
    {
        Success = 0,
        Unsupported = 1,
        Unavailable = 2,
        InvalidInput = 3,
        Failed = 4
    }

    internal interface IVibrationDevice : IDisposable
    {
        bool IsAvailable { get; }
        State State { get; }
        bool Update();
        
        bool ForceFeedbackCapable { get; }
        void PlayEffect(VibrationEffectType type, int intensity = 10000);
        void StopEffect(VibrationEffectType type);
        void Gain(VibrationEffectType type, int value);
    }

    internal interface IAdvancedVibrationDevice
    {
        VibrationFeatureAvailability GetFeatureAvailability(VibrationFeature feature);
        VibrationOperationResult LoadEffect(VibrationEffectType type, string effectPath);
    }
}

