using System;
using TopSpeed.Input.Devices.Joystick;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Input.Devices.Vibration
{
    internal interface IVibrationDevice : IDisposable
    {
        bool IsAvailable { get; }
        JoystickStateSnapshot State { get; }
        bool Update();
        
        bool ForceFeedbackCapable { get; }
        void LoadEffect(VibrationEffectType type, string effectPath); // Path optional for XInput
        void PlayEffect(VibrationEffectType type, int intensity = 10000);
        void StopEffect(VibrationEffectType type);
        void Gain(VibrationEffectType type, int value);
    }
}
