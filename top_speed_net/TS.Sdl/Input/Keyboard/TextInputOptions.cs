namespace TS.Sdl.Input
{
    public sealed class TextInputOptions
    {
        public TextInputType Type { get; set; } = TextInputType.Text;
        public Capitalization Capitalization { get; set; } = Capitalization.Sentences;
        public bool AutoCorrect { get; set; } = true;
        public bool MultiLine { get; set; } = true;
        public int? AndroidInputType { get; set; }
    }
}
