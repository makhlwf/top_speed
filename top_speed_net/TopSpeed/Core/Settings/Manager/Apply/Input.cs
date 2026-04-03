using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input;
using TopSpeed.Localization;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static void ApplyInput(RaceSettings settings, SettingsInputDocument input, List<SettingsIssue> issues)
        {
            if (input.ForceFeedback.HasValue)
                settings.ForceFeedback = input.ForceFeedback.Value;

            settings.KeyboardProgressiveRate = ReadEnum(input.KeyboardProgressiveRate, settings.KeyboardProgressiveRate, "input.keyboardProgressiveRate", issues);
            settings.DeviceMode = ReadEnum(input.DeviceMode, settings.DeviceMode, "input.deviceMode", issues);

            if (input.Keyboard == null)
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Warning,
                    "input.keyboard",
                    LocalizationService.Mark("Keyboard bindings section is missing. Defaults were used for keyboard bindings.")));
            else
                ApplyKeyboard(settings, input.Keyboard, issues);

            ApplyMenuShortcuts(settings, input.MenuShortcuts, issues);

            if (input.Controller == null)
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Warning,
                    "input.controller",
                    LocalizationService.Mark("Controller bindings section is missing. Defaults were used for controller bindings.")));
            else
                ApplyController(settings, input.Controller, issues);
        }

        private static void ApplyKeyboard(RaceSettings settings, SettingsKeyboardDocument keyboard, List<SettingsIssue> issues)
        {
            settings.KeyLeft = ReadKey(keyboard.Left, settings.KeyLeft, "input.keyboard.left", issues);
            settings.KeyRight = ReadKey(keyboard.Right, settings.KeyRight, "input.keyboard.right", issues);
            settings.KeyThrottle = ReadKey(keyboard.Throttle, settings.KeyThrottle, "input.keyboard.throttle", issues);
            settings.KeyBrake = ReadKey(keyboard.Brake, settings.KeyBrake, "input.keyboard.brake", issues);
            settings.KeyClutch = ReadKey(keyboard.Clutch, settings.KeyClutch, "input.keyboard.clutch", issues);
            settings.KeyGearUp = ReadKey(keyboard.GearUp, settings.KeyGearUp, "input.keyboard.gearUp", issues);
            settings.KeyGearDown = ReadKey(keyboard.GearDown, settings.KeyGearDown, "input.keyboard.gearDown", issues);
            settings.KeyHorn = ReadKey(keyboard.Horn, settings.KeyHorn, "input.keyboard.horn", issues);
            settings.KeyRequestInfo = ReadKey(keyboard.RequestInfo, settings.KeyRequestInfo, "input.keyboard.requestInfo", issues);
            settings.KeyCurrentGear = ReadKey(keyboard.CurrentGear, settings.KeyCurrentGear, "input.keyboard.currentGear", issues);
            settings.KeyCurrentLapNr = ReadKey(keyboard.CurrentLapNr, settings.KeyCurrentLapNr, "input.keyboard.currentLapNr", issues);
            settings.KeyCurrentRacePerc = ReadKey(keyboard.CurrentRacePerc, settings.KeyCurrentRacePerc, "input.keyboard.currentRacePerc", issues);
            settings.KeyCurrentLapPerc = ReadKey(keyboard.CurrentLapPerc, settings.KeyCurrentLapPerc, "input.keyboard.currentLapPerc", issues);
            settings.KeyCurrentRaceTime = ReadKey(keyboard.CurrentRaceTime, settings.KeyCurrentRaceTime, "input.keyboard.currentRaceTime", issues);
            settings.KeyStartEngine = ReadKey(keyboard.StartEngine, settings.KeyStartEngine, "input.keyboard.startEngine", issues);
            settings.KeyReportDistance = ReadKey(keyboard.ReportDistance, settings.KeyReportDistance, "input.keyboard.reportDistance", issues);
            settings.KeyReportSpeed = ReadKey(keyboard.ReportSpeed, settings.KeyReportSpeed, "input.keyboard.reportSpeed", issues);
            settings.KeyTrackName = ReadKey(keyboard.TrackName, settings.KeyTrackName, "input.keyboard.trackName", issues);
            settings.KeyPause = ReadKey(keyboard.Pause, settings.KeyPause, "input.keyboard.pause", issues);
        }

        private static void ApplyController(RaceSettings settings, SettingsControllerDocument controller, List<SettingsIssue> issues)
        {
            settings.ControllerLeft = ReadController(controller.Left, settings.ControllerLeft, "input.controller.left", issues);
            settings.ControllerRight = ReadController(controller.Right, settings.ControllerRight, "input.controller.right", issues);
            settings.ControllerThrottle = ReadController(controller.Throttle, settings.ControllerThrottle, "input.controller.throttle", issues);
            settings.ControllerBrake = ReadController(controller.Brake, settings.ControllerBrake, "input.controller.brake", issues);
            settings.ControllerClutch = ReadController(controller.Clutch, settings.ControllerClutch, "input.controller.clutch", issues);
            settings.ControllerGearUp = ReadController(controller.GearUp, settings.ControllerGearUp, "input.controller.gearUp", issues);
            settings.ControllerGearDown = ReadController(controller.GearDown, settings.ControllerGearDown, "input.controller.gearDown", issues);
            settings.ControllerHorn = ReadController(controller.Horn, settings.ControllerHorn, "input.controller.horn", issues);
            settings.ControllerRequestInfo = ReadController(controller.RequestInfo, settings.ControllerRequestInfo, "input.controller.requestInfo", issues);
            settings.ControllerCurrentGear = ReadController(controller.CurrentGear, settings.ControllerCurrentGear, "input.controller.currentGear", issues);
            settings.ControllerCurrentLapNr = ReadController(controller.CurrentLapNr, settings.ControllerCurrentLapNr, "input.controller.currentLapNr", issues);
            settings.ControllerCurrentRacePerc = ReadController(controller.CurrentRacePerc, settings.ControllerCurrentRacePerc, "input.controller.currentRacePerc", issues);
            settings.ControllerCurrentLapPerc = ReadController(controller.CurrentLapPerc, settings.ControllerCurrentLapPerc, "input.controller.currentLapPerc", issues);
            settings.ControllerCurrentRaceTime = ReadController(controller.CurrentRaceTime, settings.ControllerCurrentRaceTime, "input.controller.currentRaceTime", issues);
            settings.ControllerStartEngine = ReadController(controller.StartEngine, settings.ControllerStartEngine, "input.controller.startEngine", issues);
            settings.ControllerReportDistance = ReadController(controller.ReportDistance, settings.ControllerReportDistance, "input.controller.reportDistance", issues);
            settings.ControllerReportSpeed = ReadController(controller.ReportSpeed, settings.ControllerReportSpeed, "input.controller.reportSpeed", issues);
            settings.ControllerTrackName = ReadController(controller.TrackName, settings.ControllerTrackName, "input.controller.trackName", issues);
            settings.ControllerPause = ReadController(controller.Pause, settings.ControllerPause, "input.controller.pause", issues);
            settings.ControllerThrottleInvertMode = ReadEnum(controller.ThrottleInvertMode, settings.ControllerThrottleInvertMode, "input.controller.throttleInvertMode", issues);
            settings.ControllerBrakeInvertMode = ReadEnum(controller.BrakeInvertMode, settings.ControllerBrakeInvertMode, "input.controller.brakeInvertMode", issues);
            settings.ControllerSteeringDeadZone = ClampInt(controller.SteeringDeadZone, settings.ControllerSteeringDeadZone, 1, 5, "input.controller.steeringDeadZone", issues);

            if (controller.Center == null)
                return;

            var center = settings.ControllerCenter;
            if (controller.Center.X.HasValue) center.X = controller.Center.X.Value;
            if (controller.Center.Y.HasValue) center.Y = controller.Center.Y.Value;
            if (controller.Center.Z.HasValue) center.Z = controller.Center.Z.Value;
            if (controller.Center.Rx.HasValue) center.Rx = controller.Center.Rx.Value;
            if (controller.Center.Ry.HasValue) center.Ry = controller.Center.Ry.Value;
            if (controller.Center.Rz.HasValue) center.Rz = controller.Center.Rz.Value;
            if (controller.Center.Slider1.HasValue) center.Slider1 = controller.Center.Slider1.Value;
            if (controller.Center.Slider2.HasValue) center.Slider2 = controller.Center.Slider2.Value;
            settings.ControllerCenter = center;
        }

        private static void ApplyMenuShortcuts(RaceSettings settings, SettingsMenuShortcutsDocument? menuShortcuts, List<SettingsIssue> issues)
        {
            settings.ShortcutKeyBindings = new Dictionary<string, Key>(System.StringComparer.Ordinal);
            if (menuShortcuts?.Bindings == null)
                return;

            for (var i = 0; i < menuShortcuts.Bindings.Count; i++)
            {
                var binding = menuShortcuts.Bindings[i];
                if (binding == null || string.IsNullOrWhiteSpace(binding.Id))
                {
                    issues.Add(new SettingsIssue(
                        SettingsIssueSeverity.Warning,
                        $"input.menuShortcuts.bindings[{i}].id",
                        LocalizationService.Mark("Shortcut binding id is missing and was ignored.")));
                    continue;
                }

                if (!binding.Key.HasValue)
                    continue;

                var keyValue = binding.Key.Value;
                if (!System.Enum.IsDefined(typeof(Key), keyValue) || keyValue < 0)
                {
                    issues.Add(new SettingsIssue(
                        SettingsIssueSeverity.Warning,
                        $"input.menuShortcuts.bindings[{i}].key",
                        LocalizationService.Format(
                            LocalizationService.Mark("The key input.menuShortcuts.bindings[{0}].key has invalid value {1} and was ignored."),
                            i,
                            keyValue)));
                    continue;
                }

                var actionId = binding.Id!.Trim();
                settings.ShortcutKeyBindings[actionId] = (Key)keyValue;
            }
        }
    }
}


