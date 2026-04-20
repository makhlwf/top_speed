using System;
using System.Collections.Generic;
using TS.Sdl.Events;

namespace TS.Sdl.Input
{
    public sealed class TouchZoneRouter : IDisposable
    {
        private readonly GestureRecognizer _recognizer;
        private readonly Dictionary<ulong, TouchTrack> _touches;
        private bool _disposed;

        public TouchZoneRouter()
            : this(null)
        {
        }

        public TouchZoneRouter(GestureRecognizer? recognizer)
        {
            Zones = new TouchZoneRegistry();
            _recognizer = recognizer ?? new GestureRecognizer();
            _recognizer.Raised += OnGestureRaised;
            _touches = new Dictionary<ulong, TouchTrack>();
        }

        public GestureRecognizer Recognizer => _recognizer;
        public TouchZoneRegistry Zones { get; }

        public event Action<TouchZoneTouchEvent>? TouchRaised;
        public event Action<TouchZoneGestureEvent>? GestureRaised;

        public void SetZone(in TouchZone zone) => Zones.Set(zone);
        public bool RemoveZone(string id) => Zones.Remove(id);

        public void ClearZones()
        {
            Zones.Clear();
            _touches.Clear();
        }

        public void Reset()
        {
            _touches.Clear();
            _recognizer.Reset();
        }

        public void Update()
        {
            ThrowIfDisposed();
            _recognizer.Update();
        }

        public void Update(ulong timestamp)
        {
            ThrowIfDisposed();
            _recognizer.Update(timestamp);
        }

        public void Process(in Event value)
        {
            ThrowIfDisposed();
            var type = (EventType)value.Type;
            switch (type)
            {
                case EventType.FingerDown:
                    HandleFingerDown(value.TouchFinger);
                    _recognizer.Process(value);
                    return;

                case EventType.FingerMotion:
                    HandleFingerMotion(value.TouchFinger);
                    _recognizer.Process(value);
                    return;

                case EventType.FingerUp:
                case EventType.FingerCanceled:
                    HandleFingerUp(type, value.TouchFinger);
                    _recognizer.Process(value);
                    CleanupTouch(value.TouchFinger.TouchId);
                    return;

                default:
                    _recognizer.Process(value);
                    return;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _recognizer.Raised -= OnGestureRaised;
            _touches.Clear();
        }

        private void HandleFingerDown(in TouchFingerEvent value)
        {
            var track = GetOrCreateTrack(value.TouchId);
            var hit = ResolveHit(value.X, value.Y, out var zone);
            var behavior = zone.HasValue ? zone.Value.Behavior : TouchZoneBehavior.Lock;
            var state = new FingerState(hit, behavior, down: true);
            track.Fingers[value.FingerId] = state;
            TouchRaised?.Invoke(new TouchZoneTouchEvent(EventType.FingerDown, value, hit));
        }

        private void HandleFingerMotion(in TouchFingerEvent value)
        {
            if (!_touches.TryGetValue(value.TouchId, out var track))
                track = GetOrCreateTrack(value.TouchId);

            if (!track.Fingers.TryGetValue(value.FingerId, out var state))
            {
                var fallback = ResolveHit(value.X, value.Y, out _);
                TouchRaised?.Invoke(new TouchZoneTouchEvent(EventType.FingerMotion, value, fallback));
                return;
            }

            if (state.Behavior == TouchZoneBehavior.Dynamic)
                state.Zone = ResolveHit(value.X, value.Y, out _);

            state.Down = true;
            track.Fingers[value.FingerId] = state;
            TouchRaised?.Invoke(new TouchZoneTouchEvent(EventType.FingerMotion, value, state.Zone));
        }

        private void HandleFingerUp(EventType type, in TouchFingerEvent value)
        {
            var hit = TouchZoneHit.None;
            if (_touches.TryGetValue(value.TouchId, out var track) &&
                track.Fingers.TryGetValue(value.FingerId, out var state))
            {
                state.Down = false;
                track.Fingers[value.FingerId] = state;
                hit = state.Zone;
            }
            else
            {
                hit = ResolveHit(value.X, value.Y, out _);
            }

            TouchRaised?.Invoke(new TouchZoneTouchEvent(type, value, hit));
        }

        private TouchZoneHit ResolveGestureZone(in GestureEvent value)
        {
            if (value.FingerCount <= 1)
            {
                if (_touches.TryGetValue(value.TouchId, out var track) &&
                    track.Fingers.TryGetValue(value.FingerId, out var state))
                    return state.Zone;

                return ResolveHit(value.X, value.Y, out _);
            }

            if (!_touches.TryGetValue(value.TouchId, out var multiTrack))
                return ResolveHit(value.X, value.Y, out _);

            var foundAssigned = false;
            var foundUnassigned = false;
            var matchedZoneId = string.Empty;
            var matched = TouchZoneHit.None;
            foreach (var finger in multiTrack.Fingers.Values)
            {
                if (!finger.Zone.IsAssigned)
                {
                    foundUnassigned = true;
                    continue;
                }

                if (!foundAssigned)
                {
                    foundAssigned = true;
                    matchedZoneId = finger.Zone.ZoneId ?? string.Empty;
                    matched = finger.Zone;
                    continue;
                }

                if (!string.Equals(matchedZoneId, finger.Zone.ZoneId, StringComparison.Ordinal))
                    return TouchZoneHit.None;
            }

            if (!foundAssigned || foundUnassigned)
                return TouchZoneHit.None;

            return matched;
        }

        private void OnGestureRaised(GestureEvent value)
        {
            var zone = ResolveGestureZone(value);
            GestureRaised?.Invoke(new TouchZoneGestureEvent(value, zone));
        }

        private TouchTrack GetOrCreateTrack(ulong touchId)
        {
            if (_touches.TryGetValue(touchId, out var existing))
                return existing;

            var track = new TouchTrack();
            _touches.Add(touchId, track);
            return track;
        }

        private TouchZoneHit ResolveHit(float x, float y, out TouchZone? zone)
        {
            if (Zones.TryResolve(x, y, out var resolved))
            {
                zone = resolved;
                return TouchZoneHit.From(resolved);
            }

            zone = null;
            return TouchZoneHit.None;
        }

        private void CleanupTouch(ulong touchId)
        {
            if (!_touches.TryGetValue(touchId, out var track))
                return;

            foreach (var state in track.Fingers.Values)
            {
                if (state.Down)
                    return;
            }

            _touches.Remove(touchId);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TouchZoneRouter));
        }

        private sealed class TouchTrack
        {
            public Dictionary<ulong, FingerState> Fingers { get; } = new Dictionary<ulong, FingerState>();
        }

        private struct FingerState
        {
            public FingerState(TouchZoneHit zone, TouchZoneBehavior behavior, bool down)
            {
                Zone = zone;
                Behavior = behavior;
                Down = down;
            }

            public TouchZoneHit Zone;
            public TouchZoneBehavior Behavior;
            public bool Down;
        }
    }
}
