using System;
using System.Collections.Generic;

namespace TS.Audio
{
    public sealed class AudioDiagnosticFilter
    {
        public AudioDiagnosticLevel MinimumLevel { get; set; } = AudioDiagnosticLevel.Trace;
        public HashSet<AudioDiagnosticKind> Kinds { get; } = new HashSet<AudioDiagnosticKind>();
        public HashSet<AudioDiagnosticEntityType> EntityTypes { get; } = new HashSet<AudioDiagnosticEntityType>();
        public HashSet<string> OutputNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> BusNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<int> SourceIds { get; } = new HashSet<int>();

        public bool Matches(AudioDiagnosticEvent diagnosticEvent)
        {
            if (diagnosticEvent == null)
                return false;

            if (diagnosticEvent.Level < MinimumLevel)
                return false;
            if (Kinds.Count > 0 && !Kinds.Contains(diagnosticEvent.Kind))
                return false;
            if (EntityTypes.Count > 0 && !EntityTypes.Contains(diagnosticEvent.EntityType))
                return false;
            if (OutputNames.Count > 0)
            {
                var outputName = diagnosticEvent.OutputName;
                if (string.IsNullOrWhiteSpace(outputName) || !OutputNames.Contains(outputName!))
                    return false;
            }

            if (BusNames.Count > 0)
            {
                var busName = diagnosticEvent.BusName;
                if (string.IsNullOrWhiteSpace(busName) || !BusNames.Contains(busName!))
                    return false;
            }
            if (SourceIds.Count > 0 && (!diagnosticEvent.SourceId.HasValue || !SourceIds.Contains(diagnosticEvent.SourceId.Value)))
                return false;
            return true;
        }

        public AudioDiagnosticFilter Clone()
        {
            var clone = new AudioDiagnosticFilter
            {
                MinimumLevel = MinimumLevel
            };

            foreach (var kind in Kinds)
                clone.Kinds.Add(kind);
            foreach (var entityType in EntityTypes)
                clone.EntityTypes.Add(entityType);
            foreach (var outputName in OutputNames)
                clone.OutputNames.Add(outputName);
            foreach (var busName in BusNames)
                clone.BusNames.Add(busName);
            foreach (var sourceId in SourceIds)
                clone.SourceIds.Add(sourceId);

            return clone;
        }
    }
}
