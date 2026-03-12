using System;
using TopSpeed.Speech;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void UpdateSavedServerDraftName()
        {
            _promptTextInput(
                "Enter the server name.",
                _savedServerDraft.Name,
                SpeechService.SpeakFlag.None,
                true,
                result =>
                {
                    if (result.Cancelled)
                        return;

                    _savedServerDraft.Name = (result.Text ?? string.Empty).Trim();
                    RebuildSavedServerFormMenu();
                });
        }

        private void UpdateSavedServerDraftHost()
        {
            _promptTextInput(
                "Enter the server IP address or host name.",
                _savedServerDraft.Host,
                SpeechService.SpeakFlag.None,
                true,
                result =>
                {
                    if (result.Cancelled)
                        return;

                    _savedServerDraft.Host = (result.Text ?? string.Empty).Trim();
                    RebuildSavedServerFormMenu();
                });
        }

        private void UpdateSavedServerDraftPort()
        {
            var current = _savedServerDraft.Port > 0 ? _savedServerDraft.Port.ToString() : string.Empty;
            _promptTextInput(
                "Enter the server port, or leave empty for default.",
                current,
                SpeechService.SpeakFlag.None,
                true,
                result =>
                {
                    if (result.Cancelled)
                        return;

                    var trimmed = (result.Text ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        _savedServerDraft.Port = 0;
                        RebuildSavedServerFormMenu();
                        return;
                    }

                    if (!int.TryParse(trimmed, out var port) || port < 1 || port > 65535)
                    {
                        _speech.Speak("Invalid port. Enter a number between 1 and 65535.");
                        return;
                    }

                    _savedServerDraft.Port = port;
                    RebuildSavedServerFormMenu();
                });
        }
    }
}
