using System;
using System.Threading;
using TopSpeed.Core.Updates;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Protocol;
#if NETFRAMEWORK
using System.Windows.Forms;
#else
using EtoApplication = Eto.Forms.Application;
using EtoClipboard = Eto.Forms.Clipboard;
#endif

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private const int AboutCopyResultId = 1001;

        private void ShowAboutDialog()
        {
            var gameVersionLine = LocalizationService.Format(
                LocalizationService.Mark("Game version: {0}"),
                UpdateConfig.CurrentVersion.ToMachineString());
            var protocolVersionLine = LocalizationService.Format(
                LocalizationService.Mark("Protocol version: {0}"),
                ProtocolProfile.Current.ToMachineString());

            var copyText = string.Join(Environment.NewLine, gameVersionLine, protocolVersionLine);
            var dialog = new Dialog(
                LocalizationService.Mark("About"),
                null,
                QuestionId.Close,
                new[]
                {
                    new DialogItem(gameVersionLine),
                    new DialogItem(protocolVersionLine)
                },
                onResult: null,
                new DialogButton(
                    AboutCopyResultId,
                    LocalizationService.Mark("Copy"),
                    onClick: () => CopyAboutText(copyText),
                    flags: DialogButtonFlags.Default),
                new DialogButton(QuestionId.Close, LocalizationService.Mark("Close")));
            _dialogs.Show(dialog);
        }

        private void CopyAboutText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (TrySetClipboardText(text))
            {
                _speech.Speak(LocalizationService.Mark("Copied to clipboard."));
                return;
            }

            _speech.Speak(LocalizationService.Mark("Unable to copy to clipboard."));
        }

        private static bool TrySetClipboardText(string text)
        {
#if NETFRAMEWORK
            if (TrySetClipboardTextWinFormsDirect(text))
                return true;

            var success = false;
            using (var completed = new ManualResetEventSlim(false))
            {
                var thread = new Thread(() =>
                {
                    success = TrySetClipboardTextWinFormsDirect(text);
                    completed.Set();
                })
                {
                    IsBackground = true,
                    Name = "AboutClipboardCopy"
                };
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                completed.Wait(1000);
            }

            return success;
#else
            try
            {
                var app = EtoApplication.Instance;
                if (app == null)
                    return false;

                var copied = false;
                app.Invoke(() =>
                {
                    try
                    {
                        EtoClipboard.Instance.Text = text;
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
#endif
        }

#if NETFRAMEWORK
        private static bool TrySetClipboardTextWinFormsDirect(string text)
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
#endif
    }
}
