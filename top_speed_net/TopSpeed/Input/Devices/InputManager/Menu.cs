using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class InputService
    {
        public bool IsAnyInputHeld()
        {
            if (_suspended)
                return false;

            UpdateMenuBackLatchImmediate();

            if (IsAnyKeyboardKeyHeld())
                return true;

            return IsAnyControllerButtonHeld();
        }

        public void PrepareForInterruptableSpeech()
        {
            if (_suspended || _disposed)
                return;

            _keyboardBackend.ResetHeldState();
            _menuBackLatched = false;
            ResetState();
        }

        public bool IsAnyMenuInputHeld()
        {
            if (_suspended)
                return false;

            if (IsAnyKeyboardKeyHeld(ignoreModifiers: true))
                return true;

            return IsAnyControllerButtonHeld();
        }

        public bool IsMenuBackHeld()
        {
            if (_suspended)
                return false;

            if (IsDown(InputKey.Escape))
                return true;

            if (!_controllerBackend.TryGetState(out var state))
                return false;

            return IsMenuBackHeld(state);
        }

        public void LatchMenuBack()
        {
            _menuBackLatched = true;
        }

        public bool ShouldIgnoreMenuBack()
        {
            if (!_menuBackLatched)
                return false;

            if (IsMenuBackHeld())
                return true;

            _menuBackLatched = false;
            return false;
        }

        private bool IsAnyKeyboardKeyHeld(bool ignoreModifiers = false)
        {
            return _keyboardBackend.IsAnyKeyHeld(ignoreModifiers);
        }

        private bool IsAnyControllerButtonHeld()
        {
            return _controllerBackend.IsAnyButtonHeld();
        }

        private void UpdateMenuBackLatchImmediate()
        {
            if (!_menuBackLatched)
                return;

            if (!IsMenuBackHeldImmediate())
                _menuBackLatched = false;
        }

        private bool IsMenuBackHeldImmediate()
        {
            if (_keyboardBackend.IsDown(InputKey.Escape))
                return true;

            if (!_controllerBackend.TryPollState(out var state))
                return false;

            return IsMenuBackHeld(state);
        }

        private bool IsMenuBackHeld(State state)
        {
            if (state.Pov4)
                return true;

            if (IgnoreControllerAxesForMenuNavigation)
                return false;

            return state.X < -MenuBackThreshold;
        }
    }
}

