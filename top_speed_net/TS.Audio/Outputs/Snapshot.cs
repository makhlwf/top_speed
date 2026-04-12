using System.Collections.Generic;

namespace TS.Audio
{
    public sealed class AudioOutputSnapshot
    {
        public string Name { get; }
        public int SampleRate { get; }
        public int Channels { get; }
        public float MasterVolume { get; }
        public float MasterVolumeDb { get; }
        public float LastPreLimiterPeak { get; }
        public float LastPreLimiterPeakDbfs { get; }
        public float LastPostLimiterPeak { get; }
        public float LastPostLimiterPeakDbfs { get; }
        public bool HrtfActive { get; }
        public int SourceCount { get; }
        public int StreamCount { get; }
        public int RetiredSourceCount { get; }
        public int RetiredEffectCount { get; }
        public IReadOnlyList<AudioBusSnapshot> Buses { get; }
        public IReadOnlyList<AudioSourceSnapshot> Sources { get; }

        public AudioOutputSnapshot(string name, int sampleRate, int channels, float masterVolume, float masterVolumeDb, float lastPreLimiterPeak, float lastPreLimiterPeakDbfs, float lastPostLimiterPeak, float lastPostLimiterPeakDbfs, bool hrtfActive, int sourceCount, int streamCount, int retiredSourceCount, int retiredEffectCount, IReadOnlyList<AudioBusSnapshot> buses, IReadOnlyList<AudioSourceSnapshot> sources)
        {
            Name = name;
            SampleRate = sampleRate;
            Channels = channels;
            MasterVolume = masterVolume;
            MasterVolumeDb = masterVolumeDb;
            LastPreLimiterPeak = lastPreLimiterPeak;
            LastPreLimiterPeakDbfs = lastPreLimiterPeakDbfs;
            LastPostLimiterPeak = lastPostLimiterPeak;
            LastPostLimiterPeakDbfs = lastPostLimiterPeakDbfs;
            HrtfActive = hrtfActive;
            SourceCount = sourceCount;
            StreamCount = streamCount;
            RetiredSourceCount = retiredSourceCount;
            RetiredEffectCount = retiredEffectCount;
            Buses = buses;
            Sources = sources;
        }
    }
}
