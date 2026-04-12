using System.Collections.Generic;

namespace TS.Audio
{
    public sealed class AudioSourceSnapshot
    {
        public int SourceId { get; }
        public string BusName { get; }
        public bool IsPlaying { get; }
        public bool IsSpatialized { get; }
        public bool UsesSteamAudio { get; }
        public int InputChannels { get; }
        public int InputSampleRate { get; }
        public bool Looping { get; }
        public float Volume { get; }
        public float VolumeDb { get; }
        public float Pitch { get; }
        public float Pan { get; }
        public float BusEffectiveVolume { get; }
        public float BusEffectiveVolumeDb { get; }
        public float EstimatedMixVolume { get; }
        public float EstimatedMixVolumeDb { get; }
        public IReadOnlyList<AudioGainStageSnapshot> BusGainStages { get; }
        public float LengthSeconds { get; }

        public AudioSourceSnapshot(int sourceId, string busName, bool isPlaying, bool isSpatialized, bool usesSteamAudio, int inputChannels, int inputSampleRate, bool looping, float volume, float volumeDb, float pitch, float pan, float busEffectiveVolume, float busEffectiveVolumeDb, float estimatedMixVolume, float estimatedMixVolumeDb, IReadOnlyList<AudioGainStageSnapshot> busGainStages, float lengthSeconds)
        {
            SourceId = sourceId;
            BusName = busName;
            IsPlaying = isPlaying;
            IsSpatialized = isSpatialized;
            UsesSteamAudio = usesSteamAudio;
            InputChannels = inputChannels;
            InputSampleRate = inputSampleRate;
            Looping = looping;
            Volume = volume;
            VolumeDb = volumeDb;
            Pitch = pitch;
            Pan = pan;
            BusEffectiveVolume = busEffectiveVolume;
            BusEffectiveVolumeDb = busEffectiveVolumeDb;
            EstimatedMixVolume = estimatedMixVolume;
            EstimatedMixVolumeDb = estimatedMixVolumeDb;
            BusGainStages = busGainStages;
            LengthSeconds = lengthSeconds;
        }
    }
}
