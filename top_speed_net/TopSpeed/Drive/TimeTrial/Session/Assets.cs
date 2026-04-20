using System;
using System.IO;
using TopSpeed.Common;
using TopSpeed.Core;
using TS.Audio;

namespace TopSpeed.Drive.TimeTrial
{
    internal sealed partial class TimeTrialSession
    {
        private enum RandomSoundSlot
        {
            EasyLeft = 0,
            Left = 1,
            HardLeft = 2,
            HairpinLeft = 3,
            EasyRight = 4,
            Right = 5,
            HardRight = 6,
            HairpinRight = 7,
            Asphalt = 8,
            Gravel = 9,
            Water = 10,
            Sand = 11,
            Snow = 12,
            Finish = 13
        }

        private void LoadDefaultRandomSounds()
        {
            LoadRandomSounds(RandomSoundSlot.EasyLeft, "race\\copilot\\easyleft");
            LoadRandomSounds(RandomSoundSlot.Left, "race\\copilot\\left");
            LoadRandomSounds(RandomSoundSlot.HardLeft, "race\\copilot\\hardleft");
            LoadRandomSounds(RandomSoundSlot.HairpinLeft, "race\\copilot\\hairpinleft");
            LoadRandomSounds(RandomSoundSlot.EasyRight, "race\\copilot\\easyright");
            LoadRandomSounds(RandomSoundSlot.Right, "race\\copilot\\right");
            LoadRandomSounds(RandomSoundSlot.HardRight, "race\\copilot\\hardright");
            LoadRandomSounds(RandomSoundSlot.HairpinRight, "race\\copilot\\hairpinright");
            LoadRandomSounds(RandomSoundSlot.Asphalt, "race\\copilot\\asphalt");
            LoadRandomSounds(RandomSoundSlot.Gravel, "race\\copilot\\gravel");
            LoadRandomSounds(RandomSoundSlot.Water, "race\\copilot\\water");
            LoadRandomSounds(RandomSoundSlot.Sand, "race\\copilot\\sand");
            LoadRandomSounds(RandomSoundSlot.Snow, "race\\copilot\\snow");
            LoadRandomSounds(RandomSoundSlot.Finish, "race\\info\\finish");
        }

        private void LoadRandomSounds(RandomSoundSlot slot, string baseName)
        {
            var first = $"{baseName}1";
            _randomSounds[(int)slot][0] = LoadLanguageSound(first);
            _totalRandomSounds[(int)slot] = 1;

            for (var i = 1; i < RandomSoundMax; i++)
            {
                var sound = TryLoadLanguageSound($"{baseName}{i + 1}", allowFallback: false);
                _randomSounds[(int)slot][i] = sound;
                if (sound == null)
                {
                    _totalRandomSounds[(int)slot] = i;
                    break;
                }
            }
        }

        private Source LoadLanguageSound(string key, bool streamFromDisk = true)
        {
            var sound = TryLoadLanguageSound(key, allowFallback: true, streamFromDisk: streamFromDisk);
            if (sound != null)
                return sound;

            var errorPath = AssetPaths.ResolveLegacySoundPath("error.wav");
            if (errorPath != null)
                return LoadBusSource(errorPath, AudioEngineOptions.CopilotBusName, streamFromDisk: true);

            throw new FileNotFoundException($"Missing language sound {key}.");
        }

        private Source? TryLoadLanguageSound(string key, bool allowFallback, bool streamFromDisk = true)
        {
            var path = allowFallback
                ? AssetPaths.ResolveLanguageSoundPathWithFallback(_settings.Language, key)
                : AssetPaths.ResolveLanguageSoundPath(_settings.Language, key);
            if (path != null)
                return LoadBusSource(path, AudioEngineOptions.CopilotBusName, streamFromDisk);

            return null;
        }

        private Source LoadLanguageMusicSound(string key, bool streamFromDisk)
        {
            var path = AssetPaths.ResolveLanguageSoundPathWithFallback(_settings.Language, key);
            if (path == null)
                throw new FileNotFoundException($"Missing language sound {key}.");

            return LoadBusSource(path, AudioEngineOptions.MusicBusName, streamFromDisk);
        }

        private Source LoadLegacySound(string fileName)
        {
            var path = AssetPaths.ResolveLegacySoundPath(fileName);
            if (path == null)
                throw new FileNotFoundException($"Missing legacy sound {fileName}.");

            return LoadBusSource(path, AudioEngineOptions.CopilotBusName, streamFromDisk: true);
        }

        private Source LoadBusSource(string path, string busName, bool streamFromDisk)
        {
            var asset = _audio.LoadAsset(path, streamFromDisk);
            return streamFromDisk
                ? _audio.CreateSource(asset, busName)
                : _audio.CreateLoopingSource(asset, busName);
        }
    }
}
