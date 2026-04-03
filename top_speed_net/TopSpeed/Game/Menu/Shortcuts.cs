using System;
using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void ApplySavedShortcutBindings()
        {
            if (_settings.ShortcutKeyBindings == null || _settings.ShortcutKeyBindings.Count == 0)
                return;

            var appliedBindings = new Dictionary<string, Key>(StringComparer.Ordinal);
            foreach (var pair in _settings.ShortcutKeyBindings)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;
                if (!_menu.TryGetShortcutBinding(pair.Key, out _))
                    continue;

                try
                {
                    _menu.SetShortcutBinding(pair.Key, pair.Value);
                    appliedBindings[pair.Key] = pair.Value;
                }
                catch (System.ArgumentException)
                {
                }
                catch (System.InvalidOperationException)
                {
                }
            }

            _settings.ShortcutKeyBindings = appliedBindings;
        }
    }
}


