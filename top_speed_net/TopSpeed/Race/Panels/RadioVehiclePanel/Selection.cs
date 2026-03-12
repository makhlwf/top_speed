using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace TopSpeed.Race.Panels
{
    internal sealed partial class RadioVehiclePanel
    {
        private void OpenRadioMedia()
        {
            if (_pickerInProgress)
                return;

            _pickerInProgress = true;
            BeginShowMediaPickerDialog(selectedPath =>
            {
                lock (_pendingPathLock)
                    _pendingSelectedPath = selectedPath;

                _pickerInProgress = false;
            });
        }

        private void OpenRadioFolder()
        {
            if (_folderPickerInProgress)
                return;

            _folderPickerInProgress = true;
            BeginShowFolderPickerDialog(_playlistFolder, selectedFolder =>
            {
                lock (_pendingPathLock)
                    _pendingSelectedFolder = selectedFolder;

                _folderPickerInProgress = false;
            });
        }

        private void ProcessPendingSelection()
        {
            string? selectedPath;
            lock (_pendingPathLock)
            {
                selectedPath = _pendingSelectedPath;
                _pendingSelectedPath = null;
            }

            if (string.IsNullOrWhiteSpace(selectedPath))
                return;

            var fullPath = Path.GetFullPath(selectedPath);
            if (!File.Exists(fullPath))
            {
                _announce("The selected media file does not exist.");
                return;
            }

            _playlist.Clear();
            _playlist.Add(fullPath);
            _playlistIndex = 0;
            _playlistFolder = string.Empty;
            ApplyLoopMode();

            LoadPlaylistEntry(_playlistIndex, preservePlaybackState: true, announceLoaded: true);
        }

        private void ProcessPendingFolderSelection()
        {
            string? selectedFolder;
            lock (_pendingPathLock)
            {
                selectedFolder = _pendingSelectedFolder;
                _pendingSelectedFolder = null;
            }

            if (string.IsNullOrWhiteSpace(selectedFolder))
                return;

            var folderPath = selectedFolder!;
            if (!BuildPlaylistFromFolder(folderPath, preserveCurrentMedia: false, announceErrors: true))
                return;

            if (!LoadPlaylistEntry(_playlistIndex, preservePlaybackState: true, announceLoaded: true))
                return;

            _announce($"Shuffle mode {(_shuffleMode ? "on" : "off")}.");
        }

        private void HandlePlaybackEndAdvance()
        {
            var isPlaying = _radio.HasMedia && _radio.IsPlaying;
            if (_radio.HasMedia && _radio.DesiredPlaying && _lastObservedPlaying && !isPlaying)
            {
                if (_playlist.Count > 1 && !_radio.LoopPlayback)
                {
                    if (StepPlaylistIndex(1))
                        LoadPlaylistEntry(_playlistIndex, preservePlaybackState: true, announceLoaded: true);
                }
            }

            _lastObservedPlaying = isPlaying;
        }

        private static void BeginShowMediaPickerDialog(Action<string?> onCompleted)
        {
            void ShowDialog()
            {
                string? selectedPath = null;
                using (var dialog = new OpenFileDialog())
                {
                    dialog.CheckFileExists = true;
                    dialog.CheckPathExists = true;
                    dialog.Multiselect = false;
                    dialog.Title = "Select radio media file";
                    dialog.Filter = "Audio files|*.wav;*.ogg;*.mp3;*.flac;*.aac;*.m4a|All files|*.*";

                    var owner = GetDialogOwner();
                    var result = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
                    if (result == DialogResult.OK)
                        selectedPath = dialog.FileName;
                }

                onCompleted(selectedPath);
            }

            var ownerWindow = GetDialogOwnerForm();
            if (ownerWindow != null && ownerWindow.IsHandleCreated && !ownerWindow.IsDisposed)
            {
                ownerWindow.BeginInvoke((Action)ShowDialog);
                return;
            }

            var thread = new Thread(() => ShowDialog())
            {
                IsBackground = true,
                Name = "RadioMediaPicker"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private static void BeginShowFolderPickerDialog(string currentFolder, Action<string?> onCompleted)
        {
            void ShowDialog()
            {
                string? selectedFolder = null;
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "Select radio media folder";
                    dialog.ShowNewFolderButton = false;
                    if (!string.IsNullOrWhiteSpace(currentFolder) && Directory.Exists(currentFolder))
                        dialog.SelectedPath = currentFolder;

                    var owner = GetDialogOwner();
                    var result = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
                    if (result == DialogResult.OK)
                        selectedFolder = dialog.SelectedPath;
                }

                onCompleted(selectedFolder);
            }

            var ownerWindow = GetDialogOwnerForm();
            if (ownerWindow != null && ownerWindow.IsHandleCreated && !ownerWindow.IsDisposed)
            {
                ownerWindow.BeginInvoke((Action)ShowDialog);
                return;
            }

            var thread = new Thread(() => ShowDialog())
            {
                IsBackground = true,
                Name = "RadioFolderPicker"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private static Form? GetDialogOwnerForm()
        {
            if (Application.OpenForms.Count == 0)
                return null;

            return Application.OpenForms[0];
        }

        private static IWin32Window? GetDialogOwner() => GetDialogOwnerForm();
    }
}
