namespace TopSpeed.Input
{
    internal readonly struct InputActionDefinition
    {
        public InputActionDefinition(InputAction action, string label)
        {
            Action = action;
            Label = label;
        }

        public InputAction Action { get; }
        public string Label { get; }
    }
}

