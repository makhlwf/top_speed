namespace TS.Audio
{
    public sealed class AudioDiagnosticSnapshot
    {
        public AudioOutputSnapshot? Output { get; }
        public AudioBusSnapshot? Bus { get; }
        public AudioSourceSnapshot? Source { get; }
        public AudioDiagnosticMixSnapshot? Mix { get; }

        public AudioDiagnosticSnapshot(AudioOutputSnapshot? output = null, AudioBusSnapshot? bus = null, AudioSourceSnapshot? source = null, AudioDiagnosticMixSnapshot? mix = null)
        {
            Output = output;
            Bus = bus;
            Source = source;
            Mix = mix;
        }
    }
}
