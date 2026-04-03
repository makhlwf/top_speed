using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        public void SetRequestInfo(AxisOrButton a)
        {
            _requestInfo = a;
            _settings.ControllerRequestInfo = a;
        }

        public void SetRequestInfo(Key key)
        {
            _kbRequestInfo = key;
            _settings.KeyRequestInfo = key;
        }

        public void SetCurrentGear(AxisOrButton a)
        {
            _currentGear = a;
            _settings.ControllerCurrentGear = a;
        }

        public void SetCurrentGear(Key key)
        {
            _kbCurrentGear = key;
            _settings.KeyCurrentGear = key;
        }

        public void SetCurrentLapNr(AxisOrButton a)
        {
            _currentLapNr = a;
            _settings.ControllerCurrentLapNr = a;
        }

        public void SetCurrentLapNr(Key key)
        {
            _kbCurrentLapNr = key;
            _settings.KeyCurrentLapNr = key;
        }

        public void SetCurrentRacePerc(AxisOrButton a)
        {
            _currentRacePerc = a;
            _settings.ControllerCurrentRacePerc = a;
        }

        public void SetCurrentRacePerc(Key key)
        {
            _kbCurrentRacePerc = key;
            _settings.KeyCurrentRacePerc = key;
        }

        public void SetCurrentLapPerc(AxisOrButton a)
        {
            _currentLapPerc = a;
            _settings.ControllerCurrentLapPerc = a;
        }

        public void SetCurrentLapPerc(Key key)
        {
            _kbCurrentLapPerc = key;
            _settings.KeyCurrentLapPerc = key;
        }

        public void SetCurrentRaceTime(AxisOrButton a)
        {
            _currentRaceTime = a;
            _settings.ControllerCurrentRaceTime = a;
        }

        public void SetCurrentRaceTime(Key key)
        {
            _kbCurrentRaceTime = key;
            _settings.KeyCurrentRaceTime = key;
        }

        public void SetStartEngine(AxisOrButton a)
        {
            _startEngine = a;
            _settings.ControllerStartEngine = a;
        }

        public void SetStartEngine(Key key)
        {
            _kbStartEngine = key;
            _settings.KeyStartEngine = key;
        }

        public void SetReportDistance(AxisOrButton a)
        {
            _reportDistance = a;
            _settings.ControllerReportDistance = a;
        }

        public void SetReportDistance(Key key)
        {
            _kbReportDistance = key;
            _settings.KeyReportDistance = key;
        }

        public void SetReportSpeed(AxisOrButton a)
        {
            _reportSpeed = a;
            _settings.ControllerReportSpeed = a;
        }

        public void SetReportSpeed(Key key)
        {
            _kbReportSpeed = key;
            _settings.KeyReportSpeed = key;
        }

        public void SetTrackName(AxisOrButton a)
        {
            _trackName = a;
            _settings.ControllerTrackName = a;
        }

        public void SetTrackName(Key key)
        {
            _kbTrackName = key;
            _settings.KeyTrackName = key;
        }

        public void SetPause(AxisOrButton a)
        {
            _pause = a;
            _settings.ControllerPause = a;
        }

        public void SetPause(Key key)
        {
            _kbPause = key;
            _settings.KeyPause = key;
        }
    }
}


