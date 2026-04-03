using System;
using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Audio;
using TopSpeed.Shortcuts;
using TopSpeed.Speech;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuManager : IDisposable
    {
        private const int DefaultFadeMs = 1000;
        private readonly Dictionary<string, MenuScreen> _screens;
        private readonly ShortcutCatalog _shortcutCatalog;
        private readonly Stack<MenuScreen> _stack;
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly Func<bool> _usageHintsEnabled;
        private bool _wrapNavigation = true;
        private bool _menuNavigatePanning;
        private bool _menuAutoFocus = true;
        private string? _menuSoundPreset;
        private bool _menuMusicSuspended;

        public MenuManager(AudioManager audio, SpeechService speech, Func<bool>? usageHintsEnabled = null)
        {
            _audio = audio;
            _speech = speech;
            _usageHintsEnabled = usageHintsEnabled ?? (() => false);
            _screens = new Dictionary<string, MenuScreen>(StringComparer.Ordinal);
            _shortcutCatalog = new ShortcutCatalog();
            _stack = new Stack<MenuScreen>();
        }

        public void Register(MenuScreen screen)
        {
            if (!_screens.ContainsKey(screen.Id))
                _screens.Add(screen.Id, screen);
        }

        public void UpdateItems(string id, IEnumerable<MenuItem> items, bool preserveSelection = false)
        {
            var screen = GetScreen(id);
            screen.ReplaceItems(items, preserveSelection);
        }

        public void UpdateItems(string id, string screenId, IEnumerable<MenuItem> items, bool preserveSelection = false)
        {
            var screen = GetScreen(id);
            if (screen.UpdateScreenItems(screenId, items, preserveSelection))
                return;

            throw new InvalidOperationException($"Screen '{screenId}' is not registered for menu '{id}'.");
        }

        public void SetScreens(string id, IEnumerable<MenuView> screens, string? initialScreenId = null)
        {
            var screen = GetScreen(id);
            screen.SetScreens(screens, initialScreenId);
        }

        public void RegisterShortcutAction(
            string actionId,
            string displayName,
            string description,
            Key key,
            Action onTrigger,
            Func<bool>? canExecute = null)
        {
            _shortcutCatalog.RegisterAction(actionId, displayName, description, key, onTrigger, canExecute);
        }

        public void SetShortcutBinding(string actionId, Key key)
        {
            _shortcutCatalog.SetBinding(actionId, key);
        }

        public void SetGlobalShortcutActions(IEnumerable<string>? actionIds)
        {
            _shortcutCatalog.SetGlobalActions(actionIds);
        }

        public void SetScopeShortcutActions(string scopeId, IEnumerable<string>? actionIds, string? scopeName = null)
        {
            _shortcutCatalog.SetScopeActions(scopeId, actionIds, scopeName);
        }

        public void SetMenuShortcutScopes(string id, IEnumerable<string>? scopeIds)
        {
            var screen = GetScreen(id);
            _shortcutCatalog.SetMenuScopes(screen.Id, scopeIds);
        }

        public void SetMenuShortcutActions(string id, IEnumerable<string>? actionIds, string? menuName = null)
        {
            var screen = GetScreen(id);
            var resolvedMenuName = string.IsNullOrWhiteSpace(menuName) ? screen.Title : menuName;
            _shortcutCatalog.SetMenuActions(screen.Id, actionIds, resolvedMenuName);
        }

        public void SetViewShortcutActions(string viewId, IEnumerable<string>? actionIds, string? viewName = null)
        {
            _shortcutCatalog.SetViewActions(viewId, actionIds, viewName);
        }

        public IReadOnlyList<ShortcutGroup> GetShortcutGroups()
        {
            return _shortcutCatalog.GetGroups();
        }

        public IReadOnlyList<ShortcutBinding> GetShortcutBindings(string groupId)
        {
            return _shortcutCatalog.GetGroupBindings(groupId);
        }

        public bool TryGetShortcutBinding(string actionId, out ShortcutBinding binding)
        {
            return _shortcutCatalog.TryGetBinding(actionId, out binding);
        }

        public bool IsShortcutKeyInUse(string groupId, Key key, string ignoredActionId)
        {
            return _shortcutCatalog.IsKeyInUseInGroup(groupId, key, ignoredActionId);
        }

        public void ResetShortcutBindings()
        {
            _shortcutCatalog.ResetBindings();
        }

        public void SetCloseHandler(string id, Func<MenuCloseSource, bool>? closeHandler)
        {
            var screen = GetScreen(id);
            screen.CloseHandler = closeHandler;
        }

    }
}


