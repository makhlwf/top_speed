using Eto.Forms;
using TopSpeed.Runtime;

namespace TopSpeed.Windowing.Eto
{
    internal sealed class ClipboardService : IClipboardService
    {
        public bool TrySetText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            try
            {
                var app = Application.Instance;
                if (app == null)
                    return false;

                var copied = false;
                app.Invoke(() =>
                {
                    try
                    {
                        Clipboard.Instance.Text = text;
                        copied = true;
                    }
                    catch
                    {
                        copied = false;
                    }
                });
                return copied;
            }
            catch
            {
                return false;
            }
        }
    }
}
