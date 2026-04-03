namespace TopSpeed.Vehicles
{
    internal static class GearSpeedLimiter
    {
        private const float OverspeedCoastMarginKph = 0.25f;
        private const float CoupledThreshold = 0.05f;

        internal static float ApplyForwardGearLimit(float speedBeforeKph, float speedAfterKph, float gearMaxKph)
        {
            if (gearMaxKph <= 0f)
                return speedAfterKph;
            if (speedAfterKph <= gearMaxKph)
                return speedAfterKph;

            // Normal acceleration crossing this gear's ceiling: clamp to the ceiling.
            if (speedBeforeKph <= gearMaxKph)
                return gearMaxKph;

            // Already overspeed in this gear (for example, quick downshift). Prevent
            // extra acceleration, but do not teleport speed down to the gear max.
            if (speedAfterKph > speedBeforeKph)
                return speedBeforeKph;

            return speedAfterKph;
        }

        internal static bool ShouldForceOverspeedCoast(
            float speedKph,
            float gearMaxKph,
            float drivelineCouplingFactor,
            bool forwardDriveGearActive,
            bool manualShiftControlActive)
        {
            if (!forwardDriveGearActive || !manualShiftControlActive)
                return false;
            if (gearMaxKph <= 0f)
                return false;
            if (drivelineCouplingFactor <= CoupledThreshold)
                return false;

            return speedKph > gearMaxKph + OverspeedCoastMarginKph;
        }
    }
}

