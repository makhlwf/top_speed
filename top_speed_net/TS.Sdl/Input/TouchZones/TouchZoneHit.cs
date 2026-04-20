namespace TS.Sdl.Input
{
    public readonly struct TouchZoneHit
    {
        public TouchZoneHit(string? zoneId, int priority, bool assigned)
        {
            if (assigned && string.IsNullOrWhiteSpace(zoneId))
            {
                ZoneId = null;
                Priority = 0;
                IsAssigned = false;
                return;
            }

            ZoneId = assigned ? zoneId : null;
            Priority = assigned ? priority : 0;
            IsAssigned = assigned;
        }

        public string? ZoneId { get; }
        public int Priority { get; }
        public bool IsAssigned { get; }

        public static TouchZoneHit None => default;

        public static TouchZoneHit From(in TouchZone zone)
        {
            return new TouchZoneHit(zone.Id, zone.Priority, assigned: true);
        }
    }
}

