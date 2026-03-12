using System;
using System.Runtime.InteropServices;

namespace TopSpeed.Speech
{
    internal sealed partial class SpeechService
    {
        private sealed class NvdaClient : IDisposable
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
            private delegate int NvdaSpeak([MarshalAs(UnmanagedType.LPWStr)] string text);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private delegate int NvdaCancel();

            private IntPtr _module;
            private NvdaSpeak? _speak;
            private NvdaCancel? _cancel;

            public bool IsAvailable => _speak != null;

            public NvdaClient()
            {
                var names = Environment.Is64BitProcess
                    ? new[] { "nvda_client_64.dll", "nvdaControllerClient64.dll" }
                    : new[] { "nvda_client_32.dll", "nvdaControllerClient32.dll" };

                foreach (var name in names)
                {
                    _module = LoadLibrary(name);
                    if (_module != IntPtr.Zero)
                        break;
                }

                if (_module == IntPtr.Zero)
                    return;

                _speak = GetProc<NvdaSpeak>("nvdaController_speakText");
                _cancel = GetProc<NvdaCancel>("nvdaController_cancelSpeech");
                if (_speak == null)
                {
                    FreeLibrary(_module);
                    _module = IntPtr.Zero;
                }
            }

            public bool Speak(string text)
            {
                if (_speak == null)
                    return false;
                try
                {
                    return _speak(text) == 0;
                }
                catch
                {
                    return false;
                }
            }

            public void Cancel()
            {
                try
                {
                    _cancel?.Invoke();
                }
                catch
                {
                }
            }

            public void Dispose()
            {
                if (_module != IntPtr.Zero)
                {
                    FreeLibrary(_module);
                    _module = IntPtr.Zero;
                }
            }

            private T? GetProc<T>(string name) where T : class
            {
                if (_module == IntPtr.Zero)
                    return null;
                var proc = GetProcAddress(_module, name);
                if (proc == IntPtr.Zero)
                    return null;
                return Marshal.GetDelegateForFunctionPointer(proc, typeof(T)) as T;
            }

            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern IntPtr LoadLibrary(string lpFileName);

            [DllImport("kernel32", SetLastError = true)]
            private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

            [DllImport("kernel32", SetLastError = true)]
            private static extern bool FreeLibrary(IntPtr hModule);
        }
    }
}
