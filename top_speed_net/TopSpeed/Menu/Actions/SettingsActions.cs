using System;
using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal interface IMenuSettingsActions
    {
        void RestoreDefaults();
        void RecalibrateScreenReaderRate();
        void SetDevice(InputDeviceMode mode);
        void UpdateSetting(Action update);
    }
}
