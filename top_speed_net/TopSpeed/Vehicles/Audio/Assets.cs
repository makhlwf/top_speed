using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Common;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private AudioSourceHandle CreateRequiredSound(string? path, bool looped = false, bool spatialize = true, bool allowHrtf = true)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Sound path not provided.");
            if (!File.Exists(path))
                throw new FileNotFoundException("Sound file not found.", path);
            if (!spatialize)
            {
                return looped
                    ? _audio.CreateLoopingSource(path!, useHrtf: false)
                    : _audio.CreateSource(path!, streamFromDisk: true, useHrtf: false);
            }

            return looped
                ? _audio.CreateLoopingSpatialSource(path!, allowHrtf: allowHrtf)
                : _audio.CreateSpatialSource(path!, streamFromDisk: true, allowHrtf: allowHrtf);
        }

        private AudioSourceHandle[] CreateRequiredSoundVariants(IReadOnlyList<string>? paths, string? fallbackSinglePath)
        {
            if (paths != null && paths.Count > 0)
            {
                var result = new AudioSourceHandle[paths.Count];
                for (var i = 0; i < paths.Count; i++)
                    result[i] = CreateRequiredSound(paths[i]);
                return result;
            }

            return new[] { CreateRequiredSound(fallbackSinglePath) };
        }

        private AudioSourceHandle[] CreateOptionalSoundVariants(IReadOnlyList<string>? paths, string? fallbackSinglePath)
        {
            if (paths != null && paths.Count > 0)
            {
                var items = new List<AudioSourceHandle>();
                for (var i = 0; i < paths.Count; i++)
                {
                    var sound = TryCreateSound(paths[i]);
                    if (sound != null)
                        items.Add(sound);
                }
                return items.ToArray();
            }

            var single = TryCreateSound(fallbackSinglePath);
            return single == null ? Array.Empty<AudioSourceHandle>() : new[] { single };
        }

        private AudioSourceHandle SelectRandomCrashHandle()
        {
            if (_soundCrashVariants.Length == 0)
                return _soundCrash;
            return _soundCrashVariants[Algorithm.RandomInt(_soundCrashVariants.Length)];
        }

        private bool AnyBackfirePlaying()
        {
            for (var i = 0; i < _soundBackfireVariants.Length; i++)
            {
                if (_soundBackfireVariants[i].IsPlaying)
                    return true;
            }
            return false;
        }

        private void PlayRandomBackfire()
        {
            if (_soundBackfireVariants.Length == 0)
                return;
            _soundBackfire = _soundBackfireVariants[Algorithm.RandomInt(_soundBackfireVariants.Length)];
            _soundBackfire.Play(loop: false);
        }

        private void StopResetBackfireVariants()
        {
            for (var i = 0; i < _soundBackfireVariants.Length; i++)
            {
                if (_soundBackfireVariants[i].IsPlaying)
                    _soundBackfireVariants[i].Stop();
                _soundBackfireVariants[i].SeekToStart();
            }
        }

        private static void DisposeSoundVariants(AudioSourceHandle[] sounds)
        {
            for (var i = 0; i < sounds.Length; i++)
            {
                sounds[i].Stop();
                sounds[i].Dispose();
            }
        }

        private AudioSourceHandle? TryCreateSound(string? path, bool looped = false, bool spatialize = true, bool allowHrtf = true)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;
            if (!spatialize)
            {
                return looped
                    ? _audio.CreateLoopingSource(path!, useHrtf: false)
                    : _audio.CreateSource(path!, streamFromDisk: true, useHrtf: false);
            }

            return looped
                ? _audio.CreateLoopingSpatialSource(path!, allowHrtf: allowHrtf)
                : _audio.CreateSpatialSource(path!, streamFromDisk: true, allowHrtf: allowHrtf);
        }
    }
}

