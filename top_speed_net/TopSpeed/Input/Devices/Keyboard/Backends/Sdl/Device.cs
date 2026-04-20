using TS.Sdl.Input;
using SdlKeyboard = TS.Sdl.Input.Keyboard;

namespace TopSpeed.Input.Devices.Keyboard.Backends.Sdl
{
    internal sealed class Device : IKeyboardDevice
    {
        private bool _suspended;

        public bool TryPopulateState(InputState state)
        {
            if (state == null)
                return false;
            if (_suspended)
                return true;

            var keyboard = SdlKeyboard.GetState();
            if (!keyboard.IsValid)
                return true;

            for (var i = 0; i < (int)Scancode.Count; i++)
            {
                var code = (Scancode)i;
                if (!keyboard.IsDown(code))
                    continue;
                if (!code.TryToInputKey(out var key))
                    continue;

                state.Set(key, true);
            }

            return true;
        }

        public bool IsDown(InputKey key)
        {
            if (_suspended)
                return false;
            if (!key.TryToScancode(out var code))
                return false;

            var keyboard = SdlKeyboard.GetState();
            return keyboard.IsValid && keyboard.IsDown(code);
        }

        public bool IsAnyKeyHeld(bool ignoreModifiers)
        {
            if (_suspended)
                return false;

            var keyboard = SdlKeyboard.GetState();
            if (!keyboard.IsValid)
                return false;

            for (var i = 0; i < (int)Scancode.Count; i++)
            {
                var code = (Scancode)i;
                if (!keyboard.IsDown(code))
                    continue;
                if (!code.TryToInputKey(out var key))
                    continue;
                if (ignoreModifiers && IsModifier(key))
                    continue;

                return true;
            }

            return false;
        }

        public void ResetHeldState()
        {
        }

        public void Suspend()
        {
            _suspended = true;
        }

        public void Resume()
        {
            _suspended = false;
        }

        public void Dispose()
        {
        }

        private static bool IsModifier(InputKey key)
        {
            return key == InputKey.LeftControl ||
                   key == InputKey.RightControl ||
                   key == InputKey.LeftShift ||
                   key == InputKey.RightShift ||
                   key == InputKey.LeftAlt ||
                   key == InputKey.RightAlt;
        }
    }
}
