namespace TopSpeed.Game
{
    internal sealed partial class GameApp
    {
        private void StartGameLoop()
        {
            _loop.Start(
                deltaSeconds =>
                {
                    var game = _game;
                    if (game != null && !game.IsModalInputActive)
                        game.Update(deltaSeconds);
                },
                () =>
                {
                    var game = _game;
                    var intervalMs = game != null ? game.LoopIntervalMs : GameLoopIntervalMs;
                    if (intervalMs <= 0)
                        intervalMs = GameLoopIntervalMs;
                    return intervalMs;
                });
        }

        private void StopGameLoop()
        {
            _loop.Stop();
        }
    }
}

