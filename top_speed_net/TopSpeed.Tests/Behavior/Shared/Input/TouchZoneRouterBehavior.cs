using System.Collections.Generic;
using TS.Sdl.Events;
using TS.Sdl.Input;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TouchZoneRouterBehaviorTests
{
    [Fact]
    public void Registry_Overlap_UsesHighestPriority()
    {
        var registry = new TouchZoneRegistry();
        registry.Set(new TouchZone("base", new TouchZoneRect(0f, 0f, 1f, 1f), priority: 1));
        registry.Set(new TouchZone("focus", new TouchZoneRect(0.2f, 0.2f, 0.4f, 0.4f), priority: 10));

        registry.TryResolve(0.3f, 0.3f, out var hit).Should().BeTrue();
        hit.Id.Should().Be("focus");
    }

    [Fact]
    public void Registry_EqualPriority_UsesRegistrationOrder()
    {
        var registry = new TouchZoneRegistry();
        registry.Set(new TouchZone("first", new TouchZoneRect(0f, 0f, 1f, 1f), priority: 5));
        registry.Set(new TouchZone("second", new TouchZoneRect(0f, 0f, 1f, 1f), priority: 5));

        registry.TryResolve(0.8f, 0.2f, out var hit).Should().BeTrue();
        hit.Id.Should().Be("first");
    }

    [Fact]
    public void HorizontalLayout_CreatesTopBottomZones()
    {
        var zones = TouchZoneLayout.Horizontal("top", "bottom", splitY: 0.4f);

        zones.Should().HaveCount(2);
        zones[0].Id.Should().Be("top");
        zones[1].Id.Should().Be("bottom");
        zones[0].Rect.Height.Should().BeApproximately(0.4f, 0.0001f);
        zones[1].Rect.Y.Should().BeApproximately(0.4f, 0.0001f);
    }

    [Fact]
    public void LockZone_KeepsOwnershipAcrossMotionAndSwipe()
    {
        var recognizer = new GestureRecognizer(new GestureOptions
        {
            SwipeMinDistance = 0.05f,
            SwipeMinVelocity = 0.1f
        });
        using var router = new TouchZoneRouter(recognizer);
        router.SetZone(new TouchZone("left", new TouchZoneRect(0f, 0f, 0.5f, 1f), behavior: TouchZoneBehavior.Lock));
        router.SetZone(new TouchZone("right", new TouchZoneRect(0.5f, 0f, 0.5f, 1f), behavior: TouchZoneBehavior.Lock));
        var touches = new List<TouchZoneTouchEvent>();
        var gestures = new List<TouchZoneGestureEvent>();
        router.TouchRaised += value => touches.Add(value);
        router.GestureRaised += value => gestures.Add(value);

        router.Process(Touch(EventType.FingerDown, Ms(0), 11, 101, 0.25f, 0.50f));
        router.Process(Touch(EventType.FingerMotion, Ms(120), 11, 101, 0.75f, 0.50f));
        router.Process(Touch(EventType.FingerUp, Ms(220), 11, 101, 0.75f, 0.50f));

        touches.Should().HaveCount(3);
        touches[0].Zone.ZoneId.Should().Be("left");
        touches[1].Zone.ZoneId.Should().Be("left");
        touches[2].Zone.ZoneId.Should().Be("left");
        var swipe = gestures.Should().ContainSingle(x => x.Gesture.Kind == GestureKind.Swipe).Subject;
        swipe.Zone.ZoneId.Should().Be("left");
    }

    [Fact]
    public void DynamicZone_ReevaluatesOwnershipDuringMotion()
    {
        var recognizer = new GestureRecognizer(new GestureOptions
        {
            SwipeMinDistance = 0.05f,
            SwipeMinVelocity = 0.1f
        });
        using var router = new TouchZoneRouter(recognizer);
        router.SetZone(new TouchZone("left", new TouchZoneRect(0f, 0f, 0.5f, 1f), behavior: TouchZoneBehavior.Dynamic));
        router.SetZone(new TouchZone("right", new TouchZoneRect(0.5f, 0f, 0.5f, 1f), behavior: TouchZoneBehavior.Dynamic));
        var touches = new List<TouchZoneTouchEvent>();
        var gestures = new List<TouchZoneGestureEvent>();
        router.TouchRaised += value => touches.Add(value);
        router.GestureRaised += value => gestures.Add(value);

        router.Process(Touch(EventType.FingerDown, Ms(0), 12, 102, 0.25f, 0.50f));
        router.Process(Touch(EventType.FingerMotion, Ms(120), 12, 102, 0.80f, 0.50f));
        router.Process(Touch(EventType.FingerUp, Ms(220), 12, 102, 0.80f, 0.50f));

        touches.Should().HaveCount(3);
        touches[0].Zone.ZoneId.Should().Be("left");
        touches[1].Zone.ZoneId.Should().Be("right");
        touches[2].Zone.ZoneId.Should().Be("right");
        var swipe = gestures.Should().ContainSingle(x => x.Gesture.Kind == GestureKind.Swipe).Subject;
        swipe.Zone.ZoneId.Should().Be("right");
    }

    [Fact]
    public void OutsideStart_DoesNotCaptureLockZoneOnMotion()
    {
        using var router = new TouchZoneRouter();
        router.SetZone(new TouchZone("top", new TouchZoneRect(0f, 0f, 1f, 0.5f), behavior: TouchZoneBehavior.Lock));
        var touches = new List<TouchZoneTouchEvent>();
        router.TouchRaised += value => touches.Add(value);

        router.Process(Touch(EventType.FingerDown, Ms(0), 13, 103, 0.50f, 0.80f));
        router.Process(Touch(EventType.FingerMotion, Ms(100), 13, 103, 0.50f, 0.20f));
        router.Process(Touch(EventType.FingerUp, Ms(140), 13, 103, 0.50f, 0.20f));

        touches.Should().HaveCount(3);
        touches[0].Zone.IsAssigned.Should().BeFalse();
        touches[1].Zone.IsAssigned.Should().BeFalse();
        touches[2].Zone.IsAssigned.Should().BeFalse();
    }

    [Fact]
    public void TwoFingerSwipeAcrossDifferentZones_ReportsUnassignedZone()
    {
        var recognizer = new GestureRecognizer(new GestureOptions
        {
            SwipeMinDistance = 0.05f,
            SwipeMinVelocity = 0.1f
        });
        using var router = new TouchZoneRouter(recognizer);
        router.SetZone(new TouchZone("left", new TouchZoneRect(0f, 0f, 0.5f, 1f), behavior: TouchZoneBehavior.Lock));
        router.SetZone(new TouchZone("right", new TouchZoneRect(0.5f, 0f, 0.5f, 1f), behavior: TouchZoneBehavior.Lock));
        var gestures = new List<TouchZoneGestureEvent>();
        router.GestureRaised += value => gestures.Add(value);

        router.Process(Touch(EventType.FingerDown, Ms(0), 20, 201, 0.30f, 0.80f));
        router.Process(Touch(EventType.FingerDown, Ms(10), 20, 202, 0.70f, 0.80f));
        router.Process(Touch(EventType.FingerMotion, Ms(80), 20, 201, 0.30f, 0.20f));
        router.Process(Touch(EventType.FingerMotion, Ms(90), 20, 202, 0.70f, 0.20f));
        router.Process(Touch(EventType.FingerUp, Ms(130), 20, 201, 0.30f, 0.20f));
        router.Process(Touch(EventType.FingerUp, Ms(140), 20, 202, 0.70f, 0.20f));

        var swipe = gestures.Should().ContainSingle(x => x.Gesture.Kind == GestureKind.Swipe && x.Gesture.FingerCount == 2).Subject;
        swipe.Zone.IsAssigned.Should().BeFalse();
    }

    [Fact]
    public void TwoFingerSwipeInSameZone_ReportsAssignedZone()
    {
        var recognizer = new GestureRecognizer(new GestureOptions
        {
            SwipeMinDistance = 0.05f,
            SwipeMinVelocity = 0.1f
        });
        using var router = new TouchZoneRouter(recognizer);
        router.SetZone(new TouchZone("left", new TouchZoneRect(0f, 0f, 0.5f, 1f), behavior: TouchZoneBehavior.Lock));
        router.SetZone(new TouchZone("right", new TouchZoneRect(0.5f, 0f, 0.5f, 1f), behavior: TouchZoneBehavior.Lock));
        var gestures = new List<TouchZoneGestureEvent>();
        router.GestureRaised += value => gestures.Add(value);

        router.Process(Touch(EventType.FingerDown, Ms(0), 21, 211, 0.25f, 0.80f));
        router.Process(Touch(EventType.FingerDown, Ms(10), 21, 212, 0.35f, 0.80f));
        router.Process(Touch(EventType.FingerMotion, Ms(80), 21, 211, 0.25f, 0.20f));
        router.Process(Touch(EventType.FingerMotion, Ms(90), 21, 212, 0.35f, 0.20f));
        router.Process(Touch(EventType.FingerUp, Ms(130), 21, 211, 0.25f, 0.20f));
        router.Process(Touch(EventType.FingerUp, Ms(140), 21, 212, 0.35f, 0.20f));

        var swipe = gestures.Should().ContainSingle(x => x.Gesture.Kind == GestureKind.Swipe && x.Gesture.FingerCount == 2).Subject;
        swipe.Zone.ZoneId.Should().Be("left");
    }

    private static Event Touch(EventType type, ulong timestamp, ulong touchId, ulong fingerId, float x, float y)
    {
        return new Event
        {
            TouchFinger = new TouchFingerEvent
            {
                Type = type,
                Timestamp = timestamp,
                TouchId = touchId,
                FingerId = fingerId,
                X = x,
                Y = y,
                DX = 0f,
                DY = 0f,
                Pressure = 1f,
                WindowId = 1
            }
        };
    }

    private static ulong Ms(int value)
    {
        return (ulong)value * 1000000UL;
    }
}
