using System.Threading;
using System.Windows.Forms;
using TopSpeed.Runtime;

namespace TopSpeed.Windowing.WinForms
{
    internal sealed class ClipboardService : IClipboardService
    {
        public bool TrySetText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (TrySetTextOnCurrentThread(text))
                return true;

            var success = false;
            using (var completed = new ManualResetEventSlim(false))
            {
                var thread = new Thread(() =>
                {
                    success = TrySetTextOnCurrentThread(text);
                    completed.Set();
                })
                {
                    IsBackground = true,
                    Name = "ClipboardCopy"
                };
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                completed.Wait(1000);
            }

            return success;
        }

        private static bool TrySetTextOnCurrentThread(string text)
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                try
                {
                    Clipboard.SetText(text, TextDataFormat.UnicodeText);
                    return true;
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    Thread.Sleep(20);
                }
                catch (ThreadStateException)
                {
                    return false;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}
