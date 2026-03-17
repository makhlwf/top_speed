using System;
using System.Collections.Generic;

namespace TopSpeed.Server.Commands
{
    internal sealed class CommandRegistry
    {
        private readonly Dictionary<string, CommandDefinition> _lookup;
        private readonly List<CommandDefinition> _ordered;

        public CommandRegistry(IEnumerable<CommandDefinition> commands)
        {
            _lookup = new Dictionary<string, CommandDefinition>(StringComparer.OrdinalIgnoreCase);
            _ordered = new List<CommandDefinition>();
            if (commands == null)
                return;

            foreach (var command in commands)
            {
                if (command == null)
                    continue;
                if (_lookup.ContainsKey(command.Name))
                    continue;

                _lookup[command.Name] = command;
                _ordered.Add(command);
            }
        }

        public IReadOnlyList<CommandDefinition> Commands => _ordered;

        public bool TryGet(string name, out CommandDefinition command)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                command = null!;
                return false;
            }

            return _lookup.TryGetValue(name.Trim(), out command!);
        }
    }
}
