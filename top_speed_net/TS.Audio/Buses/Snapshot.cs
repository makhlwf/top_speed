using System.Collections.Generic;

namespace TS.Audio
{
    public sealed class AudioBusSnapshot
    {
        public string Name { get; }
        public string? ParentName { get; }
        public float LocalVolume { get; }
        public float LocalVolumeDb { get; }
        public float EffectiveVolume { get; }
        public float EffectiveVolumeDb { get; }
        public bool Muted { get; }
        public int ChildCount { get; }
        public bool EffectsEnabled { get; }
        public int EffectCount { get; }
        public IReadOnlyList<string> Effects { get; }
        public IReadOnlyList<AudioGainStageSnapshot> GainStages { get; }

        public AudioBusSnapshot(string name, string? parentName, float localVolume, float localVolumeDb, float effectiveVolume, float effectiveVolumeDb, bool muted, int childCount, bool effectsEnabled, int effectCount, IReadOnlyList<string> effects, IReadOnlyList<AudioGainStageSnapshot> gainStages)
        {
            Name = name;
            ParentName = parentName;
            LocalVolume = localVolume;
            LocalVolumeDb = localVolumeDb;
            EffectiveVolume = effectiveVolume;
            EffectiveVolumeDb = effectiveVolumeDb;
            Muted = muted;
            ChildCount = childCount;
            EffectsEnabled = effectsEnabled;
            EffectCount = effectCount;
            Effects = effects;
            GainStages = gainStages;
        }
    }
}
