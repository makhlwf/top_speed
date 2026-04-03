using System;
using System.Diagnostics;
#if NETFRAMEWORK
using System.Speech.Synthesis;
#endif
using System.Threading;
using CrossSpeak;
using TopSpeed.Localization;

namespace TopSpeed.Speech
{
    internal sealed partial class SpeechService : IGameSpeech
    {
        public enum SpeakFlag
        {
            None,
            NoInterrupt,
            NoInterruptButStop,
            Interruptable,
            InterruptableButStop
        }

        private readonly Stopwatch _watch = new Stopwatch();
        private readonly IScreenReader _screenReader;
#if NETFRAMEWORK
        private SpeechSynthesizer? _sapi;
#endif
        private long _timeRequiredMs;
        private string _lastSpoken = string.Empty;
        private Func<bool>? _isInputHeld;
        private Action? _prepareForInterruptableSpeech;
        private bool _screenReaderReady;

        public SpeechService(Func<bool>? isInputHeld = null, Action? prepareForInterruptableSpeech = null)
        {
            _isInputHeld = isInputHeld;
            _prepareForInterruptableSpeech = prepareForInterruptableSpeech;
            _screenReader = CrossSpeakManager.Instance;
            _screenReaderReady = InitializeScreenReader();
        }

        public bool IsAvailable => _screenReaderReady || IsSapiInitialized();

        public float ScreenReaderRateMs { get; set; }

        public void BindInputProbe(Func<bool> isInputHeld)
        {
            _isInputHeld = isInputHeld;
        }

        public void BindInterruptPreparation(Action prepareForInterruptableSpeech)
        {
            _prepareForInterruptableSpeech = prepareForInterruptableSpeech;
        }

        public void Speak(string text)
        {
            Speak(text, SpeakFlag.None);
        }

        public void Speak(string text, SpeakFlag flag)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var shouldInterruptCurrent = flag == SpeakFlag.NoInterruptButStop || flag == SpeakFlag.InterruptableButStop;
            if (shouldInterruptCurrent)
                Purge();

            text = text.Trim();
            text = LocalizationService.Translate(text);
            _lastSpoken = text;

            var spoke = false;
            if (_screenReaderReady)
            {
                spoke = TrySpeakWithScreenReader(text, shouldInterruptCurrent);
                if (spoke)
                    StartSpeakTimer(text);
            }

            if (!spoke)
            {
#if NETFRAMEWORK
                EnsureSapi();
                _sapi!.SpeakAsync(text);
                while (!IsSpeaking())
                {
                    Thread.Sleep(0);
                }
#endif
            }

            if (flag == SpeakFlag.None)
                return;

            var interruptable = flag == SpeakFlag.Interruptable || flag == SpeakFlag.InterruptableButStop;
            if (interruptable)
                PrepareForInterruptableSpeech();

            while (IsSpeaking())
            {
                if (interruptable)
                {
                    if (IsInputHeld())
                        break;
                }

                Thread.Sleep(10);
            }
        }

        public bool IsSpeaking()
        {
            if (_watch.IsRunning)
                return _watch.ElapsedMilliseconds < _timeRequiredMs;

            if (_screenReaderReady)
            {
                try
                {
                    if (_screenReader.IsSpeaking())
                        return true;
                }
                catch
                {
                }
            }

#if NETFRAMEWORK
            return _sapi != null && _sapi.State == SynthesizerState.Speaking;
#else
            return false;
#endif
        }

        public void Purge()
        {
            _watch.Reset();
            _timeRequiredMs = 0;
#if NETFRAMEWORK
            if (_sapi != null)
            {
                try
                {
                    _sapi.SpeakAsyncCancelAll();
                }
                catch (OperationCanceledException)
                {
                }

                while (IsSpeaking())
                {
                    Thread.Sleep(0);
                }
            }
#endif

            if (_screenReaderReady)
            {
                try
                {
                    _screenReader.Silence();
                }
                catch
                {
                }
            }
        }

        public void Dispose()
        {
            Purge();
#if NETFRAMEWORK
            _sapi?.Dispose();
#endif

            try
            {
                _screenReader.Close();
            }
            catch
            {
            }
        }

        private bool InitializeScreenReader()
        {
            try
            {
                _screenReader.TrySAPI(false);
                _screenReader.PreferSAPI(false);
                return _screenReader.Initialize();
            }
            catch
            {
                return false;
            }
        }

        private bool TrySpeakWithScreenReader(string text, bool interrupt)
        {
            try
            {
                if (_screenReader.Output(text, interrupt))
                    return true;

                return _screenReader.Speak(text, interrupt);
            }
            catch
            {
                return false;
            }
        }

        private bool IsSapiInitialized()
        {
#if NETFRAMEWORK
            return _sapi != null;
#else
            return false;
#endif
        }

#if NETFRAMEWORK
        private void EnsureSapi()
        {
            if (_sapi == null)
                _sapi = new SpeechSynthesizer();
        }
#endif

        private void StartSpeakTimer(string text)
        {
            if (ScreenReaderRateMs <= 0f)
            {
                _watch.Reset();
                _timeRequiredMs = 0;
                return;
            }

            var words = CountWords(text);
            _timeRequiredMs = (long)(words * ScreenReaderRateMs);
            _watch.Reset();
            _watch.Start();
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private bool IsInputHeld()
        {
            try
            {
                return _isInputHeld != null && _isInputHeld();
            }
            catch
            {
                return false;
            }
        }

        private void PrepareForInterruptableSpeech()
        {
            try
            {
                _prepareForInterruptableSpeech?.Invoke();
            }
            catch
            {
            }
        }
    }
}
