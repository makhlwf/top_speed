using System;
using System.Collections.Generic;
using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class RaceInput
    {
        private Dictionary<InputAction, InputActionBinding> CreateActionBindings()
        {
            var bindings = new Dictionary<InputAction, InputActionBinding>();

            void Add(
                InputAction action,
                string label,
                InputScope scope,
                TriggerMode keyboardMode,
                TriggerMode joystickMode,
                Func<Key> getKey,
                Action<Key> setKey,
                Func<JoystickAxisOrButton> getAxis,
                Action<JoystickAxisOrButton> setAxis,
                bool allowNumpadEnterAlias = false)
            {
                bindings[action] = new InputActionBinding(
                    label,
                    new InputActionMeta(scope, keyboardMode, joystickMode, allowNumpadEnterAlias),
                    getKey,
                    setKey,
                    getAxis,
                    setAxis);
                _actionDefinitions.Add(new InputActionDefinition(action, label));
            }

            Add(InputAction.SteerLeft, "Steer left", InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbLeft, key => SetLeft(key), () => _left, axis => SetLeft(axis));
            Add(InputAction.SteerRight, "Steer right", InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbRight, key => SetRight(key), () => _right, axis => SetRight(axis));
            Add(InputAction.Throttle, "Throttle", InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbThrottle, key => SetThrottle(key), () => _throttle, axis => SetThrottle(axis));
            Add(InputAction.Brake, "Brake", InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbBrake, key => SetBrake(key), () => _brake, axis => SetBrake(axis));
            Add(InputAction.GearUp, "Shift gear up", InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbGearUp, key => SetGearUp(key), () => _gearUp, axis => SetGearUp(axis));
            Add(InputAction.GearDown, "Shift gear down", InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbGearDown, key => SetGearDown(key), () => _gearDown, axis => SetGearDown(axis));
            Add(InputAction.Horn, "Use horn", InputScope.Driving, TriggerMode.Hold, TriggerMode.Hold, () => _kbHorn, key => SetHorn(key), () => _horn, axis => SetHorn(axis));
            Add(InputAction.RequestInfo, "Request position information", InputScope.Auxiliary, TriggerMode.Hold, TriggerMode.Hold, () => _kbRequestInfo, key => SetRequestInfo(key), () => _requestInfo, axis => SetRequestInfo(axis));
            Add(InputAction.CurrentGear, "Current gear", InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbCurrentGear, key => SetCurrentGear(key), () => _currentGear, axis => SetCurrentGear(axis));
            Add(InputAction.CurrentLapNr, "Current lap number", InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbCurrentLapNr, key => SetCurrentLapNr(key), () => _currentLapNr, axis => SetCurrentLapNr(axis));
            Add(InputAction.CurrentRacePerc, "Current race percentage", InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbCurrentRacePerc, key => SetCurrentRacePerc(key), () => _currentRacePerc, axis => SetCurrentRacePerc(axis));
            Add(InputAction.CurrentLapPerc, "Current lap percentage", InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbCurrentLapPerc, key => SetCurrentLapPerc(key), () => _currentLapPerc, axis => SetCurrentLapPerc(axis));
            Add(InputAction.CurrentRaceTime, "Current race time", InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbCurrentRaceTime, key => SetCurrentRaceTime(key), () => _currentRaceTime, axis => SetCurrentRaceTime(axis));
            Add(InputAction.StartEngine, "Start the engine", InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbStartEngine, key => SetStartEngine(key), () => _startEngine, axis => SetStartEngine(axis), allowNumpadEnterAlias: true);
            Add(InputAction.ReportDistance, "Report distance", InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbReportDistance, key => SetReportDistance(key), () => _reportDistance, axis => SetReportDistance(axis));
            Add(InputAction.ReportSpeed, "Report speed", InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbReportSpeed, key => SetReportSpeed(key), () => _reportSpeed, axis => SetReportSpeed(axis));
            Add(InputAction.TrackName, "Report track name", InputScope.Auxiliary, TriggerMode.Press, TriggerMode.Press, () => _kbTrackName, key => SetTrackName(key), () => _trackName, axis => SetTrackName(axis));
            Add(InputAction.Pause, "Pause", InputScope.Auxiliary, TriggerMode.Hold, TriggerMode.Hold, () => _kbPause, key => SetPause(key), () => _pause, axis => SetPause(axis));

            return bindings;
        }
    }
}
