using System.Collections.Generic;

namespace TS.Audio
{
    public sealed class AudioDiagnosticMixSnapshot
    {
        public string OutputName { get; }
        public float MasterVolume { get; }
        public float MasterVolumeDb { get; }
        public float PreLimiterPeak { get; }
        public float PreLimiterPeakDbfs { get; }
        public float PostLimiterPeak { get; }
        public float PostLimiterPeakDbfs { get; }
        public float LimiterGain { get; }
        public float LimiterGainDb { get; }
        public IReadOnlyList<AudioSourceSnapshot> ActiveSources { get; }

        public AudioDiagnosticMixSnapshot(
            string outputName,
            float masterVolume,
            float masterVolumeDb,
            float preLimiterPeak,
            float preLimiterPeakDbfs,
            float postLimiterPeak,
            float postLimiterPeakDbfs,
            float limiterGain,
            float limiterGainDb,
            IReadOnlyList<AudioSourceSnapshot> activeSources)
        {
            OutputName = outputName;
            MasterVolume = masterVolume;
            MasterVolumeDb = masterVolumeDb;
            PreLimiterPeak = preLimiterPeak;
            PreLimiterPeakDbfs = preLimiterPeakDbfs;
            PostLimiterPeak = postLimiterPeak;
            PostLimiterPeakDbfs = postLimiterPeakDbfs;
            LimiterGain = limiterGain;
            LimiterGainDb = limiterGainDb;
            ActiveSources = activeSources;
        }
    }
}
