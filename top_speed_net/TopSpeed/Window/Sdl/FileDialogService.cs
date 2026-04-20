using System;
using TopSpeed.Runtime;
using TS.Sdl.Dialogs;

namespace TopSpeed.Windowing.Sdl
{
    internal sealed class FileDialogService : IFileDialogs
    {
        private readonly WindowHost _window;

        public FileDialogService(WindowHost window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
        }

        public void PickAudioFile(Action<string?> onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            var filters = new[]
            {
                new DialogFileFilter("Audio files", "wav;ogg;mp3;flac;aac;m4a"),
                new DialogFileFilter("All files", "*")
            };

            FileDialogs.ShowOpenFile(
                result =>
                {
                    if (result == null || result.WasCancelled || result.Paths.Length == 0)
                    {
                        onCompleted(null);
                        return;
                    }

                    onCompleted(result.Paths[0]);
                },
                _window.NativeHandle,
                filters);
        }

        public void PickFolder(string? initialFolder, Action<string?> onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            FileDialogs.ShowOpenFolder(
                result =>
                {
                    if (result == null || result.WasCancelled || result.Paths.Length == 0)
                    {
                        onCompleted(null);
                        return;
                    }

                    onCompleted(result.Paths[0]);
                },
                _window.NativeHandle,
                initialFolder,
                allowMany: false);
        }
    }
}

