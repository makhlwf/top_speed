using System;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        public void Initialize()
        {
            _left = AxisOrButton.AxisNone;
            _right = AxisOrButton.AxisNone;
            _throttle = AxisOrButton.AxisNone;
            _brake = AxisOrButton.AxisNone;
            _clutch = AxisOrButton.AxisNone;
            _gearUp = AxisOrButton.AxisNone;
            _gearDown = AxisOrButton.AxisNone;
            _horn = AxisOrButton.AxisNone;
            _requestInfo = AxisOrButton.AxisNone;
            _currentGear = AxisOrButton.AxisNone;
            _currentLapNr = AxisOrButton.AxisNone;
            _currentRacePerc = AxisOrButton.AxisNone;
            _currentLapPerc = AxisOrButton.AxisNone;
            _currentRaceTime = AxisOrButton.AxisNone;
            _startEngine = AxisOrButton.AxisNone;
            _reportDistance = AxisOrButton.AxisNone;
            _reportSpeed = AxisOrButton.AxisNone;
            _trackName = AxisOrButton.AxisNone;
            _pause = AxisOrButton.AxisNone;
            ReadFromSettings();
            _allowDrivingInput = true;
            _allowAuxiliaryInput = true;
            _overlayInputBlocked = false;
            _controllerIsRacingWheel = false;
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
            _left = _settings.ControllerLeft;
            _right = _settings.ControllerRight;
            _throttle = _settings.ControllerThrottle;
            _brake = _settings.ControllerBrake;
            _clutch = _settings.ControllerClutch;
            _gearUp = _settings.ControllerGearUp;
            _gearDown = _settings.ControllerGearDown;
            _horn = _settings.ControllerHorn;
            _requestInfo = _settings.ControllerRequestInfo;
            _currentGear = _settings.ControllerCurrentGear;
            _currentLapNr = _settings.ControllerCurrentLapNr;
            _currentRacePerc = _settings.ControllerCurrentRacePerc;
            _currentLapPerc = _settings.ControllerCurrentLapPerc;
            _currentRaceTime = _settings.ControllerCurrentRaceTime;
            _startEngine = _settings.ControllerStartEngine;
            _reportDistance = _settings.ControllerReportDistance;
            _reportSpeed = _settings.ControllerReportSpeed;
            _trackName = _settings.ControllerTrackName;
            _pause = _settings.ControllerPause;
            _center = _settings.ControllerCenter;
            _hasCenter = true;
            _kbLeft = _settings.KeyLeft;
            _kbRight = _settings.KeyRight;
            _kbThrottle = _settings.KeyThrottle;
            _kbBrake = _settings.KeyBrake;
            _kbClutch = _settings.KeyClutch;
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


