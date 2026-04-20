using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal interface IMenuMappingActions
    {
        void BeginMapping(InputMappingMode mode, DriveIntent action);
        void BeginShortcutMapping(string groupId, string actionId, string displayName);
        string FormatMappingValue(DriveIntent action, InputMappingMode mode);
        void ResetMappings(InputMappingMode mode);
    }
}

