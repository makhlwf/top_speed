using System;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuManager
    {
        public bool TryPlayNavigateCue(string menuId)
        {
            if (string.IsNullOrWhiteSpace(menuId) || _stack.Count == 0)
                return false;

            if (!_screens.TryGetValue(menuId, out var screen))
                return false;

            if (!ReferenceEquals(_stack.Peek(), screen))
                return false;

            screen.PlayNavigateCue();
            return true;
        }

        public bool TryPlayWrapCue(string menuId)
        {
            if (string.IsNullOrWhiteSpace(menuId) || _stack.Count == 0)
                return false;

            if (!_screens.TryGetValue(menuId, out var screen))
                return false;

            if (!ReferenceEquals(_stack.Peek(), screen))
                return false;

            screen.PlayWrapCue();
            return true;
        }

        public bool TryPlayEdgeCue(string menuId)
        {
            if (string.IsNullOrWhiteSpace(menuId) || _stack.Count == 0)
                return false;

            if (!_screens.TryGetValue(menuId, out var screen))
                return false;

            if (!ReferenceEquals(_stack.Peek(), screen))
                return false;

            screen.PlayEdgeCue();
            return true;
        }
    }
}
