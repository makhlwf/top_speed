using System.Collections.Generic;

namespace TopSpeed.Race.TimeTrial
{
    internal sealed class SampleStats
    {
        public int BestMs { get; set; }
        public long TotalMs { get; set; }
        public int Count { get; set; }

        public void Add(int timeMs)
        {
            if (timeMs <= 0)
                return;

            if (BestMs <= 0 || timeMs < BestMs)
                BestMs = timeMs;

            TotalMs += timeMs;
            Count++;
        }

        public int AverageMs => Count <= 0 ? 0 : (int)((TotalMs + (Count / 2L)) / Count);
    }

    internal sealed class TrackStats
    {
        public string DisplayName { get; set; } = string.Empty;
        public SampleStats Laps { get; } = new SampleStats();
        public Dictionary<int, SampleStats> Runs { get; } = new Dictionary<int, SampleStats>();
    }

    internal sealed class StatsFile
    {
        public Dictionary<string, TrackStats> Tracks { get; } = new Dictionary<string, TrackStats>();
    }

    internal sealed class StatsSnapshot
    {
        public int RunBestMs { get; set; }
        public int RunAverageMs { get; set; }
        public int RunCount { get; set; }
        public int LapBestMs { get; set; }
        public int LapAverageMs { get; set; }
        public int LapCount { get; set; }
    }
}
