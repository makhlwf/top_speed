namespace TopSpeed.Vehicles.Control
{
    internal readonly struct CarControlContext
    {
        public CarControlContext(
            CarState state,
            bool started,
            bool manualTransmission,
            int gear,
            float speed,
            float positionX,
            float positionY,
            float elapsed)
        {
            State = state;
            Started = started;
            ManualTransmission = manualTransmission;
            Gear = gear;
            Speed = speed;
            PositionX = positionX;
            PositionY = positionY;
            Elapsed = elapsed;
        }

        public CarState State { get; }
        public bool Started { get; }
        public bool ManualTransmission { get; }
        public int Gear { get; }
        public float Speed { get; }
        public float PositionX { get; }
        public float PositionY { get; }
        public float Elapsed { get; }
    }
}

