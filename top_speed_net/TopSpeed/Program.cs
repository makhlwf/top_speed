using System;
using System.IO;
using System.Runtime.InteropServices;
#if NETFRAMEWORK
using System.Windows.Forms;
#endif
using TopSpeed.Game;
using TopSpeed.Localization;
using TopSpeed.Runtime;
#if NETFRAMEWORK
using TopSpeed.Windowing.WinForms;
#else
using TopSpeed.Windowing.Eto;
#endif

namespace TopSpeed
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
#if NETFRAMEWORK
            using var timerResolution = new WindowsTimerResolution(1);
#endif
#if NETFRAMEWORK
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, args) => HandleException(args.Exception);
#endif
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                HandleException(args.ExceptionObject as Exception ?? new Exception(LocalizationService.Mark("Unknown exception.")));

            NativeLibraryBootstrap.Initialize();

#if NETFRAMEWORK
            var window = new WindowHost();
            using (var app = new GameApp(
                       window,
                       window,
                       new LoopHost(),
                       new FileDialogService()))
#else
            var window = new WindowHost();
            var textInput = new TextInputService(window);
            using (var app = new GameApp(
                       window,
                       textInput,
                       new LoopHost(),
                       new FileDialogService(window)))
#endif
            {
                app.Run();
            }
        }

#if NETFRAMEWORK
        private sealed class WindowsTimerResolution : IDisposable
        {
            private readonly uint _milliseconds;
            private readonly bool _active;

            public WindowsTimerResolution(uint milliseconds)
            {
                _milliseconds = milliseconds;
                try
                {
                    _active = timeBeginPeriod(_milliseconds) == 0;
                }
                catch
                {
                    _active = false;
                }
            }

            public void Dispose()
            {
                if (!_active)
                    return;

                try
                {
                    timeEndPeriod(_milliseconds);
                }
                catch
                {
                    // Ignore timer API shutdown failures.
                }
            }

            [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
            private static extern uint timeBeginPeriod(uint uPeriod);

            [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
            private static extern uint timeEndPeriod(uint uPeriod);
        }
#endif

        private static void HandleException(Exception exception)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var logName = $"topspeed_error_{timestamp}.log";
            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), logName);
                File.WriteAllText(path, exception.ToString());
            }
            catch
            {
                // Ignore logging failures.
            }

#if NETFRAMEWORK
            try
            {
                MessageBox.Show(
                    LocalizationService.Format(
                        LocalizationService.Mark("An unexpected error occurred. A log file was created: {0}"),
                        logName),
                    LocalizationService.Translate(LocalizationService.Mark("Top Speed")),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                // Ignore UI failures.
            }
#endif
        }
    }
}


