using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Localization;

namespace TopSpeed.Input
{
    internal sealed partial class DriveInput
    {
        internal IReadOnlyList<DriveIntentDefinition> GetIntentDefinitions()
        {
            return _intentDefinitions;
        }

        internal string GetIntentLabel(DriveIntent action)
        {
            return _intentBindings.TryGetValue(action, out var binding)
                ? binding.Label
                : LocalizationService.Mark("Action");
        }

        internal Key GetKeyMapping(DriveIntent action)
        {
            return _intentBindings.TryGetValue(action, out var binding)
                ? binding.GetKey()
                : Key.Unknown;
        }

        internal AxisOrButton GetAxisMapping(DriveIntent action)
        {
            return _intentBindings.TryGetValue(action, out var binding)
                ? binding.GetAxis()
                : AxisOrButton.AxisNone;
        }

        internal void ApplyKeyMapping(DriveIntent action, Key key)
        {
            if (_intentBindings.TryGetValue(action, out var binding))
                binding.SetKey(key);
        }

        internal void ApplyAxisMapping(DriveIntent action, AxisOrButton axis)
        {
            if (_intentBindings.TryGetValue(action, out var binding))
                binding.SetAxis(axis);
        }
    }
}



