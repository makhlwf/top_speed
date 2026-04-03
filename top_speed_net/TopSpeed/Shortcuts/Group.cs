using System;

namespace TopSpeed.Shortcuts
{
    internal readonly struct ShortcutGroup
    {
        public ShortcutGroup(string id, string name, bool isGlobal)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Shortcut group id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Shortcut group name is required.", nameof(name));

            Id = id.Trim();
            Name = name.Trim();
            IsGlobal = isGlobal;
        }

        public string Id { get; }
        public string Name { get; }
        public bool IsGlobal { get; }
    }
}

