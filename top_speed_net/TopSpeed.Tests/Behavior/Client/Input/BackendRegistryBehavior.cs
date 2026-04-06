using System;
using TopSpeed.Input;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class BackendRegistryBehaviorTests
{
    [Fact]
    public void CreateKeyboard_UsesHighestPrioritySupportedFactory()
    {
        var expected = new InputHarness.FakeKeyboardDevice();
        var registry = new BackendRegistry(
            new IKeyboardBackendFactory[]
            {
                new InputHarness.FakeKeyboardFactory("low", priority: 10, supported: true, created: new InputHarness.FakeKeyboardDevice()),
                new InputHarness.FakeKeyboardFactory("high", priority: 100, supported: true, created: expected)
            },
            new IControllerBackendFactory[]
            {
                new InputHarness.FakeControllerFactory("controller", 1, true, new InputHarness.FakeControllerBackend())
            });

        registry.CreateKeyboard(IntPtr.Zero, eventSource: null).Should().BeSameAs(expected);
    }

    [Fact]
    public void CreateKeyboard_FallsBack_WhenPreferredFactoryThrows()
    {
        var fallback = new InputHarness.FakeKeyboardDevice();
        var registry = new BackendRegistry(
            new IKeyboardBackendFactory[]
            {
                new InputHarness.FakeKeyboardFactory("throwing", priority: 100, supported: true, created: null, exception: new InvalidOperationException("boom")),
                new InputHarness.FakeKeyboardFactory("fallback", priority: 10, supported: true, created: fallback)
            },
            new IControllerBackendFactory[]
            {
                new InputHarness.FakeControllerFactory("controller", 1, true, new InputHarness.FakeControllerBackend())
            });

        registry.CreateKeyboard(IntPtr.Zero, eventSource: null).Should().BeSameAs(fallback);
    }

    [Fact]
    public void CreateController_UsesNextFactory_WhenHigherPriorityIsUnsupported()
    {
        var expected = new InputHarness.FakeControllerBackend();
        var registry = new BackendRegistry(
            new IKeyboardBackendFactory[]
            {
                new InputHarness.FakeKeyboardFactory("keyboard", 1, true, new InputHarness.FakeKeyboardDevice())
            },
            new IControllerBackendFactory[]
            {
                new InputHarness.FakeControllerFactory("unsupported", priority: 100, supported: false, created: new InputHarness.FakeControllerBackend()),
                new InputHarness.FakeControllerFactory("fallback", priority: 10, supported: true, created: expected)
            });

        registry.CreateController(IntPtr.Zero).Should().BeSameAs(expected);
    }

    [Fact]
    public void CreateController_ThrowsDeterministicError_WhenNoneCanCreate()
    {
        var registry = new BackendRegistry(
            new IKeyboardBackendFactory[]
            {
                new InputHarness.FakeKeyboardFactory("keyboard", 1, true, new InputHarness.FakeKeyboardDevice())
            },
            new IControllerBackendFactory[]
            {
                new InputHarness.FakeControllerFactory("unsupported", priority: 100, supported: false, created: null),
                new InputHarness.FakeControllerFactory("throwing", priority: 10, supported: true, created: null, exception: new InvalidOperationException("x"))
            });

        var act = () => registry.CreateController(IntPtr.Zero);

        var ex = act.Should().Throw<InvalidOperationException>().Which;
        ex.Message.Should().Contain("Unable to initialize controller backend");
        ex.Message.Should().Contain("Attempts");
        ex.Message.Should().Contain("unsupported");
        ex.Message.Should().Contain("throwing");
    }
}
