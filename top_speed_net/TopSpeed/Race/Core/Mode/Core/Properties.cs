namespace TopSpeed.Race
{
    internal abstract partial class RaceMode
    {
        public bool Started => _started;
        public bool ManualTransmission => _manualTransmission;
        public bool WantsExit => ExitRequested;
        public bool WantsPause => PauseRequested;
        protected bool LocalMediaLoaded => _localRadio.HasMedia;
        protected bool LocalMediaPlaying => _localRadio.HasMedia && _localRadio.DesiredPlaying;
        protected uint LocalMediaId => _localRadio.HasMedia ? _localRadio.MediaId : 0u;
    }
}

