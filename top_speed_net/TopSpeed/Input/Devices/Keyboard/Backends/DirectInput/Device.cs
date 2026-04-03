using System;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Keyboard;
using Key = SharpDX.DirectInput.Key;

namespace TopSpeed.Input.Devices.Keyboard.Backends.DirectInput
{
    internal sealed class Device : IKeyboardDevice
    {
        private readonly SharpDX.DirectInput.Keyboard _keyboard;
        private readonly SharpDX.DirectInput.DirectInput _directInput;
        private bool _disposed;

        public Device(IntPtr windowHandle)
        {
            _directInput = new SharpDX.DirectInput.DirectInput();
            _keyboard = new SharpDX.DirectInput.Keyboard(_directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            TryAcquire();
        }

        public bool TryPopulateState(InputState state)
        {
            if (!TryGetKeyboardState(out var snapshot))
                return false;

            foreach (var key in snapshot.PressedKeys)
            {
                state.Set(key.ToInputKey(), true);
            }

            ApplyModifierFallbacks(snapshot, state);
            return true;
        }

        public bool IsDown(InputKey key)
        {
            if (!TryGetKeyboardState(out var state))
                return false;

            return state.IsPressed(key.ToDirectInputKey());
        }

        public bool IsAnyKeyHeld(bool ignoreModifiers)
        {
            if (!TryGetKeyboardState(out var state))
                return false;

            if (!ignoreModifiers)
                return state.PressedKeys.Count > 0;

            foreach (var key in state.PressedKeys)
            {
                if (key == Key.LeftControl || key == Key.RightControl ||
                    key == Key.LeftShift || key == Key.RightShift ||
                    key == Key.LeftAlt || key == Key.RightAlt)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        public void ResetHeldState()
        {
            // DirectInput reads live state on demand; no cached held-state reset required.
        }

        public void Suspend()
        {
            SafeRelease(() => _keyboard.Unacquire());
        }

        public void Resume()
        {
            TryAcquire();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            SafeRelease(() => _keyboard.Unacquire());
            SafeRelease(() => _keyboard.Dispose());
            SafeRelease(() => _directInput.Dispose());
        }

        private bool TryAcquire()
        {
            if (_disposed)
                return false;

            try
            {
                _keyboard.Acquire();
                return true;
            }
            catch (SharpDXException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }

        private bool TryGetKeyboardState(out SharpDX.DirectInput.KeyboardState state)
        {
            state = null!;
            if (_disposed)
                return false;

            try
            {
                _keyboard.Acquire();
                state = _keyboard.GetCurrentState();
                return true;
            }
            catch (SharpDXException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }

        private static void ApplyModifierFallbacks(SharpDX.DirectInput.KeyboardState state, InputState inputState)
        {
            // Some setups intermittently miss right-side modifiers in DirectInput state while
            // chorded with arrows. Use Win32 async key state as fallback.
            var anyShift = state.IsPressed(Key.LeftShift)
                || state.IsPressed(Key.RightShift)
                || IsVirtualKeyDown(VkShift);
            var leftShift = state.IsPressed(Key.LeftShift) || IsVirtualKeyDown(VkLeftShift);
            var rightShift = state.IsPressed(Key.RightShift) || IsVirtualKeyDown(VkRightShift);
            if (anyShift && !leftShift && !rightShift)
            {
                leftShift = true;
                rightShift = true;
            }

            var anyCtrl = state.IsPressed(Key.LeftControl)
                || state.IsPressed(Key.RightControl)
                || IsVirtualKeyDown(VkControl);
            var leftCtrl = state.IsPressed(Key.LeftControl) || IsVirtualKeyDown(VkLeftControl);
            var rightCtrl = state.IsPressed(Key.RightControl) || IsVirtualKeyDown(VkRightControl);
            if (anyCtrl && !leftCtrl && !rightCtrl)
            {
                leftCtrl = true;
                rightCtrl = true;
            }

            var anyAlt = state.IsPressed(Key.LeftAlt)
                || state.IsPressed(Key.RightAlt)
                || IsVirtualKeyDown(VkMenu);
            var leftAlt = state.IsPressed(Key.LeftAlt) || IsVirtualKeyDown(VkLeftMenu);
            var rightAlt = state.IsPressed(Key.RightAlt) || IsVirtualKeyDown(VkRightMenu);
            if (anyAlt && !leftAlt && !rightAlt)
            {
                leftAlt = true;
                rightAlt = true;
            }

            inputState.Set(InputKey.LeftShift, leftShift);
            inputState.Set(InputKey.RightShift, rightShift);
            inputState.Set(InputKey.LeftControl, leftCtrl);
            inputState.Set(InputKey.RightControl, rightCtrl);
            inputState.Set(InputKey.LeftAlt, leftAlt);
            inputState.Set(InputKey.RightAlt, rightAlt);
        }

        private static bool IsVirtualKeyDown(int vk)
        {
            return (GetAsyncKeyState(vk) & 0x8000) != 0;
        }

        private static void SafeRelease(Action release)
        {
            try
            {
                release();
            }
            catch (SharpDXException)
            {
            }
            catch (NullReferenceException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private const int VkShift = 0x10;
        private const int VkControl = 0x11;
        private const int VkMenu = 0x12;
        private const int VkLeftShift = 0xA0;
        private const int VkRightShift = 0xA1;
        private const int VkLeftControl = 0xA2;
        private const int VkRightControl = 0xA3;
        private const int VkLeftMenu = 0xA4;
        private const int VkRightMenu = 0xA5;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }
}

