using System.Collections.Generic;

namespace TS.Audio
{
    public sealed class AudioBusOptions
    {
        public string Name { get; set; } = string.Empty;
        public string? ParentName { get; set; }
        public bool UseSpeechOutput { get; set; }
        public float Volume { get; set; } = 1f;
        public bool Muted { get; set; }
        public PlaybackPolicy? Defaults { get; set; }
    }

    public sealed class AudioEngineOptions
    {
        public const string WorldBusName = "world";
        public const string UiBusName = "ui";
        public const string MusicBusName = "music";
        public const string VehiclesBusName = "vehicles";
        public const string TrackBusName = "track";
        public const string CopilotBusName = "copilot";
        public const string RadioBusName = "radio";
        public const string SpeechBusName = "speech";

        public AudioSystemConfig? SystemConfig { get; set; }
        public AudioOutputConfig? PrimaryOutput { get; set; }
        public AudioOutputConfig? SpeechOutput { get; set; }
        public PlaybackPolicy Defaults { get; } = new PlaybackPolicy();
        public IList<AudioBusOptions> Buses { get; } = new List<AudioBusOptions>();

        public AudioEngineOptions()
        {
            Buses.Add(new AudioBusOptions { Name = WorldBusName, ParentName = "main", Defaults = new PlaybackPolicy { UseHrtf = true } });
            Buses.Add(new AudioBusOptions { Name = UiBusName, ParentName = "main" });
            Buses.Add(new AudioBusOptions { Name = MusicBusName, ParentName = "main" });
            Buses.Add(new AudioBusOptions { Name = VehiclesBusName, ParentName = "main", Defaults = new PlaybackPolicy { UseHrtf = true } });
            Buses.Add(new AudioBusOptions { Name = TrackBusName, ParentName = "main" });
            Buses.Add(new AudioBusOptions { Name = CopilotBusName, ParentName = "main" });
            Buses.Add(new AudioBusOptions { Name = RadioBusName, ParentName = "main", Defaults = new PlaybackPolicy { UseHrtf = true } });
            Buses.Add(new AudioBusOptions { Name = SpeechBusName, UseSpeechOutput = true, ParentName = "main" });
        }
    }
}
