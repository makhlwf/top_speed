using SharpDX.DirectInput;

namespace TopSpeed.Input.Devices.Keyboard
{
    internal static class DirectInputKeyMap
    {
        public static InputKey ToInputKey(this Key key)
        {
            return (InputKey)(int)key;
        }

        public static Key ToDirectInputKey(this InputKey key)
        {
            return (Key)(int)key;
        }
    }
}

