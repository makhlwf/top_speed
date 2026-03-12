using System;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Vehicles.Events
{
    internal sealed class Processor
    {
        private readonly Action _onCarStart;
        private readonly Action _onCarRestart;
        private readonly Action _onCrashComplete;
        private readonly Action _onInGear;
        private readonly Action<VibrationEffectType> _onStopVibration;
        private readonly Action _onStopBumpVibration;

        public Processor(
            Action onCarStart,
            Action onCarRestart,
            Action onCrashComplete,
            Action onInGear,
            Action<VibrationEffectType> onStopVibration,
            Action onStopBumpVibration)
        {
            _onCarStart = onCarStart;
            _onCarRestart = onCarRestart;
            _onCrashComplete = onCrashComplete;
            _onInGear = onInGear;
            _onStopVibration = onStopVibration;
            _onStopBumpVibration = onStopBumpVibration;
        }

        public void ProcessDue(EventQueue queue, float now)
        {
            queue.DrainDue(now, ProcessEntry);
        }

        private void ProcessEntry(EventEntry entry)
        {
            switch (entry.Type)
            {
                case EventType.CarStart:
                    _onCarStart();
                    break;
                case EventType.CarRestart:
                    _onCarRestart();
                    break;
                case EventType.CrashComplete:
                    _onCrashComplete();
                    break;
                case EventType.InGear:
                    _onInGear();
                    break;
                case EventType.StopVibration:
                    if (entry.Effect.HasValue)
                        _onStopVibration(entry.Effect.Value);
                    break;
                case EventType.StopBumpVibration:
                    _onStopBumpVibration();
                    break;
            }
        }
    }
}
