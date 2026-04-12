namespace TS.Audio
{
    public interface IAudioDiagnosticSink
    {
        void Write(AudioDiagnosticEvent diagnosticEvent);
    }
}
