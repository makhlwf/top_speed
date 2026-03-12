namespace TopSpeed.Audio
{
    internal sealed partial class AudioManager
    {
        public void Dispose()
        {
            StopUpdateThread();
            ClearCachedSources();
            _output.Dispose();
            _system.Dispose();
        }
    }
}
