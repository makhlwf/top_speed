using System;
using TopSpeed.Runtime;

namespace TopSpeed.Game
{
    internal sealed partial class GameApp : IDisposable
    {
        private const int GameLoopIntervalMs = 8;
        private readonly IWindowHost _window;
        private readonly ITextInputService _textInput;
        private readonly ILoopHost _loop;
        private readonly IFileDialogs _fileDialogs;
        private Game? _game;

        public GameApp(
            IWindowHost window,
            ITextInputService textInput,
            ILoopHost loop,
            IFileDialogs fileDialogs)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _textInput = textInput ?? throw new ArgumentNullException(nameof(textInput));
            _loop = loop ?? throw new ArgumentNullException(nameof(loop));
            _fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
            _window.Closed += OnClosed;
            _window.Loaded += OnLoaded;
        }

        public void Run()
        {
            _window.Run();
        }

        public void Dispose()
        {
            _window.Closed -= OnClosed;
            _window.Loaded -= OnLoaded;
            _loop.Stop();
            _loop.Dispose();
            _window.Dispose();
            _game?.Dispose();
            _game = null;
        }
    }
}


