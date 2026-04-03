using System;

namespace TopSpeed.Race
{
    internal enum RaceResultMode
    {
        Race = 0,
        TimeTrial = 1
    }

    internal sealed class RaceResultSummary
    {
        public RaceResultMode Mode { get; set; } = RaceResultMode.Race;
        public bool IsMultiplayer { get; set; }
        public int LocalPosition { get; set; }
        public bool TimeTrialBeatRecord { get; set; }
        public int TimeTrialCurrentTimeMs { get; set; }
        public int TimeTrialPreviousBestTimeMs { get; set; }
        public RaceResultEntry[] Entries { get; set; } = Array.Empty<RaceResultEntry>();
    }

    internal sealed class RaceResultEntry
    {
        public string Name { get; set; } = string.Empty;
        public int Position { get; set; }
        public int TimeMs { get; set; }
        public bool IsLocalPlayer { get; set; }
    }
}

