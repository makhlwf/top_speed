using System;
using System.IO;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Tracks;
using TopSpeed.Vehicles.Parsing;

namespace TopSpeed.Vehicles.Loader
{
    internal static class Custom
    {
        public static VehicleDefinition Load(string vehicleFile, TrackWeather weather)
        {
            var filePath = Path.IsPathRooted(vehicleFile)
                ? vehicleFile
                : Path.Combine(AssetPaths.Root, vehicleFile);
            var builtinRoot = Path.Combine(AssetPaths.SoundsRoot, "Vehicles");

            if (!VehicleTsvParser.TryLoadFromFile(filePath, out var parsed, out var issues))
            {
                var message = issues == null || issues.Count == 0
                    ? LocalizationService.Mark("Unknown parse error.")
                    : string.Join(" ", issues);
                throw new InvalidDataException(LocalizationService.Format(
                    LocalizationService.Mark("Failed to load custom vehicle '{0}'. {1}"),
                    filePath,
                    message));
            }

            var spec = Spec.FromCustom(parsed, weather);
            var def = new VehicleDefinition
            {
                CarType = CarType.Vehicle1,
                Name = parsed.Meta.Name,
                UserDefined = true,
                CustomFile = Path.GetFileNameWithoutExtension(filePath),
                CustomVersion = parsed.Meta.Version,
                CustomDescription = parsed.Meta.Description
            };
            Spec.Apply(def, spec);

            def.SetSoundPath(VehicleAction.Engine, Sound.ResolveCustom(parsed.Sounds.Engine, builtinRoot, parsed.SourceDirectory, VehicleAction.Engine));
            def.SetSoundPath(VehicleAction.Start, Sound.ResolveCustom(parsed.Sounds.Start, builtinRoot, parsed.SourceDirectory, VehicleAction.Start));
            if (!string.IsNullOrWhiteSpace(parsed.Sounds.Stop))
                def.SetSoundPath(VehicleAction.Stop, Sound.ResolveCustom(parsed.Sounds.Stop!, builtinRoot, parsed.SourceDirectory, VehicleAction.Stop));
            def.SetSoundPath(VehicleAction.Horn, Sound.ResolveCustom(parsed.Sounds.Horn, builtinRoot, parsed.SourceDirectory, VehicleAction.Horn));
            if (!string.IsNullOrWhiteSpace(parsed.Sounds.Throttle))
                def.SetSoundPath(VehicleAction.Throttle, Sound.ResolveCustom(parsed.Sounds.Throttle!, builtinRoot, parsed.SourceDirectory, VehicleAction.Throttle));
            def.SetSoundPath(VehicleAction.Brake, Sound.ResolveCustom(parsed.Sounds.Brake, builtinRoot, parsed.SourceDirectory, VehicleAction.Brake));
            def.SetSoundPaths(VehicleAction.Crash, Sound.ResolveCustomList(parsed.Sounds.CrashVariants, builtinRoot, parsed.SourceDirectory, VehicleAction.Crash));
            if (parsed.Sounds.BackfireVariants != null && parsed.Sounds.BackfireVariants.Count > 0)
                def.SetSoundPaths(VehicleAction.Backfire, Sound.ResolveCustomList(parsed.Sounds.BackfireVariants, builtinRoot, parsed.SourceDirectory, VehicleAction.Backfire));

            return def;
        }
    }
}

