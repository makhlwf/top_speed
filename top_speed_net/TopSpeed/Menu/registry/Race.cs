using System;
using System.Collections.Generic;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Parsing;

using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private void PrepareMode(RaceMode mode)
        {
            _setup.Mode = mode;
            _setup.ClearSelection();
        }

        private void CompleteTransmission(RaceMode mode, TransmissionMode transmission)
        {
            if (!SupportsTransmissionMode(transmission))
            {
                _ui.SpeakMessage(LocalizationService.Mark("This vehicle does not support the selected transmission mode."));
                return;
            }

            _setup.Transmission = transmission;
            _race.QueueRaceStart(mode);
        }

        private MenuScreen BuildTrackTypeMenu(string id, RaceMode mode)
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Race track"), MenuAction.None, nextMenuId: TrackMenuId(mode, TrackCategory.RaceTrack), onActivate: () => _setup.TrackCategory = TrackCategory.RaceTrack),
                new MenuItem(LocalizationService.Mark("Street adventure"), MenuAction.None, nextMenuId: TrackMenuId(mode, TrackCategory.StreetAdventure), onActivate: () => _setup.TrackCategory = TrackCategory.StreetAdventure),
                new MenuItem(LocalizationService.Mark("Custom track"), MenuAction.None, onActivate: () => OpenCustomTrackMenuOrAnnounce(mode)),
                new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, onActivate: () => PushRandomTrackType(mode)),
                BackItem()
            };
            return _menu.CreateMenu(id, items, LocalizationService.Mark("Choose track type"));
        }

        private MenuScreen BuildTrackMenu(string id, RaceMode mode, TrackCategory category)
        {
            var items = new List<MenuItem>();
            var trackList = TrackList.GetTracks(category);
            var nextMenuId = VehicleMenuId(mode);

            foreach (var track in trackList)
            {
                var key = track.Key;
                items.Add(new MenuItem(track.Display, MenuAction.None, nextMenuId: nextMenuId, onActivate: () => _selection.SelectTrack(category, key)));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, nextMenuId: nextMenuId, onActivate: () => _selection.SelectRandomTrack(category)));
            items.Add(BackItem());
            return _menu.CreateMenu(id, items, LocalizationService.Mark("Select a track"));
        }

        private MenuScreen BuildCustomTrackMenu(string id, RaceMode mode)
        {
            return _menu.CreateMenu(id, BuildCustomTrackItems(mode), LocalizationService.Mark("Select a custom track"));
        }

        private void RefreshCustomTrackMenu(RaceMode mode)
        {
            var id = TrackMenuId(mode, TrackCategory.CustomTrack);
            _menu.UpdateItems(id, BuildCustomTrackItems(mode));
        }

        private List<MenuItem> BuildCustomTrackItems(RaceMode mode)
        {
            var items = new List<MenuItem>();
            var nextMenuId = VehicleMenuId(mode);
            var customTracks = _selection.GetCustomTrackInfo();
            if (customTracks.Count == 0)
            {
                items.Add(BackItem());
                return items;
            }

            foreach (var track in customTracks)
            {
                var key = track.Key;
                items.Add(new MenuItem(track.Display, MenuAction.None, nextMenuId: nextMenuId,
                    onActivate: () => _selection.SelectTrack(TrackCategory.CustomTrack, key)));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, nextMenuId: nextMenuId, onActivate: _selection.SelectRandomCustomTrack));
            items.Add(BackItem());
            return items;
        }

        private MenuScreen BuildVehicleMenu(string id, RaceMode mode)
        {
            var items = new List<MenuItem>();

            for (var i = 0; i < VehicleCatalog.VehicleCount; i++)
            {
                var index = i;
                var name = VehicleCatalog.Vehicles[i].Name;
                items.Add(new MenuItem(name, MenuAction.None, onActivate: () => CompleteVehicleSelection(mode, () => _selection.SelectVehicle(index))));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Custom"), MenuAction.None, onActivate: () => OpenCustomVehicleMenuOrAnnounce(mode)));
            items.Add(new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, onActivate: () => CompleteVehicleSelection(mode, _selection.SelectRandomCustomVehicle)));
            items.Add(BackItem());
            return _menu.CreateMenu(id, items, LocalizationService.Mark("Select a vehicle"));
        }

        private MenuScreen BuildCustomVehicleMenu(string id, RaceMode mode)
        {
            return _menu.CreateMenu(id, BuildCustomVehicleItems(mode), LocalizationService.Mark("Select a custom vehicle"));
        }

        private void RefreshCustomVehicleMenu(RaceMode mode)
        {
            _menu.UpdateItems(CustomVehicleMenuId(mode), BuildCustomVehicleItems(mode));
        }

        private List<MenuItem> BuildCustomVehicleItems(RaceMode mode)
        {
            var items = new List<MenuItem>();
            var customVehicles = _selection.GetCustomVehicleInfo();
            if (customVehicles.Count == 0)
            {
                items.Add(BackItem());
                return items;
            }

            foreach (var vehicle in customVehicles)
            {
                var filePath = vehicle.Key;
                var displayName = string.IsNullOrWhiteSpace(vehicle.Display)
                    ? LocalizationService.Mark("Custom vehicle")
                    : vehicle.Display;
                items.Add(new MenuItem(displayName, MenuAction.None, onActivate: () => CompleteVehicleSelection(mode, () => _selection.SelectCustomVehicle(filePath))));
            }

            items.Add(new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, onActivate: () => CompleteVehicleSelection(mode, _selection.SelectRandomVehicle)));
            items.Add(BackItem());
            return items;
        }

        private MenuScreen BuildTransmissionMenu(string id, RaceMode mode)
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Automatic"), MenuAction.None, onActivate: () => CompleteTransmission(mode, TransmissionMode.Automatic)),
                new MenuItem(LocalizationService.Mark("Manual"), MenuAction.None, onActivate: () => CompleteTransmission(mode, TransmissionMode.Manual)),
                new MenuItem(LocalizationService.Mark("Random"), MenuAction.None, onActivate: () => CompleteTransmission(mode, PickRandomSupportedTransmissionMode())),
                BackItem()
            };
            return _menu.CreateMenu(id, items, LocalizationService.Mark("Select transmission mode"));
        }

        private TransmissionMode PickRandomSupportedTransmissionMode()
        {
            if (!TryGetSelectedTransmissionOptions(out _, out var supportedTypes))
                return TransmissionMode.Automatic;

            var supportsAutomatic = TransmissionSelect.SupportsAutomatic(supportedTypes);
            var supportsManual = TransmissionSelect.SupportsManual(supportedTypes);
            if (supportsAutomatic && supportsManual)
                return Algorithm.RandomInt(2) == 0 ? TransmissionMode.Automatic : TransmissionMode.Manual;
            if (supportsManual)
                return TransmissionMode.Manual;
            return TransmissionMode.Automatic;
        }

        private bool SupportsTransmissionMode(TransmissionMode mode)
        {
            if (!TryGetSelectedTransmissionOptions(out var primaryType, out var supportedTypes))
                return false;

            return TransmissionSelect.TryResolveRequested(
                automaticRequested: mode == TransmissionMode.Automatic,
                primary: primaryType,
                supported: supportedTypes,
                out _);
        }

        private void CompleteVehicleSelection(RaceMode mode, Action selectVehicle)
        {
            selectVehicle();
            if (TryResolveSingleTransmissionMode(out var forcedMode))
            {
                _setup.Transmission = forcedMode;
                _ui.SpeakMessage(forcedMode == TransmissionMode.Automatic
                    ? LocalizationService.Mark("Automatic transmission is required for this vehicle.")
                    : LocalizationService.Mark("Manual transmission is required for this vehicle."));
                _race.QueueRaceStart(mode);
                return;
            }

            _menu.Push(TransmissionMenuId(mode));
        }

        private bool TryResolveSingleTransmissionMode(out TransmissionMode mode)
        {
            mode = TransmissionMode.Automatic;
            if (!TryGetSelectedTransmissionOptions(out var primaryType, out var supportedTypes))
                return false;

            if (!TransmissionSelect.TryResolveSingleMode(primaryType, supportedTypes, out var automatic))
                return false;

            mode = automatic ? TransmissionMode.Automatic : TransmissionMode.Manual;
            return true;
        }

        private bool TryGetSelectedTransmissionOptions(out TransmissionType primaryType, out TransmissionType[] supportedTypes)
        {
            primaryType = TransmissionType.Atc;
            supportedTypes = new[] { TransmissionType.Atc };

            if (string.IsNullOrWhiteSpace(_setup.VehicleFile))
            {
                var index = _setup.VehicleIndex ?? 0;
                index = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, index));
                var vehicle = VehicleCatalog.Vehicles[index];
                primaryType = vehicle.PrimaryTransmissionType;
                supportedTypes = vehicle.SupportedTransmissionTypes ?? supportedTypes;
                return true;
            }

            if (!VehicleTsvParser.TryLoadFromFile(_setup.VehicleFile!, out var parsed, out _))
                return false;

            primaryType = parsed.PrimaryTransmissionType;
            supportedTypes = parsed.SupportedTransmissionTypes;
            return true;
        }

        private void PushRandomTrackType(RaceMode mode)
        {
            var customTracks = _selection.GetCustomTrackInfo();
            var roll = Algorithm.RandomInt(customTracks.Count > 0 ? 3 : 2);
            var category = roll switch
            {
                0 => TrackCategory.RaceTrack,
                1 => TrackCategory.StreetAdventure,
                _ => TrackCategory.CustomTrack
            };

            _setup.TrackCategory = category;
            if (category == TrackCategory.CustomTrack)
                RefreshCustomTrackMenu(mode);
            _menu.Push(TrackMenuId(mode, category));
        }

        private void OpenCustomTrackMenuOrAnnounce(RaceMode mode)
        {
            var customTracks = _selection.GetCustomTrackInfo();
            var issues = _selection.ConsumeCustomTrackIssues();
            if (customTracks.Count == 0)
            {
                if (issues.Count > 0)
                {
                    _ui.ShowMessageDialog(
                        LocalizationService.Mark("Custom track errors"),
                        LocalizationService.Mark("Some custom track files are invalid and were skipped."),
                        issues);
                }
                else
                {
                    _ui.SpeakMessage(LocalizationService.Mark("No custom tracks found."));
                }
                return;
            }

            _setup.TrackCategory = TrackCategory.CustomTrack;
            RefreshCustomTrackMenu(mode);
            _menu.Push(TrackMenuId(mode, TrackCategory.CustomTrack));

            if (issues.Count > 0)
            {
                _ui.ShowMessageDialog(
                    LocalizationService.Mark("Custom track errors"),
                    LocalizationService.Mark("Some custom track files are invalid and were skipped."),
                    issues);
            }
        }

        private void OpenCustomVehicleMenuOrAnnounce(RaceMode mode)
        {
            var customVehicles = _selection.GetCustomVehicleInfo();
            var issues = _selection.ConsumeCustomVehicleIssues();
            if (customVehicles.Count == 0)
            {
                if (issues.Count > 0)
                {
                    _ui.ShowMessageDialog(
                        LocalizationService.Mark("Custom vehicle errors"),
                        LocalizationService.Mark("Some custom vehicle files are invalid and were skipped."),
                        issues);
                }
                else
                {
                    _ui.SpeakMessage(LocalizationService.Mark("No custom vehicles found."));
                }
                return;
            }

            RefreshCustomVehicleMenu(mode);
            _menu.Push(CustomVehicleMenuId(mode));

            if (issues.Count > 0)
            {
                _ui.ShowMessageDialog(
                    LocalizationService.Mark("Custom vehicle errors"),
                    LocalizationService.Mark("Some custom vehicle files are invalid and were skipped."),
                    issues);
            }
        }

        private static string TrackMenuId(RaceMode mode, TrackCategory category)
        {
            var prefix = mode == RaceMode.TimeTrial ? "time_trial" : "single_race";
            return category switch
            {
                TrackCategory.RaceTrack => $"{prefix}_tracks_race",
                TrackCategory.StreetAdventure => $"{prefix}_tracks_adventure",
                _ => $"{prefix}_tracks_custom"
            };
        }

        private static string VehicleMenuId(RaceMode mode)
        {
            return mode == RaceMode.TimeTrial ? "time_trial_vehicles" : "single_race_vehicles";
        }

        private static string CustomVehicleMenuId(RaceMode mode)
        {
            return mode == RaceMode.TimeTrial ? "time_trial_vehicles_custom" : "single_race_vehicles_custom";
        }

        private static string TransmissionMenuId(RaceMode mode)
        {
            return mode == RaceMode.TimeTrial ? "time_trial_transmission" : "single_race_transmission";
        }

        private static MenuItem BackItem()
        {
            return new MenuItem(LocalizationService.Mark("Go back"), MenuAction.Back);
        }
    }
}





