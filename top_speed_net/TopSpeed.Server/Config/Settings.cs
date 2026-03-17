namespace TopSpeed.Server.Config
{
    internal sealed class ServerSettings
    {
        public int Port { get; set; } = 28630;
        public int DiscoveryPort { get; set; } = 28631;
        public int MaxPlayers { get; set; } = 64;
        public string Motd { get; set; } = string.Empty;
        public bool CheckForUpdatesOnStartup { get; set; }
    }
}
