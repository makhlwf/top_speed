using System.Text.Json.Serialization;

namespace TopSpeed.Server.Config
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(ServerSettings))]
    internal partial class ServerSettingsJsonContext : JsonSerializerContext
    {
    }
}
