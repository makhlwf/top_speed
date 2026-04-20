using System;
using System.Collections.Generic;
using TS.Sdl.Input;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Input
{
    internal interface IInputService : IDisposable
    {
        event Action? NoControllerDetected;
        event Action<string>? ControllerBackendUnavailable;

        InputState Current { get; }
        bool ActiveControllerIsRacingWheel { get; }
        bool IgnoreControllerAxesForMenuNavigation { get; }
        IVibrationDevice? VibrationDevice { get; }
        bool TryGetControllerDisplayProfile(out ControllerDisplayProfile profile);

        void Update();
        bool IsDown(Key key);
        bool WasPressed(Key key);
        void SubmitGesture(in GestureEvent value);
        void SubmitTouchZoneGesture(in TouchZoneGestureEvent value);
        bool WasGesturePressed(GestureIntent intent);
        bool WasZoneGesturePressed(GestureIntent intent, string zoneId);
        bool TryGetTouchZoneState(string zoneId, out TouchZoneState state);
        void SetTouchZones(IReadOnlyList<TouchZone> zones);
        void ClearTouchZones();
        bool TryGetControllerState(out State state);
        DriveInputFrame CaptureDriveInputFrame();
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



