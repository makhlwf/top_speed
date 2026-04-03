namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
        private sealed class BotEvent
        {
            public float Time { get; set; }
            public BotEventType Type { get; set; }
        }

        private enum BotEventType
        {
            CarStart,
            CarComputerStart,
            CarRestart,
            InGear,
            StopHorn,
            StartHorn
        }

        internal enum ComputerState
        {
            Stopped,
            Starting,
            Running,
            Crashing,
            Stopping
        }
    }
}

