using System.Collections.Generic;

namespace TS.Audio
{
    public sealed partial class AudioOutput
    {
        public void Dispose()
        {
            if (_disposed)
                return;

            var snapshot = CaptureSnapshot();
            _disposed = true;

            AudioSourceHandle[] sourceSnapshot;
            TrackStream[] streamSnapshot;
            RetiredSource[] retiredSnapshot;
            RetiredEffect[] retiredEffectSnapshot;
            AudioBus[] busSnapshot;

            lock (_sourceLock)
            {
                sourceSnapshot = _sources.ToArray();
                streamSnapshot = _streams.ToArray();
                retiredSnapshot = _retired.ToArray();
                retiredEffectSnapshot = _retiredEffects.ToArray();
                _sources.Clear();
                _streams.Clear();
                _retired.Clear();
                _retiredEffects.Clear();
            }

            lock (_busLock)
            {
                busSnapshot = new List<AudioBus>(_buses.Values).ToArray();
                _buses.Clear();
            }

            for (var i = 0; i < sourceSnapshot.Length; i++)
                sourceSnapshot[i].DisposeNative();

            for (var i = 0; i < streamSnapshot.Length; i++)
                streamSnapshot[i].Dispose();

            for (var i = 0; i < retiredSnapshot.Length; i++)
                retiredSnapshot[i].Source.DisposeNative();

            for (var i = 0; i < retiredEffectSnapshot.Length; i++)
                retiredEffectSnapshot[i].Effect.DisposeNative();

            for (var i = 0; i < busSnapshot.Length; i++)
                busSnapshot[i].Dispose();

            _steamAudio?.Dispose();
            _runtime.Dispose();
            _diagnostics.Emit(
                AudioDiagnosticLevel.Info,
                AudioDiagnosticKind.OutputDisposed,
                AudioDiagnosticEntityType.Output,
                Name,
                null,
                null,
                "Audio output disposed.",
                null,
                new AudioDiagnosticSnapshot(output: snapshot));
        }
    }
}
