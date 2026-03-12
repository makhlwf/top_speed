using System;
using System.Reflection;

namespace TopSpeed.Speech
{
    internal sealed partial class SpeechService
    {
        private sealed class JawsClient
        {
            private const string ProgId = "FreedomSci.JawsApi";
            private Type? _jawsType;
            private object? _jawsObject;
            private bool _initialized;
            private bool _available;

            public bool IsAvailable => EnsureInitialized();

            public bool Speak(string text, bool stop)
            {
                return Invoke("SayString", text, stop);
            }

            public void Stop()
            {
                Invoke("StopSpeech");
            }

            private bool EnsureInitialized()
            {
                if (_initialized)
                    return _available;
                _initialized = true;
                try
                {
                    _jawsType = Type.GetTypeFromProgID(ProgId);
                    if (_jawsType == null)
                        return false;
                    _jawsObject = Activator.CreateInstance(_jawsType);
                    _available = _jawsObject != null;
                }
                catch
                {
                    _available = false;
                }
                return _available;
            }

            private bool Invoke(string method, params object[] args)
            {
                if (!EnsureInitialized() || _jawsType == null || _jawsObject == null)
                    return false;
                try
                {
                    var result = _jawsType.InvokeMember(
                        method,
                        BindingFlags.InvokeMethod,
                        null,
                        _jawsObject,
                        args);
                    return result is bool ok ? ok : result != null;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
