using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TopSpeed.Protocol;
using TopSpeed.Server.Config;
using TopSpeed.Server.Logging;
using TopSpeed.Server.Network;
using TopSpeed.Server.Updates;

namespace TopSpeed.Server.Commands
{
    internal sealed class CommandHost : IDisposable
    {
        private readonly RaceServer _server;
        private readonly ServerSettings _settings;
        private readonly ServerSettingsStore _settingsStore;
        private readonly Logger _logger;
        private readonly CancellationTokenSource _shutdownSource;
        private readonly ServerUpdateRunner _updater;
        private readonly CommandRegistry _registry;
        private Thread? _thread;
        private bool _stopRequested;

        public CommandHost(
            RaceServer server,
            ServerSettings settings,
            ServerSettingsStore settingsStore,
            Logger logger,
            CancellationTokenSource shutdownSource,
            ServerUpdateRunner updater)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _shutdownSource = shutdownSource ?? throw new ArgumentNullException(nameof(shutdownSource));
            _updater = updater ?? throw new ArgumentNullException(nameof(updater));
            _registry = new CommandRegistry(new[]
            {
                new CommandDefinition("help", "Show available server commands.", ExecuteHelp),
                new CommandDefinition("options", "Open server options menu.", ExecuteOptions),
                new CommandDefinition("players", "List connected players and protocol versions.", ExecutePlayers),
                new CommandDefinition("version", "Display server and protocol versions.", ExecuteVersion),
                new CommandDefinition("update", "Manually check for server updates.", ExecuteUpdate),
                new CommandDefinition("shutdown", "Shutdown the server.", ExecuteShutdown)
            });
        }

        public bool Start()
        {
            if (!IsInputAvailable())
            {
                var message = "Standard input is not available. Server commands are disabled.";
                _logger.Warning(message);
                ConsoleSink.WriteLine(message);
                return false;
            }

            ConsoleSink.WriteLine("Server command interface ready. Type \"help\" to get the list of commands.");
            _thread = new Thread(RunLoop)
            {
                IsBackground = true,
                Name = "TopSpeed.Server.Commands"
            };
            _thread.Start();
            return true;
        }

        public void Dispose()
        {
            _stopRequested = true;
        }

        private void RunLoop()
        {
            while (!_stopRequested && !_shutdownSource.IsCancellationRequested)
            {
                if (!CommandInput.TryReadLine(">", out var raw))
                {
                    DisableCommands("Standard input is no longer available. Server commands are disabled.");
                    return;
                }

                var input = raw.Trim();
                if (input.Length == 0)
                    continue;

                var commandName = ParseCommandName(input);
                if (!_registry.TryGet(commandName, out var command))
                {
                    ConsoleSink.WriteLine($"Invalid command \"{commandName}\". Type \"help\" for the list of commands.");
                    continue;
                }

                try
                {
                    command.Execute();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Command '{command.Name}' failed: {ex.Message}");
                    ConsoleSink.WriteLine("Command failed. Check server logs for details.");
                }
            }
        }

        private void ExecuteHelp()
        {
            ConsoleSink.WriteLine("Available commands:");
            var commands = _registry.Commands;
            for (var i = 0; i < commands.Count; i++)
            {
                var command = commands[i];
                ConsoleSink.WriteLine($"\"{command.Name}\": {command.Description}");
            }
        }

        private void ExecutePlayers()
        {
            var players = _server.GetPlayersSnapshot();
            ConsoleSink.WriteLine($"{players.Length} players are connected:");
            for (var i = 0; i < players.Length; i++)
            {
                var player = players[i];
                ConsoleSink.WriteLine($"{player.Name}, using protocol version {player.ProtocolVersion}");
            }
        }

        private void ExecuteShutdown()
        {
            ConsoleSink.WriteLine("shutting down...");
            _server.ShutdownByHost("The server will be shut down immediately by the host.");
            _stopRequested = true;
            _shutdownSource.Cancel();
        }

        private void ExecuteVersion()
        {
            ConsoleSink.WriteLine($"Server version: {ServerUpdateConfig.CurrentVersion.ToMachineString()}");
            ConsoleSink.WriteLine($"Protocol version: {ProtocolProfile.Current.ToMachineString()}");
            ConsoleSink.WriteLine($"Protocol supported range: {ProtocolProfile.ServerSupported.MinSupported.ToMachineString()} to {ProtocolProfile.ServerSupported.MaxSupported.ToMachineString()}");
        }

        private void ExecuteUpdate()
        {
            if (_updater.RunInteractiveCheck())
                ExecuteShutdown();
        }

        private void ExecuteOptions()
        {
            while (!_stopRequested && !_shutdownSource.IsCancellationRequested)
            {
                var options = BuildOptionsMenuEntries();
                if (!CommandInput.TryPromptMenuChoice("Server options:", options, out var choiceIndex))
                {
                    DisableCommands("Standard input is no longer available. Server commands are disabled.");
                    return;
                }

                switch (choiceIndex)
                {
                    case 0:
                        EditMotd();
                        break;
                    case 1:
                        EditServerPort();
                        break;
                    case 2:
                        EditDiscoveryPort();
                        break;
                    case 3:
                        EditMaxPlayers();
                        break;
                    case 4:
                        ToggleCheckForUpdatesOnStartup();
                        break;
                    default:
                        return;
                }
            }
        }

        private IReadOnlyList<string> BuildOptionsMenuEntries()
        {
            return new[]
            {
                $"Message of the day: {FormatMotd(_settings.Motd)}",
                $"Server port: {_settings.Port}",
                $"Discovery port: {_settings.DiscoveryPort}",
                $"Max players: {_settings.MaxPlayers}",
                $"Check for updates on startup: {CommandInput.FormatOnOff(_settings.CheckForUpdatesOnStartup)}",
                "Back"
            };
        }

        private void EditMotd()
        {
            var prompt = $"Enter message of the day (max {ProtocolConstants.MaxMotdLength} chars, empty clears value):";
            if (!CommandInput.TryPromptText(prompt, ProtocolConstants.MaxMotdLength, allowEmpty: true, out var motd))
            {
                DisableCommands("Standard input is no longer available. Server commands are disabled.");
                return;
            }

            _settings.Motd = motd;
            _server.SetMotd(motd);
            SaveSettings();
            ConsoleSink.WriteLine("Message of the day updated.");
        }

        private void EditServerPort()
        {
            if (!CommandInput.TryPromptInt("Enter server port (1-65535):", 1, 65535, out var port))
            {
                DisableCommands("Standard input is no longer available. Server commands are disabled.");
                return;
            }

            _settings.Port = port;
            SaveSettings();
            ConsoleSink.WriteLine($"Server port updated to {port}. Restart required for this change.");
        }

        private void EditDiscoveryPort()
        {
            if (!CommandInput.TryPromptInt("Enter discovery port (1-65535):", 1, 65535, out var port))
            {
                DisableCommands("Standard input is no longer available. Server commands are disabled.");
                return;
            }

            _settings.DiscoveryPort = port;
            SaveSettings();
            ConsoleSink.WriteLine($"Discovery port updated to {port}. Restart required for this change.");
        }

        private void EditMaxPlayers()
        {
            if (!CommandInput.TryPromptInt("Enter max players (1-255):", 1, byte.MaxValue, out var maxPlayers))
            {
                DisableCommands("Standard input is no longer available. Server commands are disabled.");
                return;
            }

            _settings.MaxPlayers = maxPlayers;
            _server.SetMaxPlayers(maxPlayers);
            SaveSettings();
            ConsoleSink.WriteLine($"Max players updated to {maxPlayers}.");
        }

        private void ToggleCheckForUpdatesOnStartup()
        {
            _settings.CheckForUpdatesOnStartup = !_settings.CheckForUpdatesOnStartup;
            SaveSettings();
            ConsoleSink.WriteLine($"Check for updates on startup: {CommandInput.FormatOnOff(_settings.CheckForUpdatesOnStartup)}");
        }

        private void SaveSettings()
        {
            _settingsStore.Save(_settings, _logger);
        }

        private void DisableCommands(string message)
        {
            _stopRequested = true;
            _logger.Warning(message);
            ConsoleSink.WriteLine(message);
        }

        private static string ParseCommandName(string input)
        {
            var index = input.IndexOf(' ');
            if (index < 0)
                return input.Trim();
            return input.Substring(0, index).Trim();
        }

        private static string FormatMotd(string motd)
        {
            return string.IsNullOrWhiteSpace(motd) ? "(empty)" : motd;
        }

        private static bool IsInputAvailable()
        {
            if (Console.IsInputRedirected)
                return true;

            try
            {
                _ = Console.KeyAvailable;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}
