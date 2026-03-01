using System.Collections.Generic;

namespace TopSpeed.Menu
{
    internal interface IMenuUiActions
    {
        void SpeakMessage(string text);
        void ShowMessageDialog(string title, string caption, IReadOnlyList<string> items);
        void SpeakNotImplemented();
    }
}
