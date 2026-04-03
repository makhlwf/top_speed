using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Localization;

namespace TopSpeed.Input
{
    internal sealed class KeyMapManager
    {
        private readonly RaceInput _input;

        public KeyMapManager(RaceInput input)
        {
            _input = input;
        }

        public IReadOnlyList<InputActionDefinition> Actions => _input.GetActionDefinitions();

        public string GetLabel(InputAction action)
        {
            return _input.GetActionLabel(action);
        }

        public Key GetKey(InputAction action)
        {
            return _input.GetKeyMapping(action);
        }

        public AxisOrButton GetAxis(InputAction action)
        {
            return _input.GetAxisMapping(action);
        }

        public void ApplyKeyMapping(InputAction action, Key key)
        {
            _input.ApplyKeyMapping(action, key);
        }

        public void ApplyAxisMapping(InputAction action, AxisOrButton axis)
        {
            _input.ApplyAxisMapping(action, axis);
        }

        public bool IsKeyInUse(Key key, InputAction ignore)
        {
            foreach (var action in Actions)
            {
                if (action.Action == ignore)
                    continue;
                if (GetKey(action.Action) == key)
                    return true;
            }
            return false;
        }

        public bool IsAxisInUse(AxisOrButton axis, InputAction ignore)
        {
            foreach (var action in Actions)
            {
                if (action.Action == ignore)
                    continue;
                if (GetAxis(action.Action) == axis)
                    return true;
            }
            return false;
        }

        public static bool IsReservedKey(Key key)
        {
            if (key >= Key.F1 && key <= Key.F8)
                return true;
            if (key == Key.F11)
                return true;
            if (key >= Key.D1 && key <= Key.D8)
                return true;
            return key == Key.LeftAlt;
        }

        public static string FormatKey(Key key)
        {
            if ((int)key <= 0)
                return "none";
            return key.ToString();
        }

        public static string FormatAxis(AxisOrButton axis)
        {
            return axis switch
            {
                AxisOrButton.AxisNone => "none",
                AxisOrButton.AxisXNeg => "X-",
                AxisOrButton.AxisXPos => "X+",
                AxisOrButton.AxisYNeg => "Y-",
                AxisOrButton.AxisYPos => "Y+",
                AxisOrButton.AxisZNeg => "Z-",
                AxisOrButton.AxisZPos => "Z+",
                AxisOrButton.AxisRxNeg => "Rx-",
                AxisOrButton.AxisRxPos => "Rx+",
                AxisOrButton.AxisRyNeg => "Ry-",
                AxisOrButton.AxisRyPos => "Ry+",
                AxisOrButton.AxisRzNeg => "Rz-",
                AxisOrButton.AxisRzPos => "Rz+",
                AxisOrButton.AxisSlider1Neg => "Slider1-",
                AxisOrButton.AxisSlider1Pos => "Slider1+",
                AxisOrButton.AxisSlider2Neg => "Slider2-",
                AxisOrButton.AxisSlider2Pos => "Slider2+",
                AxisOrButton.Button1 => "Button 1",
                AxisOrButton.Button2 => "Button 2",
                AxisOrButton.Button3 => "Button 3",
                AxisOrButton.Button4 => "Button 4",
                AxisOrButton.Button5 => "Button 5",
                AxisOrButton.Button6 => "Button 6",
                AxisOrButton.Button7 => "Button 7",
                AxisOrButton.Button8 => "Button 8",
                AxisOrButton.Button9 => "Button 9",
                AxisOrButton.Button10 => "Button 10",
                AxisOrButton.Button11 => "Button 11",
                AxisOrButton.Button12 => "Button 12",
                AxisOrButton.Button13 => "Button 13",
                AxisOrButton.Button14 => "Button 14",
                AxisOrButton.Button15 => "Button 15",
                AxisOrButton.Button16 => "Button 16",
                AxisOrButton.Pov1 => "POV 1 up",
                AxisOrButton.Pov2 => "POV 1 right",
                AxisOrButton.Pov3 => "POV 1 down",
                AxisOrButton.Pov4 => "POV 1 left",
                AxisOrButton.Pov5 => "POV 2 up",
                AxisOrButton.Pov6 => "POV 2 right",
                AxisOrButton.Pov7 => "POV 2 down",
                AxisOrButton.Pov8 => "POV 2 left",
                _ => axis.ToString()
            };
        }

        public string GetMappingInstruction(bool keyboard, InputAction action)
        {
            var label = GetLabel(action).ToLowerInvariant();
            return keyboard
                ? LocalizationService.Format(LocalizationService.Mark("Press the new key for {0}."), label)
                : LocalizationService.Format(LocalizationService.Mark("Move or press the controller control for {0}."), label);
        }
    }
}


