using TS.Sdl.Input;

namespace TopSpeed.Input
{
    internal static class GestureIntentMapper
    {
        public static bool TryMap(in GestureEvent value, out GestureIntent intent)
        {
            switch (value.Kind)
            {
                case GestureKind.Swipe:
                    var threeFinger = value.FingerCount >= 3;
                    var twoFinger = value.FingerCount >= 2;
                    intent = value.Direction switch
                    {
                        SwipeDirection.Left => threeFinger ? GestureIntent.ThreeFingerSwipeLeft : (twoFinger ? GestureIntent.TwoFingerSwipeLeft : GestureIntent.SwipeLeft),
                        SwipeDirection.Right => threeFinger ? GestureIntent.ThreeFingerSwipeRight : (twoFinger ? GestureIntent.TwoFingerSwipeRight : GestureIntent.SwipeRight),
                        SwipeDirection.Up => threeFinger ? GestureIntent.ThreeFingerSwipeUp : (twoFinger ? GestureIntent.TwoFingerSwipeUp : GestureIntent.SwipeUp),
                        SwipeDirection.Down => threeFinger ? GestureIntent.ThreeFingerSwipeDown : (twoFinger ? GestureIntent.TwoFingerSwipeDown : GestureIntent.SwipeDown),
                        _ => GestureIntent.Unknown
                    };
                    return intent != GestureIntent.Unknown;

                case GestureKind.Tap:
                    intent = value.FingerCount >= 3
                        ? GestureIntent.ThreeFingerTap
                        : GestureIntent.Tap;
                    return true;

                case GestureKind.DoubleTap:
                    intent = value.FingerCount >= 3
                        ? GestureIntent.ThreeFingerDoubleTap
                        : GestureIntent.DoubleTap;
                    return true;

                case GestureKind.TripleTap:
                    intent = value.FingerCount >= 3
                        ? GestureIntent.ThreeFingerTripleTap
                        : GestureIntent.TripleTap;
                    return true;

                case GestureKind.LongPress:
                    intent = GestureIntent.LongPress;
                    return true;

                case GestureKind.TwoFingerTap:
                    intent = GestureIntent.TwoFingerTap;
                    return true;

                case GestureKind.TwoFingerDoubleTap:
                    intent = GestureIntent.TwoFingerDoubleTap;
                    return true;

                case GestureKind.TwoFingerTripleTap:
                    intent = GestureIntent.TwoFingerTripleTap;
                    return true;

                default:
                    intent = GestureIntent.Unknown;
                    return false;
            }
        }
    }
}
