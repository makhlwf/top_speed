using System;
using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal interface IMenuSettingsActions
    {
        string GetLanguageName();
        void ChangeLanguage();
        void RestoreDefaults();
        void RecalibrateScreenReaderRate();
        void CheckForUpdates();
        void SetDevice(InputDeviceMode mode);
        void UpdateSetting(Action update);
    }
}

