using System;

namespace TopSpeed.Menu
{
    internal sealed class MenuItemAction
    {
        public MenuItemAction(string label, Action? onActivate = null)
        {
            Label = label ?? string.Empty;
            OnActivate = onActivate;
        }

        public string Label { get; }
        public Action? OnActivate { get; }

        public void Activate()
        {
            OnActivate?.Invoke();
        }
    }
}
