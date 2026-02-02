namespace TopSpeed.Vehicles
{
    internal static class VehicleAudioHeights
    {
        private const float DefaultHeightOffset = 0.5f;
        private const float DefaultVehicleHeight = 1.0f;
        private const float DefaultEngineHeight = 0.4f;

        public static float ResolveVehicleHeight(VehicleDefinition definition)
        {
            if (definition != null && definition.VehicleHeightM > 0f)
                return definition.VehicleHeightM;
            if (definition != null && definition.CgHeightM > 0f)
                return definition.CgHeightM + DefaultHeightOffset;
            return DefaultVehicleHeight;
        }

        public static float ResolveHornHeight(VehicleDefinition definition, float vehicleHeight)
        {
            if (definition != null && definition.HornHeightM > 0f)
                return definition.HornHeightM;
            return vehicleHeight > 0f ? vehicleHeight : DefaultVehicleHeight;
        }

        public static float ResolveEngineHeight(VehicleDefinition definition)
        {
            if (definition != null && definition.EngineHeightM > 0f)
                return definition.EngineHeightM;
            return DefaultEngineHeight;
        }
    }
}
