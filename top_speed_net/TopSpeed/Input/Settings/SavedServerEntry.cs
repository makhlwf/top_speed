namespace TopSpeed.Input
{
    internal sealed class SavedServerEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string DefaultCallSign { get; set; } = string.Empty;
    }
}

