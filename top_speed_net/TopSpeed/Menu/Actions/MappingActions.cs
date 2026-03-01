using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal interface IMenuMappingActions
    {
        void BeginMapping(InputMappingMode mode, InputAction action);
        string FormatMappingValue(InputAction action, InputMappingMode mode);
    }
}
