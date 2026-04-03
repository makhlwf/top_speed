namespace TopSpeed.Input.Devices.Keyboard
{
    internal interface IKeyboardDevice : System.IDisposable
    {
        bool TryPopulateState(InputState state);
        bool IsDown(InputKey key);
        bool IsAnyKeyHeld(bool ignoreModifiers);
        void ResetHeldState();
        void Suspend();
        void Resume();
    }
}
