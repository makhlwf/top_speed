using System;

namespace TopSpeed.Physics.Torque
{
    public static class PresetCatalog
    {
        public static readonly string[] CanonicalNames =
        {
            "city_compact",
            "family_sedan",
            "sport_sedan",
            "sport_coupe",
            "grand_tourer",
            "hot_hatch",
            "muscle_v8",
            "supercar_na",
            "supercar_turbo",
            "rally_turbo",
            "diesel_suv",
            "diesel_truck",
            "supersport_bike",
            "naked_bike"
        };

        public static string NamesText => string.Join(", ", CanonicalNames);

        public static bool TryNormalize(string? preset, out string normalizedName)
        {
            normalizedName = string.Empty;
            if (string.IsNullOrWhiteSpace(preset))
                return false;

            var raw = (preset ?? string.Empty).Trim().ToLowerInvariant();
            for (var i = 0; i < CanonicalNames.Length; i++)
            {
                if (string.Equals(raw, CanonicalNames[i], StringComparison.Ordinal))
                {
                    normalizedName = CanonicalNames[i];
                    return true;
                }
            }

            return false;
        }

        public static PresetShape Get(string presetName)
        {
            switch (presetName)
            {
                case "city_compact":
                    return new PresetShape(1.00f, 1.55f, 0.42f, 0.58f);
                case "family_sedan":
                    return new PresetShape(1.05f, 1.35f, 0.38f, 0.64f);
                case "sport_sedan":
                    return new PresetShape(1.15f, 1.25f, 0.34f, 0.70f);
                case "sport_coupe":
                    return new PresetShape(1.24f, 1.18f, 0.32f, 0.74f);
                case "grand_tourer":
                    return new PresetShape(1.08f, 1.16f, 0.36f, 0.73f);
                case "hot_hatch":
                    return new PresetShape(1.10f, 1.28f, 0.35f, 0.68f);
                case "muscle_v8":
                    return new PresetShape(0.90f, 1.22f, 0.44f, 0.66f);
                case "supercar_na":
                    return new PresetShape(1.34f, 1.10f, 0.28f, 0.77f);
                case "supercar_turbo":
                    return new PresetShape(0.88f, 1.40f, 0.34f, 0.66f);
                case "rally_turbo":
                    return new PresetShape(0.86f, 1.38f, 0.36f, 0.64f);
                case "diesel_suv":
                    return new PresetShape(0.80f, 1.90f, 0.50f, 0.56f);
                case "diesel_truck":
                    return new PresetShape(0.66f, 2.05f, 0.58f, 0.52f);
                case "supersport_bike":
                    return new PresetShape(1.62f, 1.14f, 0.26f, 0.69f);
                case "naked_bike":
                    return new PresetShape(1.45f, 1.20f, 0.30f, 0.67f);
                default:
                    return new PresetShape(1.05f, 1.35f, 0.38f, 0.64f);
            }
        }
    }

    public readonly struct PresetShape
    {
        public PresetShape(
            float riseExponent,
            float fallExponent,
            float idleTorqueFactor,
            float redlineTorqueFactor)
        {
            RiseExponent = riseExponent;
            FallExponent = fallExponent;
            IdleTorqueFactor = idleTorqueFactor;
            RedlineTorqueFactor = redlineTorqueFactor;
        }

        public float RiseExponent { get; }
        public float FallExponent { get; }
        public float IdleTorqueFactor { get; }
        public float RedlineTorqueFactor { get; }
    }
}
