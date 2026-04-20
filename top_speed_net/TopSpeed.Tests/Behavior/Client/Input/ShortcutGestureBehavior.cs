using TopSpeed.Input;
using TopSpeed.Shortcuts;
using TS.Sdl.Input;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class ShortcutGestureBehaviorTests
{
    [Fact]
    public void TryResolveTriggeredAction_UsesGestureIntentWhenDefined()
    {
        var (service, _, _) = InputHarness.CreateService();
        using (service)
        {
            var catalog = new ShortcutCatalog();
            var triggerCount = 0;

            catalog.RegisterAction(
                "chat.next",
                "Next chat",
                "Moves to the next chat category.",
                InputKey.Right,
                () => triggerCount++,
                gestureIntent: GestureIntent.SwipeRight);
            catalog.SetGlobalActions(new[] { "chat.next" });

            service.SubmitGesture(new GestureEvent { Kind = GestureKind.Swipe, Direction = SwipeDirection.Right });

            catalog.TryResolveTriggeredAction(service, new ShortcutContext("main", string.Empty), out var action).Should().BeTrue();
            action.Trigger();
            triggerCount.Should().Be(1);
            catalog.TryResolveTriggeredAction(service, new ShortcutContext("main", string.Empty), out _).Should().BeFalse();
        }
    }

    [Fact]
    public void TryResolveTriggeredAction_FiresOnceWhenKeyAndGestureArriveTogether()
    {
        var (service, keyboard, _) = InputHarness.CreateService();
        using (service)
        {
            var catalog = new ShortcutCatalog();
            var triggerCount = 0;

            catalog.RegisterAction(
                "chat.next",
                "Next chat",
                "Moves to the next chat category.",
                InputKey.Right,
                () => triggerCount++,
                gestureIntent: GestureIntent.SwipeRight);
            catalog.SetGlobalActions(new[] { "chat.next" });

            keyboard.SetDown(InputKey.Right);
            service.SubmitGesture(new GestureEvent { Kind = GestureKind.Swipe, Direction = SwipeDirection.Right });

            catalog.TryResolveTriggeredAction(service, new ShortcutContext("main", string.Empty), out var action).Should().BeTrue();
            action.Trigger();
            triggerCount.Should().Be(1);
            catalog.TryResolveTriggeredAction(service, new ShortcutContext("main", string.Empty), out _).Should().BeFalse();
        }
    }
}

