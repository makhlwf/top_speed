using System;
using System.Collections.Generic;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Input.Backends.Sdl
{
    internal sealed class Controller : IControllerBackend
    {
        public event Action? ScanTimedOut
        {
            add { }
            remove { }
        }

        public bool ActiveControllerIsRacingWheel => false;
        public bool IgnoreAxesForMenuNavigation => false;
        public IVibrationDevice? VibrationDevice => null;

        public void SetEnabled(bool enabled)
        {
        }

        public void Update()
        {
        }

        public bool TryGetState(out State state)
        {
            state = default;
            return false;
        }

        public bool TryPollState(out State state)
        {
            state = default;
            return false;
        }

        public bool IsAnyButtonHeld()
        {
            return false;
        }

        public bool TryGetPendingChoices(out IReadOnlyList<Choice> choices)
        {
            choices = Array.Empty<Choice>();
            return false;
        }

        public bool TrySelect(Guid instanceGuid)
        {
            return false;
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
