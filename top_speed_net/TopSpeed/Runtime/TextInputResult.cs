namespace TopSpeed.Runtime
{
    internal readonly struct TextInputResult
    {
        private TextInputResult(bool cancelled, string text)
        {
            Cancelled = cancelled;
            Text = text;
        }

        public bool Cancelled { get; }
        public string Text { get; }

        public static TextInputResult Submitted(string text) => new TextInputResult(false, text ?? string.Empty);

        public static TextInputResult CreateCancelled() => new TextInputResult(true, string.Empty);
    }
}


