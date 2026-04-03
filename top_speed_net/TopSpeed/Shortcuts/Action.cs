using System;
using Key = TopSpeed.Input.InputKey;

namespace TopSpeed.Shortcuts
{
    internal sealed class ShortcutAction
    {
        private readonly Action _onTrigger;
        private readonly Func<bool>? _canExecute;

        public ShortcutAction(
            string id,
            string displayName,
            string description,
            Key key,
            Action onTrigger,
            Func<bool>? canExecute = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Shortcut action id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Shortcut display name is required.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Shortcut description is required.", nameof(description));

            Id = id.Trim();
            DisplayName = displayName.Trim();
            Description = description.Trim();
            Key = key;
            DefaultKey = key;
            _onTrigger = onTrigger ?? throw new ArgumentNullException(nameof(onTrigger));
            _canExecute = canExecute;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Key Key { get; private set; }
        public Key DefaultKey { get; }

        public void SetKey(Key key)
        {
            Key = key;
        }

        public void ResetKey()
        {
            Key = DefaultKey;
        }

        public bool CanExecute()
        {
            return _canExecute == null || _canExecute();
        }

        public void Trigger()
        {
            _onTrigger();
        }
    }
}


