using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Localization;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        internal IReadOnlyList<InputActionDefinition> GetActionDefinitions()
        {
            return _actionDefinitions;
        }

        internal string GetActionLabel(InputAction action)
        {
            return _actionBindings.TryGetValue(action, out var binding)
                ? binding.Label
                : LocalizationService.Mark("Action");
        }

        internal Key GetKeyMapping(InputAction action)
        {
            return _actionBindings.TryGetValue(action, out var binding)
                ? binding.GetKey()
                : Key.Unknown;
        }

        internal AxisOrButton GetAxisMapping(InputAction action)
        {
            return _actionBindings.TryGetValue(action, out var binding)
                ? binding.GetAxis()
                : AxisOrButton.AxisNone;
        }

        internal void ApplyKeyMapping(InputAction action, Key key)
        {
            if (_actionBindings.TryGetValue(action, out var binding))
                binding.SetKey(key);
        }

        internal void ApplyAxisMapping(InputAction action, AxisOrButton axis)
        {
            if (_actionBindings.TryGetValue(action, out var binding))
                binding.SetAxis(axis);
        }
    }
}


