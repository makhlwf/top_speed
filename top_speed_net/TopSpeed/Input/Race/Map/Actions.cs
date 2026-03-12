using System.Collections.Generic;
using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Joystick;

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
                : "Action";
        }

        internal Key GetKeyMapping(InputAction action)
        {
            return _actionBindings.TryGetValue(action, out var binding)
                ? binding.GetKey()
                : Key.Unknown;
        }

        internal JoystickAxisOrButton GetAxisMapping(InputAction action)
        {
            return _actionBindings.TryGetValue(action, out var binding)
                ? binding.GetAxis()
                : JoystickAxisOrButton.AxisNone;
        }

        internal void ApplyKeyMapping(InputAction action, Key key)
        {
            if (_actionBindings.TryGetValue(action, out var binding))
                binding.SetKey(key);
        }

        internal void ApplyAxisMapping(InputAction action, JoystickAxisOrButton axis)
        {
            if (_actionBindings.TryGetValue(action, out var binding))
                binding.SetAxis(axis);
        }
    }
}
