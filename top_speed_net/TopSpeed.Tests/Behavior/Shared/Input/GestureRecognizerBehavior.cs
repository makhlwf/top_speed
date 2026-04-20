using System.Collections.Generic;
using TS.Sdl.Events;
using TS.Sdl.Input;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class GestureRecognizerBehaviorTests
{
    [Fact]
    public void TapThenSecondTapInWindow_RaisesDoubleTap()
    {
        var recognizer = new GestureRecognizer(new GestureOptions
        {
            DoubleTapGap = System.TimeSpan.FromMilliseconds(350)
        });
        var raised = new List<GestureEvent>();
        recognizer.Raised += value => raised.Add(value);

        recognizer.Process(Touch(EventType.FingerDown, Ms(0), 1, 10, 0.30f, 0.30f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(100), 1, 10, 0.31f, 0.30f));
        recognizer.Process(Touch(EventType.FingerDown, Ms(220), 1, 11, 0.32f, 0.31f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(300), 1, 11, 0.32f, 0.31f));

        raised.Should().HaveCount(2);
        raised[0].Kind.Should().Be(GestureKind.Tap);
        raised[1].Kind.Should().Be(GestureKind.DoubleTap);
    }

    [Fact]
    public void HoldPastThreshold_RaisesLongPressWithoutTap()
    {
        var recognizer = new GestureRecognizer(new GestureOptions
        {
            LongPressTime = System.TimeSpan.FromMilliseconds(400)
        });
        var raised = new List<GestureEvent>();
        recognizer.Raised += value => raised.Add(value);

        recognizer.Process(Touch(EventType.FingerDown, Ms(0), 1, 10, 0.40f, 0.40f));
        recognizer.Update(Ms(420));
        recognizer.Process(Touch(EventType.FingerUp, Ms(470), 1, 10, 0.40f, 0.40f));

        raised.Should().ContainSingle(x => x.Kind == GestureKind.LongPress);
        raised.Should().NotContain(x => x.Kind == GestureKind.Tap || x.Kind == GestureKind.DoubleTap);
    }

    [Fact]
    public void FastHorizontalMove_RaisesSwipeWithDirection()
    {
        var recognizer = new GestureRecognizer(new GestureOptions
        {
            SwipeMinDistance = 0.05f,
            SwipeMinVelocity = 0.1f
        });
        var raised = new List<GestureEvent>();
        recognizer.Raised += value => raised.Add(value);

        recognizer.Process(Touch(EventType.FingerDown, Ms(0), 2, 22, 0.20f, 0.50f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(120), 2, 22, 0.75f, 0.50f));

        var swipe = raised.Should().ContainSingle(x => x.Kind == GestureKind.Swipe).Subject;
        swipe.Direction.Should().Be(SwipeDirection.Right);
        swipe.DeltaX.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void TwoShortFingerTaps_RaisesTwoFingerTap()
    {
        var recognizer = new GestureRecognizer(new GestureOptions
        {
            TwoTapMaxTime = System.TimeSpan.FromMilliseconds(260),
            TwoTapMove = 0.03f
        });
        var raised = new List<GestureEvent>();
        recognizer.Raised += value => raised.Add(value);

        recognizer.Process(Touch(EventType.FingerDown, Ms(0), 3, 31, 0.40f, 0.50f));
        recognizer.Process(Touch(EventType.FingerDown, Ms(20), 3, 32, 0.60f, 0.50f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(120), 3, 31, 0.40f, 0.50f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(130), 3, 32, 0.60f, 0.50f));

        raised.Should().ContainSingle(x => x.Kind == GestureKind.TwoFingerTap);
        raised.Should().NotContain(x =>
            x.Kind == GestureKind.PinchBegin ||
            x.Kind == GestureKind.RotateBegin);
    }

    [Fact]
    public void TwoFingerParallelMove_RaisesTwoFingerSwipe()
    {
        var recognizer = new GestureRecognizer(new GestureOptions
        {
            SwipeMinDistance = 0.05f,
            SwipeMinVelocity = 0.1f,
            TwoTapMove = 0.02f
        });
        var raised = new List<GestureEvent>();
        recognizer.Raised += value => raised.Add(value);

        recognizer.Process(Touch(EventType.FingerDown, Ms(0), 7, 71, 0.40f, 0.70f));
        recognizer.Process(Touch(EventType.FingerDown, Ms(10), 7, 72, 0.60f, 0.70f));
        recognizer.Process(Touch(EventType.FingerMotion, Ms(80), 7, 71, 0.40f, 0.30f));
        recognizer.Process(Touch(EventType.FingerMotion, Ms(90), 7, 72, 0.60f, 0.30f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(130), 7, 71, 0.40f, 0.30f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(140), 7, 72, 0.60f, 0.30f));

        var swipe = raised.Should().ContainSingle(x => x.Kind == GestureKind.Swipe && x.FingerCount == 2).Subject;
        swipe.Direction.Should().Be(SwipeDirection.Up);
        swipe.DeltaY.Should().BeLessThan(0f);
    }

    [Fact]
    public void ThirdTapInSequence_RaisesTripleTap()
    {
        var recognizer = new GestureRecognizer(new GestureOptions
        {
            DoubleTapGap = System.TimeSpan.FromMilliseconds(350)
        });
        var raised = new List<GestureEvent>();
        recognizer.Raised += value => raised.Add(value);

        recognizer.Process(Touch(EventType.FingerDown, Ms(0), 5, 50, 0.30f, 0.30f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(80), 5, 50, 0.30f, 0.30f));
        recognizer.Process(Touch(EventType.FingerDown, Ms(180), 5, 51, 0.30f, 0.30f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(260), 5, 51, 0.30f, 0.30f));
        recognizer.Process(Touch(EventType.FingerDown, Ms(340), 5, 52, 0.30f, 0.30f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(420), 5, 52, 0.30f, 0.30f));

        raised.Should().Contain(x => x.Kind == GestureKind.Tap && x.TapCount == 1);
        raised.Should().Contain(x => x.Kind == GestureKind.DoubleTap && x.TapCount == 2);
        raised.Should().Contain(x => x.Kind == GestureKind.TripleTap && x.TapCount == 3);
    }

    [Fact]
    public void RepeatedTwoFingerTap_RaisesTwoFingerDoubleTap()
    {
        var recognizer = new GestureRecognizer(new GestureOptions
        {
            TwoTapMaxTime = System.TimeSpan.FromMilliseconds(240),
            TwoTapMove = 0.03f,
            DoubleTapGap = System.TimeSpan.FromMilliseconds(320),
            DoubleTapMove = 0.05f
        });
        var raised = new List<GestureEvent>();
        recognizer.Raised += value => raised.Add(value);

        recognizer.Process(Touch(EventType.FingerDown, Ms(0), 6, 61, 0.40f, 0.50f));
        recognizer.Process(Touch(EventType.FingerDown, Ms(15), 6, 62, 0.60f, 0.50f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(90), 6, 61, 0.40f, 0.50f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(100), 6, 62, 0.60f, 0.50f));

        recognizer.Process(Touch(EventType.FingerDown, Ms(220), 6, 63, 0.41f, 0.50f));
        recognizer.Process(Touch(EventType.FingerDown, Ms(235), 6, 64, 0.61f, 0.50f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(300), 6, 63, 0.41f, 0.50f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(310), 6, 64, 0.61f, 0.50f));

        raised.Should().Contain(x => x.Kind == GestureKind.TwoFingerTap && x.TapCount == 1);
        raised.Should().Contain(x => x.Kind == GestureKind.TwoFingerDoubleTap && x.TapCount == 2);
    }

    [Fact]
    public void TwoFingerScaleAndAngleChange_RaisesPinchAndRotateLifecycle()
    {
        var recognizer = new GestureRecognizer(new GestureOptions
        {
            PinchStartDistance = 0.01f,
            RotateStartRadians = 0.08f
        });
        var raised = new List<GestureEvent>();
        recognizer.Raised += value => raised.Add(value);

        recognizer.Process(Touch(EventType.FingerDown, Ms(0), 4, 41, 0.40f, 0.50f));
        recognizer.Process(Touch(EventType.FingerDown, Ms(10), 4, 42, 0.60f, 0.50f));
        recognizer.Process(Touch(EventType.FingerMotion, Ms(60), 4, 41, 0.35f, 0.45f));
        recognizer.Process(Touch(EventType.FingerMotion, Ms(70), 4, 42, 0.65f, 0.55f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(90), 4, 41, 0.35f, 0.45f));
        recognizer.Process(Touch(EventType.FingerUp, Ms(100), 4, 42, 0.65f, 0.55f));

        raised.Should().Contain(x => x.Kind == GestureKind.PinchBegin);
        raised.Should().Contain(x => x.Kind == GestureKind.PinchUpdate);
        raised.Should().Contain(x => x.Kind == GestureKind.PinchEnd);
        raised.Should().Contain(x => x.Kind == GestureKind.RotateBegin);
        raised.Should().Contain(x => x.Kind == GestureKind.RotateUpdate);
        raised.Should().Contain(x => x.Kind == GestureKind.RotateEnd);
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
