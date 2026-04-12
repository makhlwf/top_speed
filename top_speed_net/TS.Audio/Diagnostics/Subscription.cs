using System;

namespace TS.Audio
{
    public sealed class AudioDiagnosticSubscription : IDisposable
    {
        private Action? _dispose;

        internal AudioDiagnosticSubscription(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            var dispose = _dispose;
            if (dispose == null)
                return;

            _dispose = null;
            dispose();
        }
    }
}
