namespace TopSpeed.Physics.Torque
{
    public readonly struct CurvePoint
    {
        public CurvePoint(float rpm, float torqueNm)
        {
            Rpm = rpm;
            TorqueNm = torqueNm;
        }

        public float Rpm { get; }
        public float TorqueNm { get; }
    }
}
