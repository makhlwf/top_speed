using System;
using TopSpeed.Physics.Powertrain;

namespace TopSpeed.Bots
{
    public static partial class BotPhysics
    {
        private static float CalculateBrakeDecel(BotPhysicsConfig config, float brakeInput, float surfaceDecelMod)
        {
            return Calculator.BrakeDecelKph(
                config.Powertrain,
                brakeInput,
                surfaceDecelMod);
        }

        private static float CalculateEngineBrakingDecel(BotPhysicsConfig config, int gear, float speedMps, float surfaceDecelMod)
        {
            return Calculator.EngineBrakeDecelKph(
                config.Powertrain,
                gear,
                inReverse: false,
                speedMps,
                surfaceDecelMod,
                SpeedToRpm(config, speedMps, gear));
        }
    }
}


