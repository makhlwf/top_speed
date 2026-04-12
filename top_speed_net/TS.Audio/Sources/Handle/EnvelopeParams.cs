namespace TS.Audio
{
    internal sealed class AudioSourceEnvelopeParams
    {
        public float CurrentGain = 1f;
        public float TargetGain = 1f;
        public int RemainingFrames;
        public int StopWhenDone;
        public int StopRequested;
    }
}
