using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        public void SetRequestInfo(JoystickAxisOrButton a)
        {
            _requestInfo = a;
            _settings.JoystickRequestInfo = a;
        }

        public void SetRequestInfo(Key key)
        {
            _kbRequestInfo = key;
            _settings.KeyRequestInfo = key;
        }

        public void SetCurrentGear(JoystickAxisOrButton a)
        {
            _currentGear = a;
            _settings.JoystickCurrentGear = a;
        }

        public void SetCurrentGear(Key key)
        {
            _kbCurrentGear = key;
            _settings.KeyCurrentGear = key;
        }

        public void SetCurrentLapNr(JoystickAxisOrButton a)
        {
            _currentLapNr = a;
            _settings.JoystickCurrentLapNr = a;
        }

        public void SetCurrentLapNr(Key key)
        {
            _kbCurrentLapNr = key;
            _settings.KeyCurrentLapNr = key;
        }

        public void SetCurrentRacePerc(JoystickAxisOrButton a)
        {
            _currentRacePerc = a;
            _settings.JoystickCurrentRacePerc = a;
        }

        public void SetCurrentRacePerc(Key key)
        {
            _kbCurrentRacePerc = key;
            _settings.KeyCurrentRacePerc = key;
        }

        public void SetCurrentLapPerc(JoystickAxisOrButton a)
        {
            _currentLapPerc = a;
            _settings.JoystickCurrentLapPerc = a;
        }

        public void SetCurrentLapPerc(Key key)
        {
            _kbCurrentLapPerc = key;
            _settings.KeyCurrentLapPerc = key;
        }

        public void SetCurrentRaceTime(JoystickAxisOrButton a)
        {
            _currentRaceTime = a;
            _settings.JoystickCurrentRaceTime = a;
        }

        public void SetCurrentRaceTime(Key key)
        {
            _kbCurrentRaceTime = key;
            _settings.KeyCurrentRaceTime = key;
        }

        public void SetStartEngine(JoystickAxisOrButton a)
        {
            _startEngine = a;
            _settings.JoystickStartEngine = a;
        }

        public void SetStartEngine(Key key)
        {
            _kbStartEngine = key;
            _settings.KeyStartEngine = key;
        }

        public void SetReportDistance(JoystickAxisOrButton a)
        {
            _reportDistance = a;
            _settings.JoystickReportDistance = a;
        }

        public void SetReportDistance(Key key)
        {
            _kbReportDistance = key;
            _settings.KeyReportDistance = key;
        }

        public void SetReportSpeed(JoystickAxisOrButton a)
        {
            _reportSpeed = a;
            _settings.JoystickReportSpeed = a;
        }

        public void SetReportSpeed(Key key)
        {
            _kbReportSpeed = key;
            _settings.KeyReportSpeed = key;
        }

        public void SetTrackName(JoystickAxisOrButton a)
        {
            _trackName = a;
            _settings.JoystickTrackName = a;
        }

        public void SetTrackName(Key key)
        {
            _kbTrackName = key;
            _settings.KeyTrackName = key;
        }

        public void SetPause(JoystickAxisOrButton a)
        {
            _pause = a;
            _settings.JoystickPause = a;
        }

        public void SetPause(Key key)
        {
            _kbPause = key;
            _settings.KeyPause = key;
        }
    }
}
