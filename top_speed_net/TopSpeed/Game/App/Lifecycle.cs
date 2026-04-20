using System.Threading.Tasks;

namespace TopSpeed.Game
{
    internal sealed partial class GameApp
    {
        private void OnLoaded()
        {
            _game = new Game(_window, _textInput, _fileDialogs, _clipboard);
            var game = _game;
            _game.ExitRequested += async () =>
            {
                game.FadeOutMenuMusic(500);
                await Task.Delay(500).ConfigureAwait(true);
                _window.RequestClose();
            };
            game.Initialize();
            StartGameLoop();
        }

        private void OnClosed()
        {
            StopGameLoop();
            _game?.Dispose();
            _game = null;
        }
    }
}

