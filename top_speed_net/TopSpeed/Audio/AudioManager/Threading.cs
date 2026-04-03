using System.Threading;

namespace TopSpeed.Audio
{
    internal sealed partial class AudioManager
    {
        public void StartUpdateThread(int intervalMs = 8)
        {
            if (_updateRunning)
                return;
            _updateRunning = true;
            _updateThread = new Thread(() => UpdateLoop(intervalMs))
            {
                IsBackground = true,
                Name = "AudioUpdate"
            };
            _updateThread.Start();
        }

        public void StopUpdateThread()
        {
            _updateRunning = false;
            if (_updateThread == null)
                return;
            if (_updateThread.IsAlive)
                _updateThread.Join(200);
            _updateThread = null;
        }

        private void UpdateLoop(int intervalMs)
        {
            while (_updateRunning)
            {
                _system.Update();
                Thread.Sleep(intervalMs);
            }
        }
    }
}

