using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CrossSpeak
{
    internal sealed class ScreenReaderWindows : IScreenReader
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        private bool _loaded;

        public ScreenReaderWindows()
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            if (assemblyDirectory == string.Empty)
                return;

            var dllDirectory = Path.Combine(assemblyDirectory, "lib", "screen-reader-libs", "windows");
            SetDllDirectory(dllDirectory);
        }

        public bool Initialize()
        {
            if (_loaded)
                Close();

            TolkNative.Load();
            _loaded = TolkNative.IsLoaded();
            return _loaded;
        }

        public bool IsLoaded() => _loaded && TolkNative.IsLoaded();

        public void TrySAPI(bool trySAPI) => TolkNative.TrySAPI(trySAPI);

        public void PreferSAPI(bool preferSAPI) => TolkNative.PreferSAPI(preferSAPI);

        public string DetectScreenReader()
        {
            if (!IsLoaded())
                return null;

            return TolkNative.DetectScreenReader();
        }

        public bool HasSpeech() => IsLoaded() && TolkNative.HasSpeech();

        public bool HasBraille() => IsLoaded() && TolkNative.HasBraille();

        public bool Speak(string text, bool interrupt = false)
        {
            if (string.IsNullOrWhiteSpace(text) || !IsLoaded())
                return false;

            return TolkNative.Speak(text, interrupt);
        }

        public bool Braille(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || !IsLoaded())
                return false;

            return TolkNative.Braille(text);
        }

        public bool Output(string text, bool interrupt = false)
        {
            if (string.IsNullOrWhiteSpace(text) || !IsLoaded())
                return false;

            return TolkNative.Output(text, interrupt);
        }

        public bool IsSpeaking() => IsLoaded() && TolkNative.IsSpeaking();

        public float GetVolume() => 0.0f;

        public void SetVolume(float volume)
        {
        }

        public float GetRate() => 0.0f;

        public void SetRate(float rate)
        {
        }

        public bool Silence() => IsLoaded() && TolkNative.Silence();

        public void Close()
        {
            if (!_loaded)
                return;

            TolkNative.Unload();
            _loaded = false;
        }

        private static class TolkNative
        {
            [DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            private static extern void Tolk_Load();

            [DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool Tolk_IsLoaded();

            [DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            private static extern void Tolk_Unload();

            [DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            private static extern void Tolk_TrySAPI([MarshalAs(UnmanagedType.I1)] bool trySAPI);

            [DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            private static extern void Tolk_PreferSAPI([MarshalAs(UnmanagedType.I1)] bool preferSAPI);

            [DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr Tolk_DetectScreenReader();

            [DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool Tolk_HasSpeech();

            [DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool Tolk_HasBraille();

            [DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool Tolk_Output([MarshalAs(UnmanagedType.LPWStr)] string str, [MarshalAs(UnmanagedType.I1)] bool interrupt);

            [DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool Tolk_Speak([MarshalAs(UnmanagedType.LPWStr)] string str, [MarshalAs(UnmanagedType.I1)] bool interrupt);

            [DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool Tolk_Braille([MarshalAs(UnmanagedType.LPWStr)] string str);

            [DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool Tolk_IsSpeaking();

            [DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            private static extern bool Tolk_Silence();

            public static void Load() => Tolk_Load();
            public static bool IsLoaded() => Tolk_IsLoaded();
            public static void Unload() => Tolk_Unload();
            public static void TrySAPI(bool trySAPI) => Tolk_TrySAPI(trySAPI);
            public static void PreferSAPI(bool preferSAPI) => Tolk_PreferSAPI(preferSAPI);
            public static string DetectScreenReader() => Marshal.PtrToStringUni(Tolk_DetectScreenReader());
            public static bool HasSpeech() => Tolk_HasSpeech();
            public static bool HasBraille() => Tolk_HasBraille();
            public static bool Output(string str, bool interrupt = false) => Tolk_Output(str, interrupt);
            public static bool Speak(string str, bool interrupt = false) => Tolk_Speak(str, interrupt);
            public static bool Braille(string str) => Tolk_Braille(str);
            public static bool IsSpeaking() => Tolk_IsSpeaking();
            public static bool Silence() => Tolk_Silence();
        }
    }
}
