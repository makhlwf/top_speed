using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class InputService
    {
        public void Update()
        {
            _previous.CopyFrom(_current);
            _current.Clear();

            if (_suspended || _disposed)
                return;

            if (!_keyboardBackend.TryPopulateState(_current))
                return;

            _controllerBackend.Update();
        }

        public bool WasPressed(InputKey key)
        {
            if (_suspended)
                return false;

            var index = (int)key;
            if (index < 0 || index >= _keyLatch.Length)
                return false;

            if (_keyboardBackend.IsDown(key))
            {
                if (_keyLatch[index])
                    return false;

                _keyLatch[index] = true;
                return true;
            }

            _keyLatch[index] = false;
            return false;
        }

        public bool TryGetControllerState(out State state)
        {
            return _controllerBackend.TryGetState(out state);
        }

        public void ResetState()
        {
            _current.Clear();
            _previous.Clear();
            for (var i = 0; i < _keyLatch.Length; i++)
                _keyLatch[i] = false;
        }
    }
}

