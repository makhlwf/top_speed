using System;
using System.Collections.Generic;
using System.Threading;

namespace TS.Audio
{
    public sealed class AudioDiagnostics
    {
        private sealed class Subscriber
        {
            public int Id { get; }
            public AudioDiagnosticFilter? Filter { get; }
            public Action<AudioDiagnosticEvent> Handler { get; }

            public Subscriber(int id, Action<AudioDiagnosticEvent> handler, AudioDiagnosticFilter? filter)
            {
                Id = id;
                Handler = handler;
                Filter = filter;
            }
        }

        private sealed class SinkRegistration
        {
            public IAudioDiagnosticSink Sink { get; }
            public AudioDiagnosticFilter? Filter { get; }

            public SinkRegistration(IAudioDiagnosticSink sink, AudioDiagnosticFilter? filter)
            {
                Sink = sink;
                Filter = filter;
            }
        }

        private readonly object _lock;
        private readonly AudioDiagnosticRingBuffer<AudioDiagnosticEvent> _history;
        private readonly List<Subscriber> _subscribers;
        private readonly List<SinkRegistration> _sinks;
        private AudioDiagnosticConfig _config;
        private int _nextSubscriberId;

        public AudioDiagnostics()
        {
            _lock = new object();
            _history = new AudioDiagnosticRingBuffer<AudioDiagnosticEvent>(512);
            _subscribers = new List<Subscriber>();
            _sinks = new List<SinkRegistration>();
            _config = new AudioDiagnosticConfig();
        }

        public AudioDiagnosticConfig GetConfig()
        {
            lock (_lock)
                return _config.Clone();
        }

        public void Configure(AudioDiagnosticConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            lock (_lock)
            {
                _config = config.Clone();
                _history.SetCapacity(_config.HistoryCapacity);
            }
        }

        public AudioDiagnosticSubscription Subscribe(Action<AudioDiagnosticEvent> handler, AudioDiagnosticFilter? filter = null)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var subscriber = new Subscriber(Interlocked.Increment(ref _nextSubscriberId), handler, filter?.Clone());
            lock (_lock)
                _subscribers.Add(subscriber);

            return new AudioDiagnosticSubscription(() => Unsubscribe(subscriber.Id));
        }

        public void AddSink(IAudioDiagnosticSink sink, AudioDiagnosticFilter? filter = null)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            lock (_lock)
                _sinks.Add(new SinkRegistration(sink, filter?.Clone()));
        }

        public bool RemoveSink(IAudioDiagnosticSink sink)
        {
            if (sink == null)
                return false;

            lock (_lock)
            {
                for (var i = 0; i < _sinks.Count; i++)
                {
                    if (!ReferenceEquals(_sinks[i].Sink, sink))
                        continue;

                    _sinks.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<AudioDiagnosticEvent> GetHistory(AudioDiagnosticFilter? filter = null)
        {
            var snapshot = default(List<AudioDiagnosticEvent>);
            lock (_lock)
                snapshot = _history.Snapshot();

            if (filter == null)
                return snapshot;

            var filtered = new List<AudioDiagnosticEvent>(snapshot.Count);
            for (var i = 0; i < snapshot.Count; i++)
            {
                var diagnosticEvent = snapshot[i];
                if (filter.Matches(diagnosticEvent))
                    filtered.Add(diagnosticEvent);
            }

            return filtered;
        }

        public void ClearHistory()
        {
            lock (_lock)
                _history.Clear();
        }

        internal bool ShouldEmit(AudioDiagnosticLevel level, AudioDiagnosticKind kind, AudioDiagnosticEntityType entityType, string? outputName, string? busName, int? sourceId)
        {
            lock (_lock)
            {
                if (!_config.Enabled)
                    return false;

                return Matches(_config.Filter, level, kind, entityType, outputName, busName, sourceId);
            }
        }

        internal void Emit(
            AudioDiagnosticLevel level,
            AudioDiagnosticKind kind,
            AudioDiagnosticEntityType entityType,
            string? outputName,
            string? busName,
            int? sourceId,
            string message,
            IReadOnlyDictionary<string, object?>? data = null,
            AudioDiagnosticSnapshot? snapshot = null)
        {
            Subscriber[] subscribers;
            SinkRegistration[] sinks;
            AudioDiagnosticEvent diagnosticEvent;

            lock (_lock)
            {
                if (!_config.Enabled || !Matches(_config.Filter, level, kind, entityType, outputName, busName, sourceId))
                    return;

                diagnosticEvent = new AudioDiagnosticEvent(
                    DateTime.UtcNow,
                    level,
                    kind,
                    entityType,
                    outputName,
                    busName,
                    sourceId,
                    message,
                    data,
                    snapshot);

                _history.Add(diagnosticEvent);
                subscribers = _subscribers.ToArray();
                sinks = _sinks.ToArray();
            }

            for (var i = 0; i < subscribers.Length; i++)
            {
                var subscriber = subscribers[i];
                if (subscriber.Filter != null && !subscriber.Filter.Matches(diagnosticEvent))
                    continue;

                ThreadPool.QueueUserWorkItem(_ => subscriber.Handler(diagnosticEvent));
            }

            for (var i = 0; i < sinks.Length; i++)
            {
                var sink = sinks[i];
                if (sink.Filter != null && !sink.Filter.Matches(diagnosticEvent))
                    continue;

                ThreadPool.QueueUserWorkItem(_ => sink.Sink.Write(diagnosticEvent));
            }
        }

        private void Unsubscribe(int id)
        {
            lock (_lock)
            {
                for (var i = 0; i < _subscribers.Count; i++)
                {
                    if (_subscribers[i].Id != id)
                        continue;

                    _subscribers.RemoveAt(i);
                    return;
                }
            }
        }

        private static bool Matches(AudioDiagnosticFilter? filter, AudioDiagnosticLevel level, AudioDiagnosticKind kind, AudioDiagnosticEntityType entityType, string? outputName, string? busName, int? sourceId)
        {
            if (filter == null)
                return true;

            if (level < filter.MinimumLevel)
                return false;
            if (filter.Kinds.Count > 0 && !filter.Kinds.Contains(kind))
                return false;
            if (filter.EntityTypes.Count > 0 && !filter.EntityTypes.Contains(entityType))
                return false;
            if (filter.OutputNames.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(outputName) || !filter.OutputNames.Contains(outputName!))
                    return false;
            }

            if (filter.BusNames.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(busName) || !filter.BusNames.Contains(busName!))
                    return false;
            }
            if (filter.SourceIds.Count > 0 && (!sourceId.HasValue || !filter.SourceIds.Contains(sourceId.Value)))
                return false;

            return true;
        }
    }
}
