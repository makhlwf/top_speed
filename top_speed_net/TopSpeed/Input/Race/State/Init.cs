using System;
using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        public void Initialize()
        {
            _left = JoystickAxisOrButton.AxisNone;
            _right = JoystickAxisOrButton.AxisNone;
            _throttle = JoystickAxisOrButton.AxisNone;
            _brake = JoystickAxisOrButton.AxisNone;
            _gearUp = JoystickAxisOrButton.AxisNone;
            _gearDown = JoystickAxisOrButton.AxisNone;
            _horn = JoystickAxisOrButton.AxisNone;
            _requestInfo = JoystickAxisOrButton.AxisNone;
            _currentGear = JoystickAxisOrButton.AxisNone;
            _currentLapNr = JoystickAxisOrButton.AxisNone;
            _currentRacePerc = JoystickAxisOrButton.AxisNone;
            _currentLapPerc = JoystickAxisOrButton.AxisNone;
            _currentRaceTime = JoystickAxisOrButton.AxisNone;
            _startEngine = JoystickAxisOrButton.AxisNone;
            _reportDistance = JoystickAxisOrButton.AxisNone;
            _reportSpeed = JoystickAxisOrButton.AxisNone;
            _trackName = JoystickAxisOrButton.AxisNone;
            _pause = JoystickAxisOrButton.AxisNone;
            ReadFromSettings();
            _allowDrivingInput = true;
            _allowAuxiliaryInput = true;
            _overlayInputBlocked = false;
            _joystickIsRacingWheel = false;
            _hasPedalBaseline = false;
            _pedalBaseline = default;

            _kbPlayer1 = Key.F1;
            _kbPlayer2 = Key.F2;
            _kbPlayer3 = Key.F3;
            _kbPlayer4 = Key.F4;
            _kbPlayer5 = Key.F5;
            _kbPlayer6 = Key.F6;
            _kbPlayer7 = Key.F7;
            _kbPlayer8 = Key.F8;
            _kbPlayerNumber = Key.F11;
            _kbPlayerPos1 = Key.D1;
            _kbPlayerPos2 = Key.D2;
            _kbPlayerPos3 = Key.D3;
            _kbPlayerPos4 = Key.D4;
            _kbPlayerPos5 = Key.D5;
            _kbPlayerPos6 = Key.D6;
            _kbPlayerPos7 = Key.D7;
            _kbPlayerPos8 = Key.D8;
            _kbFlush = Key.LeftAlt;
        }

        private void ReadFromSettings()
        {
            _left = _settings.JoystickLeft;
            _right = _settings.JoystickRight;
            _throttle = _settings.JoystickThrottle;
            _brake = _settings.JoystickBrake;
            _gearUp = _settings.JoystickGearUp;
            _gearDown = _settings.JoystickGearDown;
            _horn = _settings.JoystickHorn;
            _requestInfo = _settings.JoystickRequestInfo;
            _currentGear = _settings.JoystickCurrentGear;
            _currentLapNr = _settings.JoystickCurrentLapNr;
            _currentRacePerc = _settings.JoystickCurrentRacePerc;
            _currentLapPerc = _settings.JoystickCurrentLapPerc;
            _currentRaceTime = _settings.JoystickCurrentRaceTime;
            _startEngine = _settings.JoystickStartEngine;
            _reportDistance = _settings.JoystickReportDistance;
            _reportSpeed = _settings.JoystickReportSpeed;
            _trackName = _settings.JoystickTrackName;
            _pause = _settings.JoystickPause;
            _center = _settings.JoystickCenter;
            _hasCenter = true;
            _kbLeft = _settings.KeyLeft;
            _kbRight = _settings.KeyRight;
            _kbThrottle = _settings.KeyThrottle;
            _kbBrake = _settings.KeyBrake;
            _kbGearUp = _settings.KeyGearUp;
            _kbGearDown = _settings.KeyGearDown;
            _kbHorn = _settings.KeyHorn;
            _kbRequestInfo = _settings.KeyRequestInfo;
            _kbCurrentGear = _settings.KeyCurrentGear;
            _kbCurrentLapNr = _settings.KeyCurrentLapNr;
            _kbCurrentRacePerc = _settings.KeyCurrentRacePerc;
            _kbCurrentLapPerc = _settings.KeyCurrentLapPerc;
            _kbCurrentRaceTime = _settings.KeyCurrentRaceTime;
            _kbStartEngine = _settings.KeyStartEngine;
            _kbReportDistance = _settings.KeyReportDistance;
            _kbReportSpeed = _settings.KeyReportSpeed;
            _kbTrackName = _settings.KeyTrackName;
            _kbPause = _settings.KeyPause;
            _deviceMode = _settings.DeviceMode;
        }
    }
}
