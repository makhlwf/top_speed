using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace TS.Audio
{
    public sealed class AudioDiagnosticJsonlSink : IAudioDiagnosticSink, IDisposable
    {
        private readonly object _lock;
        private readonly StreamWriter _writer;
        private bool _disposed;

        public string Path { get; }

        public AudioDiagnosticJsonlSink(string path, bool append = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path is required.", nameof(path));

            Path = path;
            _lock = new object();

            var directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var stream = new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            _writer = new StreamWriter(stream, new UTF8Encoding(false));
            _writer.AutoFlush = true;
        }

        public void Write(AudioDiagnosticEvent diagnosticEvent)
        {
            if (diagnosticEvent == null || _disposed)
                return;

            var line = BuildJsonLine(diagnosticEvent);
            lock (_lock)
            {
                if (_disposed)
                    return;

                _writer.WriteLine(line);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _writer.Dispose();
            }
        }

        private static string BuildJsonLine(AudioDiagnosticEvent diagnosticEvent)
        {
            var builder = new StringBuilder(1024);
            builder.Append('{');
            WriteProperty(builder, "timestampUtc", diagnosticEvent.TimestampUtc.ToString("O", CultureInfo.InvariantCulture), first: true);
            WriteProperty(builder, "level", diagnosticEvent.Level.ToString());
            WriteProperty(builder, "kind", diagnosticEvent.Kind.ToString());
            WriteProperty(builder, "entityType", diagnosticEvent.EntityType.ToString());
            WriteProperty(builder, "outputName", diagnosticEvent.OutputName);
            WriteProperty(builder, "busName", diagnosticEvent.BusName);
            WriteProperty(builder, "sourceId", diagnosticEvent.SourceId);
            WriteProperty(builder, "message", diagnosticEvent.Message);
            WriteProperty(builder, "data", diagnosticEvent.Data);
            WriteProperty(builder, "snapshot", diagnosticEvent.Snapshot);
            builder.Append('}');
            return builder.ToString();
        }

        private static void WriteProperty(StringBuilder builder, string name, object? value, bool first = false)
        {
            if (!first)
                builder.Append(',');

            builder.Append('"');
            builder.Append(Escape(name));
            builder.Append("\":");
            WriteValue(builder, value);
        }

        private static void WriteValue(StringBuilder builder, object? value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            if (value is string text)
            {
                builder.Append('"');
                builder.Append(Escape(text));
                builder.Append('"');
                return;
            }

            if (value is bool boolean)
            {
                builder.Append(boolean ? "true" : "false");
                return;
            }

            if (value is Enum)
            {
                builder.Append('"');
                builder.Append(Escape(value.ToString() ?? string.Empty));
                builder.Append('"');
                return;
            }

            if (value is int || value is long || value is uint || value is ulong || value is short || value is ushort || value is byte || value is sbyte)
            {
                builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }

            if (value is float single)
            {
                builder.Append(single.ToString("R", CultureInfo.InvariantCulture));
                return;
            }

            if (value is double dbl)
            {
                builder.Append(dbl.ToString("R", CultureInfo.InvariantCulture));
                return;
            }

            if (value is decimal dec)
            {
                builder.Append(dec.ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (value is IReadOnlyDictionary<string, object?> readOnlyDictionary)
            {
                WriteDictionary(builder, readOnlyDictionary);
                return;
            }

            if (value is IDictionary<string, object?> dictionary)
            {
                WriteDictionary(builder, dictionary);
                return;
            }

            if (value is AudioDiagnosticSnapshot snapshot)
            {
                WriteSnapshot(builder, snapshot);
                return;
            }

            if (value is AudioDiagnosticMixSnapshot mixSnapshot)
            {
                WriteMixSnapshot(builder, mixSnapshot);
                return;
            }

            if (value is AudioOutputSnapshot outputSnapshot)
            {
                WriteOutputSnapshot(builder, outputSnapshot);
                return;
            }

            if (value is AudioBusSnapshot busSnapshot)
            {
                WriteBusSnapshot(builder, busSnapshot);
                return;
            }

            if (value is AudioSourceSnapshot sourceSnapshot)
            {
                WriteSourceSnapshot(builder, sourceSnapshot);
                return;
            }

            if (value is AudioGainStageSnapshot gainStageSnapshot)
            {
                WriteGainStageSnapshot(builder, gainStageSnapshot);
                return;
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                builder.Append('[');
                var first = true;
                foreach (var item in enumerable)
                {
                    if (!first)
                        builder.Append(',');
                    first = false;
                    WriteValue(builder, item);
                }
                builder.Append(']');
                return;
            }

            builder.Append('"');
            builder.Append(Escape(value.ToString() ?? string.Empty));
            builder.Append('"');
        }

        private static void WriteDictionary(StringBuilder builder, IEnumerable<KeyValuePair<string, object?>> dictionary)
        {
            builder.Append('{');
            var first = true;
            foreach (var pair in dictionary)
            {
                if (!first)
                    builder.Append(',');
                first = false;
                builder.Append('"');
                builder.Append(Escape(pair.Key));
                builder.Append("\":");
                WriteValue(builder, pair.Value);
            }
            builder.Append('}');
        }

        private static void WriteSnapshot(StringBuilder builder, AudioDiagnosticSnapshot snapshot)
        {
            builder.Append('{');
            WriteProperty(builder, "output", snapshot.Output, first: true);
            WriteProperty(builder, "bus", snapshot.Bus);
            WriteProperty(builder, "source", snapshot.Source);
            WriteProperty(builder, "mix", snapshot.Mix);
            builder.Append('}');
        }

        private static void WriteOutputSnapshot(StringBuilder builder, AudioOutputSnapshot snapshot)
        {
            builder.Append('{');
            WriteProperty(builder, "name", snapshot.Name, first: true);
            WriteProperty(builder, "sampleRate", snapshot.SampleRate);
            WriteProperty(builder, "channels", snapshot.Channels);
            WriteProperty(builder, "masterVolume", snapshot.MasterVolume);
            WriteProperty(builder, "masterVolumeDb", snapshot.MasterVolumeDb);
            WriteProperty(builder, "lastPreLimiterPeak", snapshot.LastPreLimiterPeak);
            WriteProperty(builder, "lastPreLimiterPeakDbfs", snapshot.LastPreLimiterPeakDbfs);
            WriteProperty(builder, "lastPostLimiterPeak", snapshot.LastPostLimiterPeak);
            WriteProperty(builder, "lastPostLimiterPeakDbfs", snapshot.LastPostLimiterPeakDbfs);
            WriteProperty(builder, "hrtfActive", snapshot.HrtfActive);
            WriteProperty(builder, "sourceCount", snapshot.SourceCount);
            WriteProperty(builder, "streamCount", snapshot.StreamCount);
            WriteProperty(builder, "retiredSourceCount", snapshot.RetiredSourceCount);
            WriteProperty(builder, "retiredEffectCount", snapshot.RetiredEffectCount);
            WriteProperty(builder, "buses", snapshot.Buses);
            WriteProperty(builder, "sources", snapshot.Sources);
            builder.Append('}');
        }

        private static void WriteBusSnapshot(StringBuilder builder, AudioBusSnapshot snapshot)
        {
            builder.Append('{');
            WriteProperty(builder, "name", snapshot.Name, first: true);
            WriteProperty(builder, "parentName", snapshot.ParentName);
            WriteProperty(builder, "localVolume", snapshot.LocalVolume);
            WriteProperty(builder, "localVolumeDb", snapshot.LocalVolumeDb);
            WriteProperty(builder, "effectiveVolume", snapshot.EffectiveVolume);
            WriteProperty(builder, "effectiveVolumeDb", snapshot.EffectiveVolumeDb);
            WriteProperty(builder, "muted", snapshot.Muted);
            WriteProperty(builder, "childCount", snapshot.ChildCount);
            WriteProperty(builder, "effectsEnabled", snapshot.EffectsEnabled);
            WriteProperty(builder, "effectCount", snapshot.EffectCount);
            WriteProperty(builder, "effects", snapshot.Effects);
            WriteProperty(builder, "gainStages", snapshot.GainStages);
            builder.Append('}');
        }

        private static void WriteSourceSnapshot(StringBuilder builder, AudioSourceSnapshot snapshot)
        {
            builder.Append('{');
            WriteProperty(builder, "sourceId", snapshot.SourceId, first: true);
            WriteProperty(builder, "busName", snapshot.BusName);
            WriteProperty(builder, "isPlaying", snapshot.IsPlaying);
            WriteProperty(builder, "isSpatialized", snapshot.IsSpatialized);
            WriteProperty(builder, "usesSteamAudio", snapshot.UsesSteamAudio);
            WriteProperty(builder, "inputChannels", snapshot.InputChannels);
            WriteProperty(builder, "inputSampleRate", snapshot.InputSampleRate);
            WriteProperty(builder, "looping", snapshot.Looping);
            WriteProperty(builder, "volume", snapshot.Volume);
            WriteProperty(builder, "volumeDb", snapshot.VolumeDb);
            WriteProperty(builder, "pitch", snapshot.Pitch);
            WriteProperty(builder, "pan", snapshot.Pan);
            WriteProperty(builder, "busEffectiveVolume", snapshot.BusEffectiveVolume);
            WriteProperty(builder, "busEffectiveVolumeDb", snapshot.BusEffectiveVolumeDb);
            WriteProperty(builder, "estimatedMixVolume", snapshot.EstimatedMixVolume);
            WriteProperty(builder, "estimatedMixVolumeDb", snapshot.EstimatedMixVolumeDb);
            WriteProperty(builder, "busGainStages", snapshot.BusGainStages);
            WriteProperty(builder, "lengthSeconds", snapshot.LengthSeconds);
            builder.Append('}');
        }

        private static void WriteMixSnapshot(StringBuilder builder, AudioDiagnosticMixSnapshot snapshot)
        {
            builder.Append('{');
            WriteProperty(builder, "outputName", snapshot.OutputName, first: true);
            WriteProperty(builder, "masterVolume", snapshot.MasterVolume);
            WriteProperty(builder, "masterVolumeDb", snapshot.MasterVolumeDb);
            WriteProperty(builder, "preLimiterPeak", snapshot.PreLimiterPeak);
            WriteProperty(builder, "preLimiterPeakDbfs", snapshot.PreLimiterPeakDbfs);
            WriteProperty(builder, "postLimiterPeak", snapshot.PostLimiterPeak);
            WriteProperty(builder, "postLimiterPeakDbfs", snapshot.PostLimiterPeakDbfs);
            WriteProperty(builder, "limiterGain", snapshot.LimiterGain);
            WriteProperty(builder, "limiterGainDb", snapshot.LimiterGainDb);
            WriteProperty(builder, "activeSources", snapshot.ActiveSources);
            builder.Append('}');
        }

        private static void WriteGainStageSnapshot(StringBuilder builder, AudioGainStageSnapshot snapshot)
        {
            builder.Append('{');
            WriteProperty(builder, "name", snapshot.Name, first: true);
            WriteProperty(builder, "linearGain", snapshot.LinearGain);
            WriteProperty(builder, "gainDb", snapshot.GainDb);
            builder.Append('}');
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length + 8);
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                switch (ch)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    default:
                        if (ch < 32)
                            builder.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(ch);
                        break;
                }
            }
            return builder.ToString();
        }
    }
}
