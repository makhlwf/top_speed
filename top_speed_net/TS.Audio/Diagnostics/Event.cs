using System;
using System.Collections.Generic;

namespace TS.Audio
{
    public sealed class AudioDiagnosticEvent
    {
        public DateTime TimestampUtc { get; }
        public AudioDiagnosticLevel Level { get; }
        public AudioDiagnosticKind Kind { get; }
        public AudioDiagnosticEntityType EntityType { get; }
        public string? OutputName { get; }
        public string? BusName { get; }
        public int? SourceId { get; }
        public string Message { get; }
        public IReadOnlyDictionary<string, object?> Data { get; }
        public AudioDiagnosticSnapshot? Snapshot { get; }

        public AudioDiagnosticEvent(
            DateTime timestampUtc,
            AudioDiagnosticLevel level,
            AudioDiagnosticKind kind,
            AudioDiagnosticEntityType entityType,
            string? outputName,
            string? busName,
            int? sourceId,
            string message,
            IReadOnlyDictionary<string, object?>? data,
            AudioDiagnosticSnapshot? snapshot)
        {
            TimestampUtc = timestampUtc;
            Level = level;
            Kind = kind;
            EntityType = entityType;
            OutputName = outputName;
            BusName = busName;
            SourceId = sourceId;
            Message = string.IsNullOrWhiteSpace(message) ? kind.ToString() : message;
            Data = data ?? EmptyData.Value;
            Snapshot = snapshot;
        }

        private static class EmptyData
        {
            public static readonly IReadOnlyDictionary<string, object?> Value = new Dictionary<string, object?>(0);
        }
    }
}
