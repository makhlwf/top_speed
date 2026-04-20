using System;

namespace TopSpeed.Menu
{
    internal static class MenuTouchProfile
    {
        public const string MultiplayerTopZoneId = "menu_multiplayer_top";
        public const string MultiplayerBottomZoneId = "menu_multiplayer_bottom";
        public const float MultiplayerSplitY = 0.5f;

        public static bool UsesMultiplayerZones(string? menuId)
        {
            return !string.IsNullOrWhiteSpace(menuId) &&
                menuId!.StartsWith("multiplayer", StringComparison.Ordinal);
        }
    }
}

