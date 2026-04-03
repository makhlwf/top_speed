using TopSpeed.Localization;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Session
        {
            public static string BuildDisconnectMessage(string reason)
            {
                if (string.IsNullOrWhiteSpace(reason))
                    return LocalizationService.Mark("The server closed the connection.");

                switch (reason)
                {
                    case "timeout":
                        return LocalizationService.Mark("Connection timed out.");
                    case "protocol_mismatch":
                        return LocalizationService.Mark("Connection refused due to protocol mismatch.");
                    case "protocol_rejected":
                        return LocalizationService.Mark("Connection refused due to invalid protocol negotiation.");
                    case "server_full":
                        return LocalizationService.Mark("This server is full.");
                    case "host_shutdown":
                        return LocalizationService.Mark("The server will be shut down immediately by the host.");
                    case "peer_disconnect":
                        return LocalizationService.Mark("Connection closed.");
                    default:
                        return LocalizationService.Format(LocalizationService.Mark("Connection closed by server. Reason: {0}."), reason);
                }
            }
        }
    }
}
