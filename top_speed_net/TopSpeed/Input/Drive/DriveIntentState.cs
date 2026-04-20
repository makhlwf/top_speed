namespace TopSpeed.Input
{
    internal readonly struct DriveIntentState
    {
        private readonly int _steering;
        private readonly int _throttle;
        private readonly int _brake;
        private readonly int _clutch;
        private readonly bool[] _triggered;

        public static DriveIntentState Empty { get; } = new DriveIntentState(0, 0, 0, 0, System.Array.Empty<bool>());

        public DriveIntentState(int steering, int throttle, int brake, int clutch, bool[] triggered)
        {
            _steering = steering;
            _throttle = throttle;
            _brake = brake;
            _clutch = clutch;
            _triggered = triggered;
        }

        public int GetAxisPercent(DriveIntent intent)
        {
            return intent switch
            {
                DriveIntent.Steering => _steering,
                DriveIntent.SteerLeft => _steering < 0 ? -_steering : 0,
                DriveIntent.SteerRight => _steering > 0 ? _steering : 0,
                DriveIntent.Throttle => _throttle,
                DriveIntent.Brake => _brake,
                DriveIntent.Clutch => _clutch,
                _ => 0
            };
        }

        public bool IsTriggered(DriveIntent intent)
        {
            var index = (int)intent;
            return index >= 0 && index < _triggered.Length && _triggered[index];
        }
    }
}
