namespace TS.Sdl.Input
{
    public readonly struct TouchZoneGestureEvent
    {
        public TouchZoneGestureEvent(in GestureEvent gesture, TouchZoneHit zone)
        {
            Gesture = gesture;
            Zone = zone;
        }

        public GestureEvent Gesture { get; }
        public TouchZoneHit Zone { get; }
    }
}

