using System;
using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Menu;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void RebuildSavedServerFormMenu()
        {
            var controls = new[]
            {
                new MenuFormControl(
                    () => string.IsNullOrWhiteSpace(_savedServerDraft.Name)
                        ? "Server name, currently empty."
                        : $"Server name, currently set to {_savedServerDraft.Name}",
                    UpdateSavedServerDraftName),
                new MenuFormControl(
                    () => string.IsNullOrWhiteSpace(_savedServerDraft.Host)
                        ? "Server IP or host, currently empty."
                        : $"Server IP or host, currently set to {_savedServerDraft.Host}",
                    UpdateSavedServerDraftHost),
                new MenuFormControl(
                    () => _savedServerDraft.Port > 0
                        ? $"Server port, currently set to {_savedServerDraft.Port}"
                        : "Server port, currently empty.",
                    UpdateSavedServerDraftPort)
            };

            var saveLabel = _savedServerEditIndex >= 0 ? "Save server changes" : "Save server";
            var items = MenuFormBuilder.BuildItems(
                controls,
                saveLabel,
                SaveSavedServerDraft,
                "Go back");
            _menu.UpdateItems(MultiplayerSavedServerFormMenuId, items, preserveSelection: true);
        }

        private void CloseSavedServerForm()
        {
            if (!IsSavedServerDraftDirty())
            {
                _menu.PopToPrevious();
                return;
            }

            _questions.Show(new Question(
                "Save changes before closing?",
                "Are you sure you would like to discard all changes?.",
                HandleSavedServerDiscardQuestionResult,
                new QuestionButton(QuestionId.Confirm, "Save changes", flags: QuestionButtonFlags.Default),
                new QuestionButton(QuestionId.Close, "Discard changes")));
        }

        private bool IsSavedServerDraftDirty()
        {
            var current = NormalizeSavedServerDraft(_savedServerDraft);
            var original = NormalizeSavedServerDraft(_savedServerOriginal ?? new SavedServerEntry());

            if (_savedServerEditIndex < 0)
                return !string.IsNullOrWhiteSpace(current.Host) || !string.IsNullOrWhiteSpace(current.Name) || current.Port != 0;

            return !string.Equals(current.Name, original.Name, StringComparison.Ordinal)
                || !string.Equals(current.Host, original.Host, StringComparison.OrdinalIgnoreCase)
                || current.Port != original.Port;
        }

        private void DiscardSavedServerDraftChanges()
        {
            if (_questions.IsQuestionMenu(_menu.CurrentId))
                _menu.PopToPrevious();
            if (string.Equals(_menu.CurrentId, MultiplayerSavedServerFormMenuId, StringComparison.Ordinal))
                _menu.PopToPrevious();
        }

        private void SaveSavedServerDraft()
        {
            var normalized = NormalizeSavedServerDraft(_savedServerDraft);
            if (string.IsNullOrWhiteSpace(normalized.Host))
            {
                _speech.Speak("Server IP or host cannot be empty.");
                return;
            }

            var servers = _settings.SavedServers ?? (_settings.SavedServers = new List<SavedServerEntry>());
            if (_savedServerEditIndex >= 0 && _savedServerEditIndex < servers.Count)
                servers[_savedServerEditIndex] = normalized;
            else
                servers.Add(normalized);

            _saveSettings();
            RebuildSavedServersMenu();

            if (_questions.IsQuestionMenu(_menu.CurrentId))
                _menu.PopToPrevious();
            if (string.Equals(_menu.CurrentId, MultiplayerSavedServerFormMenuId, StringComparison.Ordinal))
                _menu.PopToPrevious();

            _speech.Speak("Server saved.");
        }
    }
}
