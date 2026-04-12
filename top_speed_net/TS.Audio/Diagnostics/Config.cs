namespace TS.Audio
{
    public sealed class AudioDiagnosticConfig
    {
        public bool Enabled { get; set; }
        public int HistoryCapacity { get; set; } = 512;
        public AudioDiagnosticFilter Filter { get; set; } = new AudioDiagnosticFilter();

        public AudioDiagnosticConfig Clone()
        {
            return new AudioDiagnosticConfig
            {
                Enabled = Enabled,
                HistoryCapacity = HistoryCapacity,
                Filter = Filter?.Clone() ?? new AudioDiagnosticFilter()
            };
        }
    }
}
