using System.Collections.Generic;
using TopSpeed.Input.Devices.Controller;
using TS.Sdl.Events;
using TS.Sdl.Input;

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

        public void SubmitGesture(in GestureEvent value)
        {
            if (_suspended || _disposed)
                return;
            if (!GestureIntentMapper.TryMap(in value, out var intent))
                return;

            lock (_gestureSync)
            {
                if (_gesturePressCounts.TryGetValue(intent, out var count))
                    _gesturePressCounts[intent] = count + 1;
                else
                    _gesturePressCounts[intent] = 1;
            }
        }

        public void SubmitTouchZoneGesture(in TouchZoneGestureEvent value)
        {
            if (_suspended || _disposed)
                return;
            if (!value.Zone.IsAssigned || string.IsNullOrWhiteSpace(value.Zone.ZoneId))
                return;
            var gesture = value.Gesture;
            if (!GestureIntentMapper.TryMap(in gesture, out var intent))
                return;

            var key = new ZoneGestureKey(intent, value.Zone.ZoneId!);
            lock (_gestureSync)
            {
                if (_zoneGesturePressCounts.TryGetValue(key, out var count))
                    _zoneGesturePressCounts[key] = count + 1;
                else
                    _zoneGesturePressCounts[key] = 1;
            }
        }

        public bool WasGesturePressed(GestureIntent intent)
        {
            if (_suspended)
                return false;
            if (intent == GestureIntent.Unknown)
                return false;

            lock (_gestureSync)
            {
                if (!_gesturePressCounts.TryGetValue(intent, out var count) || count <= 0)
                    return false;

                if (count == 1)
                    _gesturePressCounts.Remove(intent);
                else
                    _gesturePressCounts[intent] = count - 1;
                return true;
            }
        }

        public bool WasZoneGesturePressed(GestureIntent intent, string zoneId)
        {
            if (_suspended)
                return false;
            if (intent == GestureIntent.Unknown || string.IsNullOrWhiteSpace(zoneId))
                return false;

            var key = new ZoneGestureKey(intent, zoneId.Trim());
            lock (_gestureSync)
            {
                if (!_zoneGesturePressCounts.TryGetValue(key, out var count) || count <= 0)
                    return false;

                if (count == 1)
                    _zoneGesturePressCounts.Remove(key);
                else
                    _zoneGesturePressCounts[key] = count - 1;
                return true;
            }
        }

        public bool TryGetTouchZoneState(string zoneId, out TouchZoneState state)
        {
            state = TouchZoneState.Inactive;
            if (_suspended || string.IsNullOrWhiteSpace(zoneId))
                return false;

            var normalizedZoneId = zoneId.Trim();
            var hasMatch = false;
            var fingerCount = 0;
            var primaryStartX = 0f;
            var primaryStartY = 0f;
            var primaryX = 0f;
            var primaryY = 0f;
            var primaryPressure = 0f;
            var primaryStartTimestamp = 0UL;
            var primaryTimestamp = 0UL;

            lock (_gestureSync)
            {
                foreach (var touch in _zoneTouchPoints.Values)
                {
                    if (!string.Equals(touch.ZoneId, normalizedZoneId, System.StringComparison.Ordinal))
                        continue;

                    fingerCount++;
                    if (!hasMatch || touch.StartTimestamp < primaryStartTimestamp)
                    {
                        hasMatch = true;
                        primaryStartX = touch.StartX;
                        primaryStartY = touch.StartY;
                        primaryX = touch.X;
                        primaryY = touch.Y;
                        primaryPressure = touch.Pressure;
                        primaryStartTimestamp = touch.StartTimestamp;
                        primaryTimestamp = touch.Timestamp;
                    }
                }
            }

            if (!hasMatch)
                return false;

            state = new TouchZoneState(
                isActive: true,
                fingerCount,
                primaryStartX,
                primaryStartY,
                primaryX,
                primaryY,
                primaryPressure,
                primaryStartTimestamp,
                primaryTimestamp);
            return true;
        }

        public void SetTouchZones(IReadOnlyList<TouchZone> zones)
        {
            if (_touchZoneGestureEventSource == null || zones == null)
                return;

            _touchZoneGestureEventSource.SetTouchZones(zones);
        }

        public void ClearTouchZones()
        {
            _touchZoneGestureEventSource?.ClearTouchZones();
            lock (_gestureSync)
            {
                _gesturePressCounts.Clear();
                _zoneGesturePressCounts.Clear();
                _zoneTouchPoints.Clear();
            }
        }

        public bool TryGetControllerState(out State state)
        {
            return _controllerBackend.TryGetState(out state);
        }

        public DriveInputFrame CaptureDriveInputFrame()
        {
            var hasController = _controllerBackend.TryGetState(out var controllerState);
            return new DriveInputFrame(_current, hasController, controllerState, ActiveControllerIsRacingWheel);
        }

        public void ResetState()
        {
            _current.Clear();
            _previous.Clear();
            for (var i = 0; i < _keyLatch.Length; i++)
                _keyLatch[i] = false;
            lock (_gestureSync)
            {
                _gesturePressCounts.Clear();
                _zoneGesturePressCounts.Clear();
                _zoneTouchPoints.Clear();
            }
        }

        private void SubmitTouchZoneTouch(in TouchZoneTouchEvent value)
        {
            if (_suspended || _disposed)
                return;

            var key = new TouchPointKey(value.TouchId, value.FingerId);
            var isAssigned = value.Zone.IsAssigned && !string.IsNullOrWhiteSpace(value.Zone.ZoneId);
            var type = value.Type;
            lock (_gestureSync)
            {
                if (type == EventType.FingerUp || type == EventType.FingerCanceled)
                {
                    _zoneTouchPoints.Remove(key);
                    return;
                }

                if (!isAssigned)
                {
                    _zoneTouchPoints.Remove(key);
                    return;
                }

                var zoneId = value.Zone.ZoneId!.Trim();
                if (_zoneTouchPoints.TryGetValue(key, out var existing))
                {
                    _zoneTouchPoints[key] = new TouchPointState(
                        zoneId,
                        existing.StartX,
                        existing.StartY,
                        value.X,
                        value.Y,
                        value.Pressure,
                        existing.StartTimestamp,
                        value.Timestamp);
                    return;
                }

                _zoneTouchPoints[key] = new TouchPointState(
                    zoneId,
                    value.X,
                    value.Y,
                    value.X,
                    value.Y,
                    value.Pressure,
                    value.Timestamp,
                    value.Timestamp);
            }
        }
    }
}

