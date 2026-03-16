using System;
using TopSpeed.Physics.Powertrain;

namespace TopSpeed.Bots
{
    public static partial class BotPhysics
    {
        private static float CalculateDriveRpm(BotPhysicsConfig config, int gear, float speedMps, float throttle)
        {
            return Calculator.DriveRpm(
                config.Powertrain,
                gear,
                speedMps,
                throttle,
                inReverse: false);
        }

        private static float CalculateEngineTorqueNm(BotPhysicsConfig config, float rpm)
        {
            return Calculator.EngineTorque(config.Powertrain, rpm);
        }
    }
}


