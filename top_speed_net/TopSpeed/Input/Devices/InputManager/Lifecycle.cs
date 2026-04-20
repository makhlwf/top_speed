using System;

namespace TopSpeed.Input
{
    internal sealed partial class InputService
    {
        public void Suspend()
        {
            _suspended = true;
            _keyboardBackend.Suspend();
            _controllerBackend.Suspend();
        }

        public void Resume()
        {
            _suspended = false;
            _keyboardBackend.Resume();
            _controllerBackend.Resume();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _controllerBackend.NoControllerDetected -= OnNoControllerDetected;
            if (_gestureEventSource != null)
                _gestureEventSource.GestureRaised -= OnGestureRaised;
            if (_touchZoneGestureEventSource != null)
                _touchZoneGestureEventSource.TouchZoneGestureRaised -= OnTouchZoneGestureRaised;
            if (_touchZoneTouchEventSource != null)
                _touchZoneTouchEventSource.TouchZoneTouchRaised -= OnTouchZoneTouchRaised;
            SafeRelease(() => _controllerBackend.Dispose());
            SafeRelease(() => _keyboardBackend.Dispose());
        }

        private static void SafeRelease(Action release)
        {
            try
            {
                release();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (NullReferenceException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}

