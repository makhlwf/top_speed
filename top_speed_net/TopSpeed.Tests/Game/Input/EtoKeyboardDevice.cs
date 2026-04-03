using TopSpeed.Input;
using TopSpeed.Input.Devices.Keyboard.Backends.Eto;
using TopSpeed.Runtime;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class EtoKeyboardDeviceTests
    {
        [Fact]
        public void KeyDownAndUp_UpdatesState()
        {
            var source = new FakeKeyboardEventSource();
            using var device = new Device(source);
            var state = new InputState();

            source.RaiseKeyDown(InputKey.Up);
            Assert.True(device.TryPopulateState(state));
            Assert.True(state.IsDown(InputKey.Up));
            Assert.True(device.IsDown(InputKey.Up));

            source.RaiseKeyUp(InputKey.Up);
            state.Clear();
            Assert.True(device.TryPopulateState(state));
            Assert.False(state.IsDown(InputKey.Up));
            Assert.False(device.IsDown(InputKey.Up));
        }

        [Fact]
        public void IsAnyKeyHeld_IgnoresOnlyModifiersWhenRequested()
        {
            var source = new FakeKeyboardEventSource();
            using var device = new Device(source);

            source.RaiseKeyDown(InputKey.LeftShift);
            Assert.True(device.IsAnyKeyHeld(ignoreModifiers: false));
            Assert.False(device.IsAnyKeyHeld(ignoreModifiers: true));

            source.RaiseKeyDown(InputKey.A);
            Assert.True(device.IsAnyKeyHeld(ignoreModifiers: false));
            Assert.True(device.IsAnyKeyHeld(ignoreModifiers: true));
        }

        [Fact]
        public void Suspend_ClearsAndBlocksStateUntilResume()
        {
            var source = new FakeKeyboardEventSource();
            using var device = new Device(source);
            var state = new InputState();

            source.RaiseKeyDown(InputKey.Space);
            device.Suspend();
            state.Clear();
            Assert.True(device.TryPopulateState(state));
            Assert.False(state.IsDown(InputKey.Space));

            source.RaiseKeyDown(InputKey.Space);
            state.Clear();
            Assert.True(device.TryPopulateState(state));
            Assert.False(state.IsDown(InputKey.Space));

            device.Resume();
            source.RaiseKeyDown(InputKey.Space);
            state.Clear();
            Assert.True(device.TryPopulateState(state));
            Assert.True(state.IsDown(InputKey.Space));
        }

        private sealed class FakeKeyboardEventSource : IKeyboardEventSource
        {
            public event System.Action<InputKey>? KeyDown;
            public event System.Action<InputKey>? KeyUp;

            public void RaiseKeyDown(InputKey key)
            {
                KeyDown?.Invoke(key);
            }

            public void RaiseKeyUp(InputKey key)
            {
                KeyUp?.Invoke(key);
            }
        }
    }
}
