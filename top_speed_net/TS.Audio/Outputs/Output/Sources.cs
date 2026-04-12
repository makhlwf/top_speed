using System;
using System.Collections.Generic;
using System.Linq;

namespace TS.Audio
{
    public sealed partial class AudioOutput
    {
        public AudioSourceHandle CreateSource(string filePath, bool streamFromDisk = true, bool useHrtf = true)
        {
            return CreateSource(new FileAsset(filePath, streamFromDisk), spatialize: useHrtf, useHrtf: useHrtf, bus: null, ownsAsset: true);
        }

        public Source CreateSource(SoundAsset asset, bool spatialize = false, bool useHrtf = false)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            return new Source(CreateSource(asset.Asset, spatialize, useHrtf, bus: null, ownsAsset: false), ownsHandle: true);
        }

        public AudioSourceHandle CreateSpatialSource(string filePath, bool streamFromDisk = true, bool allowHrtf = true)
        {
            return CreateSource(new FileAsset(filePath, streamFromDisk), spatialize: true, useHrtf: allowHrtf, bus: null, ownsAsset: true);
        }

        public Source CreateSpatialSource(SoundAsset asset, bool allowHrtf = true)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            return new Source(CreateSource(asset.Asset, spatialize: true, useHrtf: allowHrtf, bus: null, ownsAsset: false), ownsHandle: true);
        }

        public AudioSourceHandle CreateProceduralSource(ProceduralAudioCallback callback, uint channels = 1, uint sampleRate = 44100, bool useHrtf = true)
        {
            return CreateSource(new ProceduralAsset(callback, channels, sampleRate), spatialize: useHrtf, useHrtf: useHrtf, bus: null, ownsAsset: true);
        }

        public Source CreateProceduralOwnedSource(ProceduralAudioCallback callback, uint channels = 1, uint sampleRate = 44100, bool spatialize = false, bool useHrtf = false)
        {
            return new Source(CreateSource(new ProceduralAsset(callback, channels, sampleRate), spatialize, useHrtf, bus: null, ownsAsset: true), ownsHandle: true);
        }

        internal AudioSourceHandle CreateSource(AudioAsset asset, bool spatialize, bool useHrtf, AudioBus? bus, bool ownsAsset)
        {
            ThrowIfDisposed();

            var source = new AudioSourceHandle(this, asset, spatialize, useHrtf, bus ?? _mainBus, ownsAsset);
            if (_systemConfig.UseCurveDistanceScaler)
                source.ApplyCurveDistanceScaler(_systemConfig.CurveDistanceScaler);
            else
                source.SetDistanceModel(_systemConfig.DistanceModel, _systemConfig.MinDistance, _systemConfig.MaxDistance, _systemConfig.RollOff);

            source.SetDopplerFactor(_systemConfig.DopplerFactor);
            source.SetRoomAcoustics(_roomAcoustics);

            lock (_sourceLock)
                _sources.Add(source);
            return source;
        }

        public TrackStream CreateStream(params string[] filePaths)
        {
            return CreateStream(_mainBus, filePaths);
        }

        public TrackStream CreateStream(params StreamAsset[] assets)
        {
            return CreateStream(_mainBus, assets);
        }

        internal TrackStream CreateStream(AudioBus bus, params string[] filePaths)
        {
            if (filePaths == null || filePaths.Length == 0)
                throw new ArgumentException("At least one file path is required.", nameof(filePaths));

            var assets = new StreamAsset[filePaths.Length];
            for (var i = 0; i < filePaths.Length; i++)
                assets[i] = new StreamAsset(filePaths[i]);
            return CreateStream(bus, ownsAssets: true, assets);
        }

        internal TrackStream CreateStream(AudioBus bus, params StreamAsset[] assets)
        {
            return CreateStream(bus, ownsAssets: false, assets);
        }

        private TrackStream CreateStream(AudioBus bus, bool ownsAssets, params StreamAsset[] assets)
        {
            ThrowIfDisposed();
            var stream = new TrackStream(this, bus, ownsAssets, assets);
            lock (_sourceLock)
                _streams.Add(stream);
            _diagnostics.Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.StreamCreated,
                AudioDiagnosticEntityType.Stream,
                Name,
                bus.Name,
                null,
                "Audio stream created.",
                new Dictionary<string, object?>
                {
                    ["trackCount"] = assets.Length
                });
            return stream;
        }

        internal void RetireSource(AudioSourceHandle source)
        {
            lock (_sourceLock)
            {
                _sources.Remove(source);
                _retired.Add(new RetiredSource(source, DateTime.UtcNow.AddMilliseconds(250)));
            }
        }

        internal void RemoveStream(TrackStream stream)
        {
            lock (_sourceLock)
                _streams.Remove(stream);
            _diagnostics.Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.StreamDisposed,
                AudioDiagnosticEntityType.Stream,
                Name,
                null,
                null,
                "Audio stream disposed.");
        }

        internal void RetireEffect(BusEffect effect)
        {
            lock (_sourceLock)
                _retiredEffects.Add(new RetiredEffect(effect, DateTime.UtcNow.AddMilliseconds(250)));
        }

        public void Update(double deltaTime)
        {
            AudioSourceHandle[] sourceSnapshot;
            TrackStream[] streamSnapshot;

            lock (_sourceLock)
            {
                sourceSnapshot = _sources.ToArray();
                streamSnapshot = _streams.ToArray();
            }

            for (var i = 0; i < streamSnapshot.Length; i++)
                streamSnapshot[i].Update();

            for (var i = 0; i < sourceSnapshot.Length; i++)
                sourceSnapshot[i].Update(deltaTime);

            if (_steamAudio != null)
            {
                for (var i = 0; i < sourceSnapshot.Length; i++)
                    sourceSnapshot[i].UpdateDoppler(_listenerPosition, _listenerVelocity, _systemConfig);

                _steamAudio.UpdateSimulation(sourceSnapshot);
            }

            ProcessRetiredSources();
            ProcessRetiredEffects();
        }

        private void ProcessRetiredSources()
        {
            if (_disposed)
                return;

            List<RetiredSource>? ready = null;
            lock (_sourceLock)
            {
                if (_retired.Count == 0)
                    return;

                var now = DateTime.UtcNow;
                for (var i = _retired.Count - 1; i >= 0; i--)
                {
                    var item = _retired[i];
                    if (item.DisposeAfterUtc > now)
                        continue;

                    ready ??= new List<RetiredSource>();
                    ready.Add(item);
                    _retired.RemoveAt(i);
                }
            }

            if (ready == null)
                return;

            for (var i = 0; i < ready.Count; i++)
                ready[i].Source.DisposeNative();
        }

        private void ProcessRetiredEffects()
        {
            if (_disposed)
                return;

            List<RetiredEffect>? ready = null;
            lock (_sourceLock)
            {
                if (_retiredEffects.Count == 0)
                    return;

                var now = DateTime.UtcNow;
                for (var i = _retiredEffects.Count - 1; i >= 0; i--)
                {
                    var item = _retiredEffects[i];
                    if (item.DisposeAfterUtc > now)
                        continue;

                    ready ??= new List<RetiredEffect>();
                    ready.Add(item);
                    _retiredEffects.RemoveAt(i);
                }
            }

            if (ready == null)
                return;

            for (var i = 0; i < ready.Count; i++)
                ready[i].Effect.DisposeNative();
        }

        public AudioOutputSnapshot CaptureSnapshot()
        {
            AudioSourceHandle[] sourceSnapshot;
            TrackStream[] streamSnapshot;
            int retiredCount;
            int retiredEffectCount;

            lock (_sourceLock)
            {
                sourceSnapshot = _sources.ToArray();
                streamSnapshot = _streams.ToArray();
                retiredCount = _retired.Count;
                retiredEffectCount = _retiredEffects.Count;
            }

            AudioBus[] busSnapshot;
            lock (_busLock)
                busSnapshot = _buses.Values.ToArray();

            var buses = new List<AudioBusSnapshot>(busSnapshot.Length);
            for (var i = 0; i < busSnapshot.Length; i++)
                buses.Add(busSnapshot[i].CaptureSnapshot());

            var sources = new List<AudioSourceSnapshot>(sourceSnapshot.Length);
            for (var i = 0; i < sourceSnapshot.Length; i++)
                sources.Add(sourceSnapshot[i].CaptureSnapshot());

            var master = GetMasterVolume();
            var lastPrePeak = _runtime.GetLastPreLimiterPeak();
            var lastPostPeak = _runtime.GetLastPostLimiterPeak();
            return new AudioOutputSnapshot(
                Name,
                SampleRate,
                Channels,
                master,
                AudioMath.GainToDecibels(master),
                lastPrePeak,
                AudioMath.GainToDecibels(lastPrePeak),
                lastPostPeak,
                AudioMath.GainToDecibels(lastPostPeak),
                IsHrtfActive,
                sourceSnapshot.Length,
                streamSnapshot.Length,
                retiredCount,
                retiredEffectCount,
                buses,
                sources);
        }

        internal AudioDiagnosticMixSnapshot CaptureMixSnapshot(float preLimiterPeak, float postLimiterPeak, float limiterGain, float masterVolume)
        {
            AudioSourceHandle[] sourceSnapshot;
            lock (_sourceLock)
                sourceSnapshot = _sources.ToArray();

            var activeSources = new List<AudioSourceSnapshot>(sourceSnapshot.Length);
            for (var i = 0; i < sourceSnapshot.Length; i++)
            {
                var source = sourceSnapshot[i];
                if (!source.IsPlaying)
                    continue;

                activeSources.Add(source.CaptureSnapshot());
            }

            return new AudioDiagnosticMixSnapshot(
                Name,
                masterVolume,
                AudioMath.GainToDecibels(masterVolume),
                preLimiterPeak,
                AudioMath.GainToDecibels(preLimiterPeak),
                postLimiterPeak,
                AudioMath.GainToDecibels(postLimiterPeak),
                limiterGain,
                AudioMath.GainToDecibels(limiterGain),
                activeSources);
        }
    }
}
