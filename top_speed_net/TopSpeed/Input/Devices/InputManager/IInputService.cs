using System;
using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Input
{
    internal interface IInputService : IDisposable
    {
        event Action? ControllerScanTimedOut;

        InputState Current { get; }
        bool ActiveControllerIsRacingWheel { get; }
        bool IgnoreControllerAxesForMenuNavigation { get; }
        IVibrationDevice? VibrationDevice { get; }

        void Update();
        bool IsDown(Key key);
        bool WasPressed(Key key);
        bool TryGetControllerState(out State state);
        void SetDeviceMode(InputDeviceMode mode);
        bool TryGetPendingControllerChoices(out IReadOnlyList<Choice> choices);
        bool TrySelectController(Guid instanceGuid);
        bool IsAnyInputHeld();
        bool IsAnyMenuInputHeld();
        bool IsMenuBackHeld();
        void LatchMenuBack();
        bool ShouldIgnoreMenuBack();
        void ResetState();
        void Suspend();
        void Resume();
    }
}



