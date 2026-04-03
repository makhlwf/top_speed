using System;
using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Controller;
using TopSpeed.Input.Devices.Keyboard;
using TopSpeed.Input.Devices.Vibration;
using TopSpeed.Runtime;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class BackendRegistryTests
    {
        [Fact]
        public void CreateKeyboard_UsesHighestPrioritySupportedFactory()
        {
            var expected = new FakeKeyboardDevice();
            var registry = new BackendRegistry(
                new IKeyboardBackendFactory[]
                {
                    new FakeKeyboardFactory("low", priority: 10, supported: true, created: new FakeKeyboardDevice()),
                    new FakeKeyboardFactory("high", priority: 100, supported: true, created: expected)
                },
                new IControllerBackendFactory[] { new FakeControllerFactory("controller", 1, true, new FakeControllerBackend()) });

            var actual = registry.CreateKeyboard(IntPtr.Zero, eventSource: null);

            Assert.Same(expected, actual);
        }

        [Fact]
        public void CreateKeyboard_FallsBack_WhenPreferredFactoryThrows()
        {
            var fallback = new FakeKeyboardDevice();
            var registry = new BackendRegistry(
                new IKeyboardBackendFactory[]
                {
                    new FakeKeyboardFactory("throwing", priority: 100, supported: true, created: null, exception: new InvalidOperationException("boom")),
                    new FakeKeyboardFactory("fallback", priority: 10, supported: true, created: fallback)
                },
                new IControllerBackendFactory[] { new FakeControllerFactory("controller", 1, true, new FakeControllerBackend()) });

            var actual = registry.CreateKeyboard(IntPtr.Zero, eventSource: null);

            Assert.Same(fallback, actual);
        }

        [Fact]
        public void CreateController_UsesNextFactory_WhenHigherPriorityIsUnsupported()
        {
            var expected = new FakeControllerBackend();
            var registry = new BackendRegistry(
                new IKeyboardBackendFactory[] { new FakeKeyboardFactory("keyboard", 1, true, new FakeKeyboardDevice()) },
                new IControllerBackendFactory[]
                {
                    new FakeControllerFactory("unsupported", priority: 100, supported: false, created: new FakeControllerBackend()),
                    new FakeControllerFactory("fallback", priority: 10, supported: true, created: expected)
                });

            var actual = registry.CreateController(IntPtr.Zero);

            Assert.Same(expected, actual);
        }

        [Fact]
        public void CreateController_ThrowsDeterministicError_WhenNoneCanCreate()
        {
            var registry = new BackendRegistry(
                new IKeyboardBackendFactory[] { new FakeKeyboardFactory("keyboard", 1, true, new FakeKeyboardDevice()) },
                new IControllerBackendFactory[]
                {
                    new FakeControllerFactory("unsupported", priority: 100, supported: false, created: null),
                    new FakeControllerFactory("throwing", priority: 10, supported: true, created: null, exception: new InvalidOperationException("x"))
                });

            var ex = Assert.Throws<InvalidOperationException>(() => registry.CreateController(IntPtr.Zero));

            Assert.Contains("Unable to initialize controller backend", ex.Message);
            Assert.Contains("unsupported", ex.Message);
            Assert.Contains("throwing", ex.Message);
            Assert.Contains("Attempts", ex.Message);
        }

        private sealed class FakeKeyboardFactory : IKeyboardBackendFactory
        {
            private readonly bool _supported;
            private readonly IKeyboardDevice? _created;
            private readonly Exception? _exception;

            public FakeKeyboardFactory(string id, int priority, bool supported, IKeyboardDevice? created, Exception? exception = null)
            {
                Id = id;
                Priority = priority;
                _supported = supported;
                _created = created;
                _exception = exception;
            }

            public string Id { get; }
            public int Priority { get; }

            public bool IsSupported() => _supported;

            public IKeyboardDevice Create(IntPtr windowHandle, IKeyboardEventSource? eventSource)
            {
                if (_exception != null)
                    throw _exception;

                return _created!;
            }
        }

        private sealed class FakeControllerFactory : IControllerBackendFactory
        {
            private readonly bool _supported;
            private readonly IControllerBackend? _created;
            private readonly Exception? _exception;

            public FakeControllerFactory(string id, int priority, bool supported, IControllerBackend? created, Exception? exception = null)
            {
                Id = id;
                Priority = priority;
                _supported = supported;
                _created = created;
                _exception = exception;
            }

            public string Id { get; }
            public int Priority { get; }

            public bool IsSupported() => _supported;

            public IControllerBackend Create(IntPtr windowHandle)
            {
                if (_exception != null)
                    throw _exception;

                return _created!;
            }
        }

        private sealed class FakeKeyboardDevice : IKeyboardDevice
        {
            public bool TryPopulateState(InputState state) => true;
            public bool IsDown(InputKey key) => false;
            public bool IsAnyKeyHeld(bool ignoreModifiers) => false;
            public void ResetHeldState() { }
            public void Suspend() { }
            public void Resume() { }
            public void Dispose() { }
        }

        private sealed class FakeControllerBackend : IControllerBackend
        {
            public event Action? ScanTimedOut;
            public bool ActiveControllerIsRacingWheel => false;
            public bool IgnoreAxesForMenuNavigation => false;
            public IVibrationDevice? VibrationDevice => null;

            public void SetEnabled(bool enabled) { }
            public void Update() { }
            public bool TryGetState(out State state) { state = default; return false; }
            public bool TryPollState(out State state) { state = default; return false; }
            public bool IsAnyButtonHeld() => false;
            public bool TryGetPendingChoices(out IReadOnlyList<Choice> choices) { choices = Array.Empty<Choice>(); return false; }
            public bool TrySelect(Guid instanceGuid) => false;
            public void Suspend() { }
            public void Resume() { }
            public void Dispose() { }

            public void RaiseScanTimedOut()
            {
                ScanTimedOut?.Invoke();
            }
        }
    }
}
