using System;
using System.Runtime.InteropServices;

namespace TopSpeed.Speech.Prism
{
    internal sealed class AndroidMethods : IMethods
    {
        private const string Library = "prism";
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PrismAudioCallback(IntPtr userData, IntPtr samples, UIntPtr sampleCount, UIntPtr channels, UIntPtr sampleRate);

        private sealed class MemoryState
        {
            public MemoryState(MemoryAudioCallback callback)
            {
                Callback = callback;
            }

            public MemoryAudioCallback Callback { get; }
        }

        private static readonly PrismAudioCallback SpeakToMemoryProc = OnSpeakToMemory;

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Config prism_config_init();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_init(ref Config config);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern void prism_shutdown(IntPtr context);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern UIntPtr prism_registry_count(IntPtr context);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong prism_registry_id_at(IntPtr context, UIntPtr index);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_registry_name(IntPtr context, ulong id);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern int prism_registry_priority(IntPtr context, ulong id);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool prism_registry_exists(IntPtr context, ulong id);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong prism_registry_id(IntPtr context, IntPtr name);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_registry_create(IntPtr context, ulong id);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_registry_create_best(IntPtr context);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_registry_acquire(IntPtr context, ulong id);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_registry_acquire_best(IntPtr context);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern void prism_backend_free(IntPtr backend);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_backend_name(IntPtr backend);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong prism_backend_get_features(IntPtr backend);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_initialize(IntPtr backend);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_speak(IntPtr backend, IntPtr text, [MarshalAs(UnmanagedType.I1)] bool interrupt);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_speak_to_memory(IntPtr backend, IntPtr text, PrismAudioCallback callback, IntPtr userData);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_braille(IntPtr backend, IntPtr text);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_output(IntPtr backend, IntPtr text, [MarshalAs(UnmanagedType.I1)] bool interrupt);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_stop(IntPtr backend);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_pause(IntPtr backend);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_resume(IntPtr backend);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_is_speaking(IntPtr backend, [MarshalAs(UnmanagedType.I1)] out bool speaking);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_set_volume(IntPtr backend, float volume);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_get_volume(IntPtr backend, out float volume);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_set_rate(IntPtr backend, float rate);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_get_rate(IntPtr backend, out float rate);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_set_pitch(IntPtr backend, float pitch);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_get_pitch(IntPtr backend, out float pitch);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_refresh_voices(IntPtr backend);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_count_voices(IntPtr backend, out UIntPtr count);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_get_voice_name(IntPtr backend, UIntPtr voiceId, out IntPtr name);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_get_voice_language(IntPtr backend, UIntPtr voiceId, out IntPtr language);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_set_voice(IntPtr backend, UIntPtr voiceId);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_get_voice(IntPtr backend, out UIntPtr voiceId);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_get_channels(IntPtr backend, out UIntPtr channels);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_get_sample_rate(IntPtr backend, out UIntPtr sampleRate);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern Error prism_backend_get_bit_depth(IntPtr backend, out UIntPtr bitDepth);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr prism_error_string(Error error);

        public Config ConfigInit() => prism_config_init();
        public IntPtr Init(ref Config config) => prism_init(ref config);
        public void Shutdown(IntPtr context) => prism_shutdown(context);
        public int RegistryCount(IntPtr context) => checked((int)prism_registry_count(context));
        public ulong RegistryIdAt(IntPtr context, int index) => prism_registry_id_at(context, (UIntPtr)index);
        public string? RegistryName(IntPtr context, ulong id) => Strings.FromUtf8(prism_registry_name(context, id));
        public int RegistryPriority(IntPtr context, ulong id) => prism_registry_priority(context, id);
        public bool RegistryExists(IntPtr context, ulong id) => prism_registry_exists(context, id);
        public ulong RegistryId(IntPtr context, string name) => Strings.WithUtf8(name, ptr => prism_registry_id(context, ptr));
        public IntPtr Create(IntPtr context, ulong id) => prism_registry_create(context, id);
        public IntPtr CreateBest(IntPtr context) => prism_registry_create_best(context);
        public IntPtr Acquire(IntPtr context, ulong id) => prism_registry_acquire(context, id);
        public IntPtr AcquireBest(IntPtr context) => prism_registry_acquire_best(context);
        public void FreeBackend(IntPtr backend) => prism_backend_free(backend);
        public string? BackendName(IntPtr backend) => Strings.FromUtf8(prism_backend_name(backend));
        public Features BackendFeatures(IntPtr backend) => (Features)prism_backend_get_features(backend);
        public Error InitializeBackend(IntPtr backend) => prism_backend_initialize(backend);
        public Error Speak(IntPtr backend, string text, bool interrupt) => Strings.WithUtf8(text, ptr => prism_backend_speak(backend, ptr, interrupt));
        public Error SpeakToMemory(IntPtr backend, string text, MemoryAudioCallback callback)
        {
            var state = new MemoryState(callback);
            var handle = GCHandle.Alloc(state);
            try
            {
                return Strings.WithUtf8(text, ptr => prism_backend_speak_to_memory(backend, ptr, SpeakToMemoryProc, GCHandle.ToIntPtr(handle)));
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }

        public Error Braille(IntPtr backend, string text) => Strings.WithUtf8(text, ptr => prism_backend_braille(backend, ptr));
        public Error Output(IntPtr backend, string text, bool interrupt) => Strings.WithUtf8(text, ptr => prism_backend_output(backend, ptr, interrupt));
        public Error Stop(IntPtr backend) => prism_backend_stop(backend);
        public Error Pause(IntPtr backend) => prism_backend_pause(backend);
        public Error Resume(IntPtr backend) => prism_backend_resume(backend);
        public Error IsSpeaking(IntPtr backend, out bool speaking) => prism_backend_is_speaking(backend, out speaking);
        public Error SetVolume(IntPtr backend, float volume) => prism_backend_set_volume(backend, volume);
        public Error GetVolume(IntPtr backend, out float volume) => prism_backend_get_volume(backend, out volume);
        public Error SetRate(IntPtr backend, float rate) => prism_backend_set_rate(backend, rate);
        public Error GetRate(IntPtr backend, out float rate) => prism_backend_get_rate(backend, out rate);
        public Error SetPitch(IntPtr backend, float pitch) => prism_backend_set_pitch(backend, pitch);
        public Error GetPitch(IntPtr backend, out float pitch) => prism_backend_get_pitch(backend, out pitch);
        public Error RefreshVoices(IntPtr backend) => prism_backend_refresh_voices(backend);

        public Error CountVoices(IntPtr backend, out int count)
        {
            var error = prism_backend_count_voices(backend, out var nativeCount);
            count = checked((int)nativeCount);
            return error;
        }

        public Error GetVoiceName(IntPtr backend, int voiceIndex, out string? name)
        {
            var error = prism_backend_get_voice_name(backend, (UIntPtr)voiceIndex, out var value);
            name = Strings.FromUtf8(value);
            return error;
        }

        public Error GetVoiceLanguage(IntPtr backend, int voiceIndex, out string? language)
        {
            var error = prism_backend_get_voice_language(backend, (UIntPtr)voiceIndex, out var value);
            language = Strings.FromUtf8(value);
            return error;
        }

        public Error SetVoice(IntPtr backend, int voiceIndex) => prism_backend_set_voice(backend, (UIntPtr)voiceIndex);

        public Error GetVoice(IntPtr backend, out int voiceIndex)
        {
            var error = prism_backend_get_voice(backend, out var nativeIndex);
            voiceIndex = checked((int)nativeIndex);
            return error;
        }

        public Error GetChannels(IntPtr backend, out int channels)
        {
            var error = prism_backend_get_channels(backend, out var nativeValue);
            channels = checked((int)nativeValue);
            return error;
        }

        public Error GetSampleRate(IntPtr backend, out int sampleRate)
        {
            var error = prism_backend_get_sample_rate(backend, out var nativeValue);
            sampleRate = checked((int)nativeValue);
            return error;
        }

        public Error GetBitDepth(IntPtr backend, out int bitDepth)
        {
            var error = prism_backend_get_bit_depth(backend, out var nativeValue);
            bitDepth = checked((int)nativeValue);
            return error;
        }

        public string? ErrorString(Error error) => Strings.FromUtf8(prism_error_string(error));

        private static void OnSpeakToMemory(IntPtr userData, IntPtr samples, UIntPtr sampleCount, UIntPtr channels, UIntPtr sampleRate)
        {
            if (sampleCount == UIntPtr.Zero)
                return;

            var handle = GCHandle.FromIntPtr(userData);
            if (!(handle.Target is MemoryState state))
                return;

            var count = checked((int)sampleCount);
            var managed = new float[count];
            Marshal.Copy(samples, managed, 0, count);
            state.Callback(managed, checked((int)channels), checked((int)sampleRate));
        }
    }
}
