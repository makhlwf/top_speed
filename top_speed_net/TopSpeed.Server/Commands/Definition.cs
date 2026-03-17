using System;

namespace TopSpeed.Server.Commands
{
    internal sealed class CommandDefinition
    {
        private readonly Action _execute;

        public CommandDefinition(string name, string description, Action execute)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Command name is required.", nameof(name));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Command description is required.", nameof(description));

            Name = name.Trim();
            Description = description.Trim();
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public string Name { get; }
        public string Description { get; }

        public void Execute()
        {
            _execute();
        }
    }
}
