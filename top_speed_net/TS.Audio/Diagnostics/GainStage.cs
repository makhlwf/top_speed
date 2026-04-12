namespace TS.Audio
{
    public sealed class AudioGainStageSnapshot
    {
        public string Name { get; }
        public float LinearGain { get; }
        public float GainDb { get; }

        public AudioGainStageSnapshot(string name, float linearGain, float gainDb)
        {
            Name = name;
            LinearGain = linearGain;
            GainDb = gainDb;
        }
    }
}
