using System;
using System.Collections.Generic;
using System.Numerics;

namespace TS.Audio
{
    public sealed class AudioEngine : IDisposable
    {
        private readonly AudioSystem _system;
        private readonly AudioOutput _primaryOutput;
        private readonly AudioOutput _speechOutput;
        private readonly Dictionary<string, AudioBus> _buses;
        private readonly PlaybackPolicy _defaults;
        private readonly object _transientLock;
        private readonly HashSet<Source> _transientSources;
        private readonly Queue<Source> _retiredSources;
        private bool _disposed;

        public AssetLibrary Assets { get; }
        public AudioSystem System => _system;
        public AudioDiagnostics Diagnostics => _system.Diagnostics;
        public AudioOutput PrimaryOutput => _primaryOutput;
        public AudioOutput SpeechOutput => _speechOutput;
        public IReadOnlyDictionary<string, AudioBus> Buses => _buses;
        public PlaybackPolicy Defaults => _defaults;

        public AudioBus MainBus => _primaryOutput.MainBus;
        public AudioBus WorldBus => GetBus(AudioEngineOptions.WorldBusName);
        public AudioBus UiBus => GetBus(AudioEngineOptions.UiBusName);
        public AudioBus MusicBus => GetBus(AudioEngineOptions.MusicBusName);
        public AudioBus VehiclesBus => GetBus(AudioEngineOptions.VehiclesBusName);
        public AudioBus TrackBus => GetBus(AudioEngineOptions.TrackBusName);
        public AudioBus CopilotBus => GetBus(AudioEngineOptions.CopilotBusName);
        public AudioBus RadioBus => GetBus(AudioEngineOptions.RadioBusName);
        public AudioBus SpeechBus => GetBus(AudioEngineOptions.SpeechBusName);

        public AudioEngine(AudioEngineOptions? options = null)
        {
            var resolved = options ?? new AudioEngineOptions();
            _system = new AudioSystem(resolved.SystemConfig ?? new AudioSystemConfig());
            Assets = new AssetLibrary();
            _buses = new Dictionary<string, AudioBus>(StringComparer.OrdinalIgnoreCase);
            _defaults = resolved.Defaults.Clone();
            _transientLock = new object();
            _transientSources = new HashSet<Source>();
            _retiredSources = new Queue<Source>();

            _primaryOutput = _system.CreateOutput(resolved.PrimaryOutput ?? new AudioOutputConfig { Name = "main" });
            _speechOutput = _system.CreateOutput(resolved.SpeechOutput ?? new AudioOutputConfig { Name = "speech" });

            _buses["main"] = _primaryOutput.MainBus;
            _buses[AudioEngineOptions.SpeechBusName] = _speechOutput.MainBus;
            _primaryOutput.MainBus.ApplyDefaults(_defaults);
            _speechOutput.MainBus.ApplyDefaults(_defaults);

            for (var i = 0; i < resolved.Buses.Count; i++)
                CreateConfiguredBus(resolved.Buses[i]);
        }

        public AudioBus GetBus(string name)
        {
            if (!_buses.TryGetValue(name, out var bus))
                throw new KeyNotFoundException("Audio bus not found: " + name);
            return bus;
        }

        public bool TryGetBus(string name, out AudioBus bus)
        {
            return _buses.TryGetValue(name, out bus!);
        }

        public bool TryResolveFile(string path, out string fullPath)
        {
            return Assets.TryResolveFile(path, out fullPath!);
        }

        public Clip LoadClip(string filePath, bool streamFromDisk = true, bool cache = true)
        {
            return Assets.LoadClip(filePath, streamFromDisk, cache);
        }

        public StreamAsset LoadStream(string filePath, bool cache = true)
        {
            return Assets.LoadStream(filePath, cache);
        }

        public BufferAsset CreateBufferAsset(byte[] data, string? name = null)
        {
            return Assets.CreateBuffer(data, name);
        }

        public GeneratorAsset CreateGeneratorAsset(ProceduralAudioCallback callback, uint channels = 1, uint sampleRate = 44100, string? name = null)
        {
            return Assets.CreateProcedural(callback, channels, sampleRate, name);
        }

        public Source CreateSource(SoundAsset asset, string? busName = null, bool? spatialize = null, bool? useHrtf = null)
        {
            return ResolveBus(busName).CreateSource(asset, spatialize, useHrtf);
        }

        public Source CreateSource(SoundAsset asset, SourceOptions options, string? busName = null)
        {
            return ResolveBus(busName).CreateSource(asset, options);
        }

        public Source CreateSpatialSource(SoundAsset asset, string? busName = null, bool? allowHrtf = null)
        {
            return ResolveBus(busName).CreateSpatialSource(asset, allowHrtf);
        }

        public Source Play(SoundAsset asset, string? busName = null, bool? loop = null, bool? spatialize = null, bool? useHrtf = null)
        {
            return ResolveBus(busName).Play(asset, loop, spatialize, useHrtf);
        }

        public void PlayOneShot(SoundAsset asset, string? busName = null, Action<Source>? configure = null, bool? spatialize = null, bool? useHrtf = null)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            var source = CreateSource(asset, busName, spatialize, useHrtf);
            TrackTransientSource(source);
            configure?.Invoke(source);
            source.Play(loop: false);
        }

        public void PlayOneShotSpatial(SoundAsset asset, string? busName = null, Action<Source>? configure = null, bool? allowHrtf = null)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            var source = CreateSpatialSource(asset, busName, allowHrtf);
            TrackTransientSource(source);
            configure?.Invoke(source);
            source.Play(loop: false);
        }

        public Source Play(SoundAsset asset, SourceOptions options, string? busName = null)
        {
            return ResolveBus(busName).Play(asset, options);
        }

        public Source CreateProceduralSource(ProceduralAudioCallback callback, uint channels = 1, uint sampleRate = 44100, string? busName = null, bool? spatialize = null, bool? useHrtf = null)
        {
            return ResolveBus(busName).CreateProceduralSource(callback, channels, sampleRate, spatialize, useHrtf);
        }

        public TrackStream CreateStream(string? busName, params StreamAsset[] assets)
        {
            return ResolveBus(busName).CreateStream(assets);
        }

        public TrackStream CreateStream(string? busName, params string[] filePaths)
        {
            return ResolveBus(busName).CreateStream(filePaths);
        }

        public void SetListener(Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity)
        {
            _system.UpdateListenerAll(position, forward, up, velocity);
        }

        public void Update()
        {
            DrainRetiredVoices();
            _system.Update();
            DrainRetiredVoices();
        }

        public void ConfigureDiagnostics(AudioDiagnosticConfig config)
        {
            Diagnostics.Configure(config);
        }

        public AudioDiagnosticSubscription SubscribeDiagnostics(Action<AudioDiagnosticEvent> handler, AudioDiagnosticFilter? filter = null)
        {
            return Diagnostics.Subscribe(handler, filter);
        }

        public void AddDiagnosticsSink(IAudioDiagnosticSink sink, AudioDiagnosticFilter? filter = null)
        {
            Diagnostics.AddSink(sink, filter);
        }

        public bool RemoveDiagnosticsSink(IAudioDiagnosticSink sink)
        {
            return Diagnostics.RemoveSink(sink);
        }

        public IReadOnlyList<AudioDiagnosticEvent> GetDiagnosticsHistory(AudioDiagnosticFilter? filter = null)
        {
            return Diagnostics.GetHistory(filter);
        }

        public void ClearDiagnosticsHistory()
        {
            Diagnostics.ClearHistory();
        }

        public AudioEngineSnapshot CaptureSnapshot()
        {
            var outputs = new List<AudioOutputSnapshot>(_system.Outputs.Count);
            foreach (var output in _system.Outputs.Values)
                outputs.Add(output.CaptureSnapshot());
            return new AudioEngineSnapshot(DateTime.UtcNow, outputs);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ClearTransientVoices();
            Assets.Dispose();
            _system.Dispose();
            _buses.Clear();
        }

        private AudioBus ResolveBus(string? busName)
        {
            if (string.IsNullOrWhiteSpace(busName))
                return MainBus;
            return GetBus(busName!);
        }

        private void CreateConfiguredBus(AudioBusOptions options)
        {
            if (options == null || string.IsNullOrWhiteSpace(options.Name))
                return;

            if (_buses.ContainsKey(options.Name))
            {
                var existing = _buses[options.Name];
                existing.SetVolume(options.Volume);
                existing.SetMuted(options.Muted);
                existing.ApplyDefaults(ResolveDefaults(options.Defaults));
                return;
            }

            var output = options.UseSpeechOutput ? _speechOutput : _primaryOutput;
            AudioBus? parent = null;
            if (!string.IsNullOrWhiteSpace(options.ParentName))
                _buses.TryGetValue(options.ParentName!, out parent);
            parent ??= output.MainBus;

            var bus = output.CreateBus(options.Name, parent, ResolveDefaults(options.Defaults));
            bus.SetVolume(options.Volume);
            bus.SetMuted(options.Muted);
            _buses[options.Name] = bus;
        }

        private PlaybackPolicy ResolveDefaults(PlaybackPolicy? busDefaults)
        {
            var merged = _defaults.Clone();
            if (busDefaults == null)
                return merged;

            if (busDefaults.Spatialize.HasValue)
                merged.Spatialize = busDefaults.Spatialize;
            if (busDefaults.UseHrtf.HasValue)
                merged.UseHrtf = busDefaults.UseHrtf;
            if (busDefaults.Loop.HasValue)
                merged.Loop = busDefaults.Loop;
            if (busDefaults.FadeInSeconds.HasValue)
                merged.FadeInSeconds = busDefaults.FadeInSeconds;
            if (busDefaults.Volume.HasValue)
                merged.Volume = busDefaults.Volume;
            if (busDefaults.Pitch.HasValue)
                merged.Pitch = busDefaults.Pitch;
            if (busDefaults.Pan.HasValue)
                merged.Pan = busDefaults.Pan;
            if (busDefaults.StereoWidening.HasValue)
                merged.StereoWidening = busDefaults.StereoWidening;
            if (busDefaults.Position.HasValue)
                merged.Position = busDefaults.Position;
            if (busDefaults.Velocity.HasValue)
                merged.Velocity = busDefaults.Velocity;
            if (busDefaults.CurveDistanceScaler.HasValue)
                merged.CurveDistanceScaler = busDefaults.CurveDistanceScaler;
            if (busDefaults.DopplerFactor.HasValue)
                merged.DopplerFactor = busDefaults.DopplerFactor;
            if (busDefaults.RoomAcoustics.HasValue)
                merged.RoomAcoustics = busDefaults.RoomAcoustics;
            if (busDefaults.DistanceModel.HasValue)
                merged.DistanceModel = busDefaults.DistanceModel;
            if (busDefaults.RefDistance.HasValue)
                merged.RefDistance = busDefaults.RefDistance;
            if (busDefaults.MaxDistance.HasValue)
                merged.MaxDistance = busDefaults.MaxDistance;
            if (busDefaults.RollOff.HasValue)
                merged.RollOff = busDefaults.RollOff;

            return merged;
        }

        private void TrackTransientSource(Source source)
        {
            lock (_transientLock)
                _transientSources.Add(source);

            source.SetOnEnd(() => QueueTransientRetire(source));
        }

        private void QueueTransientRetire(Source source)
        {
            lock (_transientLock)
            {
                if (_transientSources.Contains(source))
                    _retiredSources.Enqueue(source);
            }
        }

        private void DrainRetiredVoices()
        {
            while (true)
            {
                Source? source;
                lock (_transientLock)
                {
                    if (_retiredSources.Count == 0)
                        break;

                    source = _retiredSources.Dequeue();
                    _transientSources.Remove(source);
                }

                try
                {
                    source.Stop();
                }
                catch
                {
                }

                try
                {
                    source.Dispose();
                }
                catch
                {
                }
            }
        }

        private void ClearTransientVoices()
        {
            lock (_transientLock)
            {
                while (_retiredSources.Count > 0)
                    _transientSources.Remove(_retiredSources.Dequeue());

                foreach (var source in _transientSources)
                {
                    try
                    {
                        source.Stop();
                    }
                    catch
                    {
                    }

                    try
                    {
                        source.Dispose();
                    }
                    catch
                    {
                    }
                }

                _transientSources.Clear();
            }
        }
    }
}
