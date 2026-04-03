using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal interface IMenuMappingActions
    {
        void BeginMapping(InputMappingMode mode, InputAction action);
        void BeginShortcutMapping(string groupId, string actionId, string displayName);
        string FormatMappingValue(InputAction action, InputMappingMode mode);
    }
}

