using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;

namespace TopSpeed.Core
{
    internal sealed class RaceSelection
    {
        private readonly RaceSetup _setup;
        private readonly RaceSettings _settings;
        private readonly Dictionary<string, (DateTime LastWriteUtc, string Display)> _customTrackCache =
            new Dictionary<string, (DateTime LastWriteUtc, string Display)>(StringComparer.OrdinalIgnoreCase);

        public RaceSelection(RaceSetup setup, RaceSettings settings)
        {
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void SelectTrack(TrackCategory category, string trackKey)
        {
            _setup.TrackCategory = category;
            _setup.TrackNameOrFile = trackKey;
        }

        public void SelectRandomTrack(TrackCategory category)
        {
            SelectRandomTrack(category, _settings.RandomCustomTracks);
        }

        public void SelectRandomTrack(TrackCategory category, bool includeCustom)
        {
            if (category == TrackCategory.CustomTrack)
            {
                SelectRandomCustomTrack();
                return;
            }
            var customTracks = includeCustom ? GetCustomTrackFiles() : Array.Empty<string>();
            _setup.TrackCategory = category;
            _setup.TrackNameOrFile = TrackList.GetRandomTrackKey(category, customTracks);
        }

        public void SelectRandomTrackAny(bool includeCustom)
        {
            var customTracks = includeCustom ? GetCustomTrackFiles() : Array.Empty<string>();
            var pick = TrackList.GetRandomTrackAny(customTracks);
            _setup.TrackCategory = pick.Category;
            _setup.TrackNameOrFile = pick.Key;
        }

        public void SelectRandomCustomTrack()
        {
            var customTracks = GetCustomTrackFiles().ToList();
            if (customTracks.Count == 0)
            {
                SelectTrack(TrackCategory.RaceTrack, TrackList.RaceTracks[0].Key);
                return;
            }

            var index = Algorithm.RandomInt(customTracks.Count);
            SelectTrack(TrackCategory.CustomTrack, customTracks[index]);
        }

        public void SelectVehicle(int index)
        {
            _setup.VehicleIndex = index;
            _setup.VehicleFile = null;
        }

        public void SelectCustomVehicle(string file)
        {
            _setup.VehicleIndex = null;
            _setup.VehicleFile = file;
        }

        public void SelectRandomVehicle()
        {
            var customFiles = _settings.RandomCustomVehicles ? GetCustomVehicleFiles().ToList() : new List<string>();
            var total = VehicleCatalog.VehicleCount + customFiles.Count;
            if (total <= 0)
            {
                SelectVehicle(0);
                return;
            }

            var roll = Algorithm.RandomInt(total);
            if (roll < VehicleCatalog.VehicleCount)
            {
                SelectVehicle(roll);
                return;
            }

            var customIndex = roll - VehicleCatalog.VehicleCount;
            if (customIndex >= 0 && customIndex < customFiles.Count)
                SelectCustomVehicle(customFiles[customIndex]);
            else
                SelectVehicle(0);
        }

        public IEnumerable<string> GetCustomTrackFiles()
        {
            var root = Path.Combine(AssetPaths.Root, "Tracks");
            if (!Directory.Exists(root))
                return Array.Empty<string>();

            var trackFiles = new List<string>();
            foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
            {
                var firstTrack = Directory.EnumerateFiles(directory, "*.tsm", SearchOption.TopDirectoryOnly)
                    .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(firstTrack))
                    trackFiles.Add(firstTrack);
            }

            return trackFiles;
        }

        public IReadOnlyList<TrackInfo> GetCustomTrackInfo()
        {
            var files = GetCustomTrackFiles().ToList();
            if (files.Count == 0)
            {
                _customTrackCache.Clear();
                return Array.Empty<TrackInfo>();
            }

            var items = new List<TrackInfo>(files.Count);
            var known = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                var display = ResolveCustomTrackDisplayName(file);
                items.Add(new TrackInfo(file, display));
            }

            var staleKeys = _customTrackCache.Keys.Where(key => !known.Contains(key)).ToList();
            foreach (var key in staleKeys)
                _customTrackCache.Remove(key);

            return items
                .OrderBy(item => item.Display, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IEnumerable<string> GetCustomVehicleFiles()
        {
            var root = Path.Combine(AssetPaths.Root, "Vehicles");
            if (!Directory.Exists(root))
                return Array.Empty<string>();
            return Directory.EnumerateFiles(root, "*.vhc", SearchOption.TopDirectoryOnly);
        }

        private string ResolveCustomTrackDisplayName(string file)
        {
            var display = TryReadCustomTrackName(file);
            if (string.IsNullOrWhiteSpace(display))
                display = GetTrackFolderName(file);
            return string.IsNullOrWhiteSpace(display) ? "Custom track" : display!;
        }

        private string? TryReadCustomTrackName(string file)
        {
            try
            {
                var lastWrite = File.GetLastWriteTimeUtc(file);
                if (_customTrackCache.TryGetValue(file, out var cached) && cached.LastWriteUtc == lastWrite)
                    return cached.Display;

                string? parsed = null;
                if (TrackTsmParser.TryLoad(file, out var data))
                    parsed = data.Name;
                var display = string.IsNullOrWhiteSpace(parsed)
                    ? GetTrackFolderName(file)
                    : parsed;

                display = string.IsNullOrWhiteSpace(display) ? "Custom track" : display!;
                _customTrackCache[file] = (lastWrite, display);
                return display;
            }
            catch
            {
                return null;
            }
        }

        private static string GetTrackFolderName(string file)
        {
            var directory = Path.GetDirectoryName(file);
            if (string.IsNullOrWhiteSpace(directory))
                return Path.GetFileNameWithoutExtension(file);
            var name = Path.GetFileName(directory);
            return string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(file) : name;
        }
    }
}
