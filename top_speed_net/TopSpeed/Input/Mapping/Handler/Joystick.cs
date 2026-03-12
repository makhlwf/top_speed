using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class InputMappingHandler
    {
        private void TryCaptureJoystickMapping()
        {
            if (!_input.TryGetJoystickState(out var state))
            {
                _mappingActive = false;
                _speech.Speak("No joystick detected.");
                return;
            }

            if (!_mappingHasPrevJoystick)
            {
                _mappingPrevJoystick = state;
                _mappingHasPrevJoystick = true;
                return;
            }

            var axis = FindTriggeredAxis(state, _mappingPrevJoystick);
            _mappingPrevJoystick = state;
            if (axis == JoystickAxisOrButton.AxisNone)
                return;
            if (_raceInput.KeyMap.IsAxisInUse(axis, _mappingAction))
            {
                _speech.Speak("That control is already in use.");
                return;
            }

            _raceInput.KeyMap.ApplyAxisMapping(_mappingAction, axis);
            _saveSettings();
            _mappingActive = false;
            var label = _raceInput.KeyMap.GetLabel(_mappingAction);
            _speech.Speak($"{label} set to {KeyMapManager.FormatAxis(axis)}.");
        }

        private JoystickAxisOrButton FindTriggeredAxis(JoystickStateSnapshot current, JoystickStateSnapshot previous)
        {
            for (var i = (int)JoystickAxisOrButton.AxisXNeg; i <= (int)JoystickAxisOrButton.Pov8; i++)
            {
                var axis = (JoystickAxisOrButton)i;
                if (IsAxisActive(axis, current) && !IsAxisActive(axis, previous))
                    return axis;
            }
            return JoystickAxisOrButton.AxisNone;
        }

        private bool IsAxisActive(JoystickAxisOrButton axis, JoystickStateSnapshot state)
        {
            var center = _settings.JoystickCenter;
            const int threshold = 50;
            switch (axis)
            {
                case JoystickAxisOrButton.AxisXNeg:
                    return state.X < center.X - threshold;
                case JoystickAxisOrButton.AxisXPos:
                    return state.X > center.X + threshold;
                case JoystickAxisOrButton.AxisYNeg:
                    return state.Y < center.Y - threshold;
                case JoystickAxisOrButton.AxisYPos:
                    return state.Y > center.Y + threshold;
                case JoystickAxisOrButton.AxisZNeg:
                    return state.Z < center.Z - threshold;
                case JoystickAxisOrButton.AxisZPos:
                    return state.Z > center.Z + threshold;
                case JoystickAxisOrButton.AxisRxNeg:
                    return state.Rx < center.Rx - threshold;
                case JoystickAxisOrButton.AxisRxPos:
                    return state.Rx > center.Rx + threshold;
                case JoystickAxisOrButton.AxisRyNeg:
                    return state.Ry < center.Ry - threshold;
                case JoystickAxisOrButton.AxisRyPos:
                    return state.Ry > center.Ry + threshold;
                case JoystickAxisOrButton.AxisRzNeg:
                    return state.Rz < center.Rz - threshold;
                case JoystickAxisOrButton.AxisRzPos:
                    return state.Rz > center.Rz + threshold;
                case JoystickAxisOrButton.AxisSlider1Neg:
                    return state.Slider1 < center.Slider1 - threshold;
                case JoystickAxisOrButton.AxisSlider1Pos:
                    return state.Slider1 > center.Slider1 + threshold;
                case JoystickAxisOrButton.AxisSlider2Neg:
                    return state.Slider2 < center.Slider2 - threshold;
                case JoystickAxisOrButton.AxisSlider2Pos:
                    return state.Slider2 > center.Slider2 + threshold;
                case JoystickAxisOrButton.Button1:
                    return state.B1;
                case JoystickAxisOrButton.Button2:
                    return state.B2;
                case JoystickAxisOrButton.Button3:
                    return state.B3;
                case JoystickAxisOrButton.Button4:
                    return state.B4;
                case JoystickAxisOrButton.Button5:
                    return state.B5;
                case JoystickAxisOrButton.Button6:
                    return state.B6;
                case JoystickAxisOrButton.Button7:
                    return state.B7;
                case JoystickAxisOrButton.Button8:
                    return state.B8;
                case JoystickAxisOrButton.Button9:
                    return state.B9;
                case JoystickAxisOrButton.Button10:
                    return state.B10;
                case JoystickAxisOrButton.Button11:
                    return state.B11;
                case JoystickAxisOrButton.Button12:
                    return state.B12;
                case JoystickAxisOrButton.Button13:
                    return state.B13;
                case JoystickAxisOrButton.Button14:
                    return state.B14;
                case JoystickAxisOrButton.Button15:
                    return state.B15;
                case JoystickAxisOrButton.Button16:
                    return state.B16;
                case JoystickAxisOrButton.Pov1:
                    return state.Pov1;
                case JoystickAxisOrButton.Pov2:
                    return state.Pov2;
                case JoystickAxisOrButton.Pov3:
                    return state.Pov3;
                case JoystickAxisOrButton.Pov4:
                    return state.Pov4;
                case JoystickAxisOrButton.Pov5:
                    return state.Pov5;
                case JoystickAxisOrButton.Pov6:
                    return state.Pov6;
                case JoystickAxisOrButton.Pov7:
                    return state.Pov7;
                case JoystickAxisOrButton.Pov8:
                    return state.Pov8;
                default:
                    return false;
            }
        }
    }
}
