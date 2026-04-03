namespace TopSpeed.Core
{
    internal readonly struct CustomVehicleInfo
    {
        public CustomVehicleInfo(string key, string display, string version, string description)
        {
            Key = key;
            Display = display;
            Version = version;
            Description = description;
        }

        public string Key { get; }
        public string Display { get; }
        public string Version { get; }
        public string Description { get; }
    }
}

