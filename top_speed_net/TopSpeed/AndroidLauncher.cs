using TopSpeed.Core;
using TopSpeed.Game;
using TopSpeed.Runtime;
using TopSpeed.Windowing.Sdl;
using System;

namespace TopSpeed
{
    public static class AndroidLauncher
    {
        private static readonly object Sync = new object();
        private static WindowHost? _window;
        private static string? _assetRoot;
        private static bool _running;

        public static void SetAssetRoot(string? path)
        {
            lock (Sync)
                _assetRoot = path;
        }

        public static void Run()
        {
            lock (Sync)
            {
                if (_running)
                    return;
                _running = true;
            }

            try
            {
                Environment.SetEnvironmentVariable("TOPSPEED_TOUCH_HINTS", "1");

                var configuredRoot = _assetRoot;
                if (!string.IsNullOrWhiteSpace(configuredRoot))
                    AssetPaths.SetRoot(configuredRoot);

                NativeLibraryBootstrap.Initialize();
                var window = new WindowHost();
                lock (Sync)
                    _window = window;

                using (var app = new GameApp(
                           window,
                           window,
                           new LoopHost(),
                           new FileDialogService(window),
                           new ClipboardService()))
                {
                    app.Run();
                }
            }
            finally
            {
                lock (Sync)
                {
                    _window = null;
                    _running = false;
                }
            }
        }

        public static void RequestClose()
        {
            lock (Sync)
                _window?.RequestClose();
        }
    }
}
