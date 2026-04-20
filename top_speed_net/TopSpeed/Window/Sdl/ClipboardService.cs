using TopSpeed.Runtime;
using SdlClipboard = TS.Sdl.Input.Clipboard;

namespace TopSpeed.Windowing.Sdl
{
    internal sealed class ClipboardService : IClipboardService
    {
        public bool TrySetText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return SdlClipboard.SetText(text);
        }
    }
}

