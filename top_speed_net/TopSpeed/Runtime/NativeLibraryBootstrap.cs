using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using MiniAudioEx.Native;
using SteamAudio;

namespace TopSpeed.Runtime
{
    internal static class NativeLibraryBootstrap
    {
        private static bool _initialized;
#if !NETFRAMEWORK
        private static readonly List<IntPtr> _loadedHandles = new List<IntPtr>();
        private static readonly HashSet<Assembly> _resolvedAssemblies = new HashSet<Assembly>();
#endif

        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            var libDirectory = Path.Combine(AppContext.BaseDirectory, "lib");
            if (!Directory.Exists(libDirectory))
                return;

#if NETFRAMEWORK
            TryLoadWindows(Path.Combine(libDirectory, "miniaudioex.dll"));
            TryLoadWindows(Path.Combine(libDirectory, "phonon.dll"));
#else
            InstallResolver(typeof(MiniAudioNative).Assembly, libDirectory);
            InstallResolver(typeof(MiniAudioExNative).Assembly, libDirectory);
            InstallResolver(typeof(IPL).Assembly, libDirectory);

            TryPreload(libDirectory, GetMiniAudioCandidates());
            TryPreload(libDirectory, GetPhononCandidates());
#endif
        }

#if NETFRAMEWORK
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string fileName);

        private static void TryLoadWindows(string absolutePath)
        {
            if (!File.Exists(absolutePath))
                return;

            try
            {
                LoadLibrary(absolutePath);
            }
            catch
            {
                // Ignore native preload failures. They will surface at call site if required.
            }
        }
#else
        private static void InstallResolver(Assembly assembly, string libDirectory)
        {
            if (_resolvedAssemblies.Contains(assembly))
                return;

            try
            {
                NativeLibrary.SetDllImportResolver(
                    assembly,
                    (libraryName, _, _) => ResolveLibrary(libraryName, libDirectory));
                _resolvedAssemblies.Add(assembly);
            }
            catch
            {
                // Resolver may already be set by another bootstrap path.
            }
        }

        private static IntPtr ResolveLibrary(string libraryName, string libDirectory)
        {
            if (IsMiniAudioLibraryName(libraryName))
                return TryLoadFirst(libDirectory, GetMiniAudioCandidates());

            if (IsPhononLibraryName(libraryName))
                return TryLoadFirst(libDirectory, GetPhononCandidates());

            return IntPtr.Zero;
        }

        private static void TryPreload(string libDirectory, IReadOnlyList<string> candidates)
        {
            TryLoadFirst(libDirectory, candidates);
        }

        private static IntPtr TryLoadFirst(string libDirectory, IReadOnlyList<string> candidates)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                var relative = candidates[i];
                var absolutePath = Path.Combine(libDirectory, relative);
                if (!File.Exists(absolutePath))
                    continue;

                try
                {
                    var handle = NativeLibrary.Load(absolutePath);
                    _loadedHandles.Add(handle);
                    return handle;
                }
                catch
                {
                    // Try next candidate.
                }
            }

            return IntPtr.Zero;
        }

        private static bool IsMiniAudioLibraryName(string? libraryName)
        {
            if (string.IsNullOrWhiteSpace(libraryName))
                return false;
            return libraryName.IndexOf("miniaudioex", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPhononLibraryName(string? libraryName)
        {
            if (string.IsNullOrWhiteSpace(libraryName))
                return false;
            return libraryName.IndexOf("phonon", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IReadOnlyList<string> GetMiniAudioCandidates()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new[] { "miniaudioex.dll" };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new[] { "libminiaudioex.dylib" };
            return new[] { "libminiaudioex.so" };
        }

        private static IReadOnlyList<string> GetPhononCandidates()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new[] { "phonon.dll" };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new[]
                {
                    "libphonon.dylib",
                    Path.Combine("phonon.bundle", "Contents", "MacOS", "phonon")
                };
            }

            return new[] { "libphonon.so" };
        }
#endif
    }
}
