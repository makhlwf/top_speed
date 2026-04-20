using System;

namespace TopSpeed.Runtime
{
    public static class UpdatePackageRuntime
    {
        private static readonly object Sync = new object();
        private static IUpdatePackageInstaller? _installer;

        public static void SetInstaller(IUpdatePackageInstaller? installer)
        {
            IUpdatePackageInstaller? previous;
            lock (Sync)
            {
                previous = _installer;
                _installer = installer;
            }

            if (previous is IDisposable disposable)
                disposable.Dispose();
        }

        public static IUpdatePackageInstaller? GetInstaller()
        {
            lock (Sync)
                return _installer;
        }
    }
}
