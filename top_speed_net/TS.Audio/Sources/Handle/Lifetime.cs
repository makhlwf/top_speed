using System;
using MiniAudioEx.Native;

namespace TS.Audio
{
    public sealed partial class AudioSourceHandle
    {
        public void Dispose()
        {
            if (_disposeRequested || _disposed)
                return;

            var snapshot = CaptureSnapshot();
            _disposeRequested = true;
            CancelFade();
            MiniAudioExNative.ma_ex_audio_source_stop(_sourceHandle);
            _output.RetireSource(this);
            Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceDisposed,
                "Audio source disposal requested.",
                null,
                new AudioDiagnosticSnapshot(source: snapshot));
        }

        internal void DisposeNative()
        {
            if (_disposed)
                return;

            _disposed = true;
            _disposeRequested = true;
            _onEnd = null;

            _graph.Dispose();
            _playback.Dispose();

            MiniAudioNative.ma_node_detach_all_output_buses(new ma_node_ptr(_group.pointer));
            MiniAudioNative.ma_sound_group_uninit(_group);
            _group.Free();
            MiniAudioExNative.ma_ex_audio_source_uninit(_sourceHandle);

            if (_ownsAsset)
                _asset.Dispose();
        }

    }
}
