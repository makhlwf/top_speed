using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        public bool GetGearUp() => IsActionTriggered(InputAction.GearUp);

        public bool GetGearDown() => IsActionTriggered(InputAction.GearDown);

        public bool GetHorn() => IsActionTriggered(InputAction.Horn);

        public bool GetRequestInfo() => IsActionTriggered(InputAction.RequestInfo);

        public bool GetCurrentGear() => IsActionTriggered(InputAction.CurrentGear);

        public bool GetCurrentLapNr() => IsActionTriggered(InputAction.CurrentLapNr);

        public bool GetCurrentRacePerc() => IsActionTriggered(InputAction.CurrentRacePerc);

        public bool GetCurrentLapPerc() => IsActionTriggered(InputAction.CurrentLapPerc);

        public bool GetCurrentRaceTime() => IsActionTriggered(InputAction.CurrentRaceTime);

        public bool GetMappedAction(InputAction action) => IsActionTriggered(action);

        public bool TryGetPlayerInfo(out int player)
        {
            if (!_allowAuxiliaryInput)
            {
                player = 0;
                return false;
            }

            if (WasPressed(_kbPlayer1)) { player = 0; return true; }
            if (WasPressed(_kbPlayer2)) { player = 1; return true; }
            if (WasPressed(_kbPlayer3)) { player = 2; return true; }
            if (WasPressed(_kbPlayer4)) { player = 3; return true; }
            if (WasPressed(_kbPlayer5)) { player = 4; return true; }
            if (WasPressed(_kbPlayer6)) { player = 5; return true; }
            if (WasPressed(_kbPlayer7)) { player = 6; return true; }
            if (WasPressed(_kbPlayer8)) { player = 7; return true; }
            player = 0;
            return false;
        }

        public bool TryGetPlayerPosition(out int player)
        {
            if (!_allowAuxiliaryInput)
            {
                player = 0;
                return false;
            }

            if (WasPressed(_kbPlayerPos1)) { player = 0; return true; }
            if (WasPressed(_kbPlayerPos2)) { player = 1; return true; }
            if (WasPressed(_kbPlayerPos3)) { player = 2; return true; }
            if (WasPressed(_kbPlayerPos4)) { player = 3; return true; }
            if (WasPressed(_kbPlayerPos5)) { player = 4; return true; }
            if (WasPressed(_kbPlayerPos6)) { player = 5; return true; }
            if (WasPressed(_kbPlayerPos7)) { player = 6; return true; }
            if (WasPressed(_kbPlayerPos8)) { player = 7; return true; }
            player = 0;
            return false;
        }

        public bool GetTrackName() => IsActionTriggered(InputAction.TrackName);

        public bool GetPlayerNumber() => _allowAuxiliaryInput && WasPressed(_kbPlayerNumber);

        public bool GetPause() => IsActionTriggered(InputAction.Pause);

        public bool GetStartEngine() => IsActionTriggered(InputAction.StartEngine);

        public bool GetFlush() => !_overlayInputBlocked && _lastState.IsDown(_kbFlush);

        public bool GetSpeedReport() => IsActionTriggered(InputAction.ReportSpeed);

        public bool GetDistanceReport() => IsActionTriggered(InputAction.ReportDistance);

        public bool GetNextPanelRequest() => WasPressed(Key.Tab) && IsCtrlDown() && !IsShiftDown();

        public bool GetPreviousPanelRequest() => WasPressed(Key.Tab) && IsCtrlDown() && IsShiftDown();

        public bool GetOpenRadioMediaRequest() => WasPressed(Key.O);

        public bool GetOpenRadioFolderRequest() => WasPressed(Key.F);

        public bool GetToggleRadioPlaybackRequest() => WasPressed(Key.P);

        public bool GetRadioVolumeUpRequest() => WasPressed(Key.Up);

        public bool GetRadioVolumeDownRequest() => WasPressed(Key.Down);

        public bool GetRadioNextTrackRequest() => WasPressed(Key.PageDown);

        public bool GetRadioPreviousTrackRequest() => WasPressed(Key.PageUp);

        public bool GetRadioToggleShuffleRequest() => WasPressed(Key.S);

        public bool GetRadioToggleLoopRequest() => WasPressed(Key.L);

        private bool WasPressed(Key key)
        {
            if (_overlayInputBlocked)
                return false;
            return _lastState.IsDown(key) && !_prevState.IsDown(key);
        }

        private bool IsActionTriggered(InputAction action)
        {
            if (_overlayInputBlocked)
                return false;
            var meta = GetActionMeta(action);
            if (!IsScopeEnabled(meta.Scope))
                return false;

            var keyboard = IsActionActiveOnKeyboard(action, meta);
            var joystick = IsActionActiveOnJoystick(action, meta);
            return keyboard || joystick;
        }

        private bool IsActionActiveOnKeyboard(InputAction action, InputActionMeta meta)
        {
            if (!UseKeyboard)
                return false;

            var key = GetKeyMapping(action);
            if (key == Key.Unknown)
                return false;

            var active = meta.KeyboardMode == TriggerMode.Hold
                ? _lastState.IsDown(key)
                : WasPressed(key);
            if (!active && meta.AllowNumpadEnterAlias && key == Key.Return)
                active = WasPressed(Key.NumberPadEnter);

            return active;
        }

        private bool IsActionActiveOnJoystick(InputAction action, InputActionMeta meta)
        {
            if (!UseJoystick)
                return false;

            var axis = GetAxisMapping(action);
            if (axis == JoystickAxisOrButton.AxisNone)
                return false;

            return meta.JoystickMode == TriggerMode.Hold
                ? GetAxis(axis) > 50
                : AxisPressed(axis);
        }

        private bool IsScopeEnabled(InputScope scope)
        {
            return scope switch
            {
                InputScope.Driving => _allowDrivingInput,
                InputScope.Auxiliary => _allowAuxiliaryInput,
                _ => false
            };
        }

        private InputActionMeta GetActionMeta(InputAction action)
        {
            if (_actionBindings.TryGetValue(action, out var binding))
                return binding.Meta;

            return new InputActionMeta(InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press);
        }
    }
}
