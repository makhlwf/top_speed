using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Data;
using TS.Audio;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        private sealed class RuntimeTrackSound
        {
            private const float DefaultRandomCrossfadeSeconds = 0.75f;
            private readonly AudioManager _audio;
            private readonly string _sourceRootFullPath;
            private readonly Random _random;
            private readonly IReadOnlyDictionary<string, TrackSoundSourceDefinition> _soundDefinitions;
            private readonly Action<AudioSourceHandle, float> _enqueueFadeOut;
            private AudioSourceHandle? _handle;
            private string? _selectedPath;

            public RuntimeTrackSound(
                AudioManager audio,
                string sourceDirectory,
                Random random,
                IReadOnlyDictionary<string, TrackSoundSourceDefinition> soundDefinitions,
                Action<AudioSourceHandle, float> enqueueFadeOut,
                string id,
                TrackSoundSourceDefinition definition)
            {
                _audio = audio;
                _sourceRootFullPath = Path.GetFullPath(sourceDirectory);
                _random = random;
                _soundDefinitions = soundDefinitions;
                _enqueueFadeOut = enqueueFadeOut;
                Id = id;
                Definition = definition;
                ActiveDefinition = definition;
                LastAreaIndex = -1;
            }

            public string Id { get; }
            public TrackSoundSourceDefinition Definition { get; private set; }
            public TrackSoundSourceDefinition ActiveDefinition { get; private set; }
            public AudioSourceHandle? Handle => _handle;
            public int LastAreaIndex { get; set; }
            public bool TriggerActive { get; set; }
            public bool TriggerInitialized { get; set; }

            public void UpdateDefinition(TrackSoundSourceDefinition definition)
            {
                Definition = definition;
                ActiveDefinition = definition;
            }

            public bool EnsureCreated(bool refreshRandomVariant, float categoryScale)
            {
                var selection = SelectVariant(refreshRandomVariant);
                if (!selection.HasValue)
                    return false;

                var activeDefinition = selection.Value.Definition;
                var path = selection.Value.Path;
                if (_handle != null && !refreshRandomVariant && string.Equals(path, _selectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    ActiveDefinition = activeDefinition;
                    return true;
                }

                var previousHandle = _handle;
                _selectedPath = path;
                ActiveDefinition = activeDefinition;

                _handle = activeDefinition.Spatial
                    ? _audio.CreateSpatialSource(path, streamFromDisk: true, allowHrtf: activeDefinition.AllowHrtf)
                    : _audio.CreateSource(path, streamFromDisk: true, useHrtf: false);
                ApplySourceSettings(_handle, activeDefinition, categoryScale);

                if (previousHandle != null)
                    DisposePreviousHandle(previousHandle, refreshRandomVariant);

                if (_handle != null)
                    _handle.SeekToStart();

                return true;
            }

            public void ApplyCategoryVolume(float categoryScale)
            {
                if (_handle == null)
                    return;

                var scale = Clamp01(categoryScale);
                var value = Clamp01(ActiveDefinition.Volume * scale);
                _handle.SetVolume(value);
            }

            public void Play()
            {
                if (_handle == null)
                    return;

                if (_handle.IsPlaying)
                    return;

                if (ActiveDefinition.FadeInSeconds > 0f)
                    _handle.Play(ActiveDefinition.Loop, ActiveDefinition.FadeInSeconds);
                else
                    _handle.Play(ActiveDefinition.Loop);
            }

            public void Stop()
            {
                if (_handle == null)
                    return;

                if (ActiveDefinition.FadeOutSeconds > 0f)
                    _handle.Stop(ActiveDefinition.FadeOutSeconds);
                else
                    _handle.Stop();
            }

            public void Dispose()
            {
                DisposeHandle();
            }

            private (TrackSoundSourceDefinition Definition, string Path)? SelectVariant(bool refreshRandomVariant)
            {
                if (!refreshRandomVariant && !string.IsNullOrWhiteSpace(_selectedPath))
                    return (ActiveDefinition, _selectedPath!);

                var variants = BuildVariantCandidates();
                if (variants.Count == 0)
                    return null;

                if (Definition.Type == TrackSoundSourceType.Random && variants.Count > 1)
                {
                    var index = _random.Next(variants.Count);
                    return variants[index];
                }

                return variants[0];
            }

            private List<(TrackSoundSourceDefinition Definition, string Path)> BuildVariantCandidates()
            {
                var resolved = new List<(TrackSoundSourceDefinition Definition, string Path)>();
                if (!string.IsNullOrWhiteSpace(Definition.Path))
                {
                    var path = ResolveSoundPath(Definition.Path!);
                    if (path != null)
                        resolved.Add((Definition, path));
                }

                for (var i = 0; i < Definition.VariantPaths.Count; i++)
                {
                    var path = ResolveSoundPath(Definition.VariantPaths[i]);
                    if (path != null)
                        resolved.Add((Definition, path));
                }

                for (var i = 0; i < Definition.VariantSourceIds.Count; i++)
                {
                    var sourceId = Definition.VariantSourceIds[i];
                    if (string.IsNullOrWhiteSpace(sourceId))
                        continue;

                    if (!_soundDefinitions.TryGetValue(sourceId, out var sourceDefinition))
                        continue;

                    if (sourceDefinition.Type == TrackSoundSourceType.Random || string.IsNullOrWhiteSpace(sourceDefinition.Path))
                        continue;

                    var path = ResolveSoundPath(sourceDefinition.Path!);
                    if (path != null)
                        resolved.Add((sourceDefinition, path));
                }

                return resolved;
            }

            private static void ApplySourceSettings(AudioSourceHandle handle, TrackSoundSourceDefinition definition, float categoryScale)
            {
                var scale = Clamp01(categoryScale);
                handle.SetVolume(Clamp01(definition.Volume * scale));
                handle.SetPitch(definition.Pitch);
                handle.SetPan(definition.Pan);

                if (definition.MinDistance.HasValue ||
                    definition.MaxDistance.HasValue ||
                    definition.Rolloff.HasValue ||
                    definition.StartRadiusMeters.HasValue ||
                    definition.EndRadiusMeters.HasValue)
                {
                    var minDistance = definition.MinDistance ?? definition.StartRadiusMeters ?? 1.0f;
                    var maxDistance = definition.MaxDistance ?? definition.EndRadiusMeters ?? 10000f;
                    var rolloff = definition.Rolloff ?? 1.0f;
                    handle.SetDistanceModel(DistanceModel.Inverse, minDistance, maxDistance, rolloff);
                }
            }

            private static float Clamp01(float value)
            {
                if (value <= 0f)
                    return 0f;
                if (value >= 1f)
                    return 1f;
                return value;
            }

            private string? ResolveSoundPath(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    return null;

                var trimmed = path.Trim();
                if (Path.IsPathRooted(trimmed))
                    return null;

                var normalized = trimmed
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .TrimStart(Path.DirectorySeparatorChar);
                if (normalized.IndexOf(':') >= 0 || ContainsTraversal(normalized))
                    return null;

                var candidate = Path.GetFullPath(Path.Combine(_sourceRootFullPath, normalized));
                if (!IsInsideTrackRoot(candidate))
                    return null;

                return File.Exists(candidate) ? candidate : null;
            }

            private bool IsInsideTrackRoot(string candidate)
            {
                if (string.Equals(candidate, _sourceRootFullPath, StringComparison.OrdinalIgnoreCase))
                    return true;

                var rootWithSeparator = _sourceRootFullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
            }

            private static bool ContainsTraversal(string path)
            {
                var parts = path.Split(Path.DirectorySeparatorChar);
                for (var i = 0; i < parts.Length; i++)
                {
                    var segment = parts[i].Trim();
                    if (segment == "." || segment == "..")
                        return true;
                }

                return false;
            }

            private void DisposeHandle()
            {
                if (_handle == null)
                    return;
                _handle.Stop();
                _handle.Dispose();
                _handle = null;
            }

            private void DisposePreviousHandle(AudioSourceHandle previousHandle, bool refreshRandomVariant)
            {
                var fadeOutSeconds = 0f;
                if (refreshRandomVariant && Definition.Type == TrackSoundSourceType.Random)
                {
                    fadeOutSeconds = Definition.CrossfadeSeconds.HasValue
                        ? Math.Max(0f, Definition.CrossfadeSeconds.Value)
                        : DefaultRandomCrossfadeSeconds;
                }

                if (fadeOutSeconds > 0f && previousHandle.IsPlaying)
                {
                    previousHandle.Stop(fadeOutSeconds);
                    _enqueueFadeOut(previousHandle, fadeOutSeconds);
                    return;
                }

                previousHandle.Stop();
                previousHandle.Dispose();
            }
        }

        private sealed class PendingHandleStop
        {
            public PendingHandleStop(AudioSourceHandle handle, DateTime disposeAtUtc)
            {
                Handle = handle;
                DisposeAtUtc = disposeAtUtc;
            }

            public AudioSourceHandle Handle { get; }
            public DateTime DisposeAtUtc { get; }
        }
    }
}

