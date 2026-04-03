using System;
using System.IO;
using TopSpeed.Core;
using TS.Audio;

namespace TopSpeed.Race
{
    internal abstract partial class RaceMode
    {
        protected void LoadRandomSounds(RandomSound pos, string baseName)
        {
            var first = $"{baseName}1";
            _randomSounds[(int)pos][0] = LoadLanguageSound(first);
            _totalRandomSounds[(int)pos] = 1;

            for (var i = 1; i < RandomSoundMax; i++)
            {
                var name = $"{baseName}{i + 1}";
                var sound = TryLoadLanguageSound(name, allowFallback: false);
                _randomSounds[(int)pos][i] = sound;
                if (sound == null)
                {
                    _totalRandomSounds[(int)pos] = i;
                    break;
                }
            }
        }

        protected AudioSourceHandle LoadLanguageSound(string key, bool streamFromDisk = true)
        {
            var sound = TryLoadLanguageSound(key, allowFallback: true, streamFromDisk: streamFromDisk);
            if (sound != null)
                return sound;
            var errorPath = GetLegacySoundPath("error.wav");
            if (errorPath != null)
                return _audio.CreateSource(errorPath, streamFromDisk: true);
            throw new FileNotFoundException($"Missing language sound {key}.");
        }

        protected AudioSourceHandle? TryLoadLanguageSound(string key, bool allowFallback, bool streamFromDisk = true)
        {
            var path = ResolveLanguageSoundPath(_settings.Language, key);
            if (path != null)
                return streamFromDisk
                    ? _audio.CreateSource(path, streamFromDisk: true)
                    : _audio.CreateLoopingSource(path);

            if (allowFallback && !string.Equals(_settings.Language, "en", StringComparison.OrdinalIgnoreCase))
            {
                path = ResolveLanguageSoundPath("en", key);
                if (path != null)
                    return streamFromDisk
                        ? _audio.CreateSource(path, streamFromDisk: true)
                        : _audio.CreateLoopingSource(path);
            }
            return null;
        }

        protected AudioSourceHandle LoadLegacySound(string fileName)
        {
            var path = GetLegacySoundPath(fileName);
            if (path == null)
                throw new FileNotFoundException($"Missing legacy sound {fileName}.");
            return _audio.CreateSource(path, streamFromDisk: true);
        }

        private string? ResolveLanguageSoundPath(string language, string key)
        {
            var relative = key.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(Path.GetExtension(relative)))
                relative += ".ogg";
            var path = Path.Combine(AssetPaths.SoundsRoot, language, relative);
            return File.Exists(path) ? path : null;
        }

        private string? GetLegacySoundPath(string fileName)
        {
            var path = Path.Combine(AssetPaths.SoundsRoot, "Legacy", fileName);
            return File.Exists(path) ? path : null;
        }

        private AudioSourceHandle? LoadTrackNameSound(string trackName)
        {
            switch (trackName)
            {
                case "america":
                    return LoadLanguageSound("tracks\\america");
                case "austria":
                    return LoadLanguageSound("tracks\\austria");
                case "belgium":
                    return LoadLanguageSound("tracks\\belgium");
                case "brazil":
                    return LoadLanguageSound("tracks\\brazil");
                case "china":
                    return LoadLanguageSound("tracks\\china");
                case "england":
                    return LoadLanguageSound("tracks\\england");
                case "finland":
                    return LoadLanguageSound("tracks\\finland");
                case "france":
                    return LoadLanguageSound("tracks\\france");
                case "germany":
                    return LoadLanguageSound("tracks\\germany");
                case "ireland":
                    return LoadLanguageSound("tracks\\ireland");
                case "italy":
                    return LoadLanguageSound("tracks\\italy");
                case "netherlands":
                    return LoadLanguageSound("tracks\\netherlands");
                case "portugal":
                    return LoadLanguageSound("tracks\\portugal");
                case "russia":
                    return LoadLanguageSound("tracks\\russia");
                case "spain":
                    return LoadLanguageSound("tracks\\spain");
                case "sweden":
                    return LoadLanguageSound("tracks\\sweden");
                case "switserland":
                    return LoadLanguageSound("tracks\\switserland");
                case "advHills":
                    return LoadLanguageSound("tracks\\rallyhills");
                case "advCoast":
                    return LoadLanguageSound("tracks\\frenchcoast");
                case "advCountry":
                    return LoadLanguageSound("tracks\\englishcountry");
                case "advAirport":
                    return LoadLanguageSound("tracks\\rideairport");
                case "advDesert":
                    return LoadLanguageSound("tracks\\rallydesert");
                case "advRush":
                    return LoadLanguageSound("tracks\\rushhour");
                case "advEscape":
                    return LoadLanguageSound("tracks\\polarescape");
                case "custom":
                    return LoadLanguageSound("menu\\customtrack");
            }

            var baseName = trackName;
            var directory = string.Empty;
            if (trackName.IndexOfAny(new[] { '\\', '/' }) >= 0)
            {
                directory = Path.GetDirectoryName(trackName) ?? string.Empty;
                baseName = Path.GetFileNameWithoutExtension(trackName);
            }
            else if (trackName.Length > 4)
            {
                baseName = trackName.Substring(0, trackName.Length - 4);
            }

            if (!baseName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                baseName += ".wav";

            var candidate = string.IsNullOrWhiteSpace(directory)
                ? Path.Combine(AppContext.BaseDirectory, baseName)
                : Path.Combine(directory, baseName);
            if (File.Exists(candidate))
                return _audio.CreateSource(candidate, streamFromDisk: true);

            var fallback = GetLegacySoundPath("error.wav");
            return fallback != null ? _audio.CreateSource(fallback, streamFromDisk: true) : null;
        }
    }
}


