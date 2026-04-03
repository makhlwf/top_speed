namespace TopSpeed.Shortcuts
{
    internal readonly struct ShortcutContext
    {
        public ShortcutContext(string menuId, string viewId)
        {
            MenuId = menuId;
            ViewId = viewId;
        }

        public string MenuId { get; }
        public string ViewId { get; }
    }
}

