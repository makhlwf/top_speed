namespace TopSpeed.Input.Devices.Keyboard.Backends.Sdl
{
    internal sealed class Device : IKeyboardDevice
    {
        public bool TryPopulateState(InputState state)
        {
            return false;
        }

        public bool IsDown(InputKey key)
        {
            return false;
        }

        public bool IsAnyKeyHeld(bool ignoreModifiers)
        {
            return false;
        }

        public void ResetHeldState()
        {
        }

        public void Suspend()
        {
        }

        public void Resume()
        {
        }

        public void Dispose()
        {
        }
    }
}
