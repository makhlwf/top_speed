namespace TopSpeed.Input
{
    internal readonly struct DriveIntentDefinition
    {
        public DriveIntentDefinition(DriveIntent action, string label)
        {
            Action = action;
            Label = label;
        }

        public DriveIntent Action { get; }
        public string Label { get; }
    }
}

