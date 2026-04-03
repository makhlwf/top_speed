namespace TopSpeed.Vehicles.Core
{
    internal sealed class CarRuntimeContext
    {
        public CarState State { get; set; }
        public bool Started { get; set; }
        public bool ManualTransmission { get; set; }
        public int Gear { get; set; }
        public float Speed { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float Elapsed { get; set; }
    }
}

