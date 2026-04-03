namespace TopSpeed.Runtime
{
    internal interface ITextInputService
    {
        void ShowTextInput(string? initialText);
        void HideTextInput();
        bool TryConsumeTextInput(out TextInputResult result);
    }
}
