using System;

namespace TopSpeed.Race
{
    internal abstract partial class RaceMode
    {
        protected void UpdateVehiclePanels(float elapsed)
        {
            if (!ReferenceEquals(_panelManager.ActivePanel, _radioPanel))
                _radioPanel.Tick(elapsed);

            var panelChanged = false;
            if (_input.GetPreviousPanelRequest())
            {
                _panelManager.MovePrevious();
                panelChanged = true;
            }
            else if (_input.GetNextPanelRequest())
            {
                _panelManager.MoveNext();
                panelChanged = true;
            }

            if (panelChanged)
            {
                ApplyActivePanelInputAccess();
                SpeakText(FormatPanelAnnouncement(_panelManager.ActivePanel.Name));
            }

            _panelManager.Update(elapsed);
        }

        protected void PauseVehiclePanels()
        {
            _panelManager.Pause();
        }

        protected void ResumeVehiclePanels()
        {
            _panelManager.Resume();
        }

        protected virtual void OnLocalRadioMediaLoaded(uint mediaId, string mediaPath)
        {
        }

        protected virtual void OnLocalRadioPlaybackChanged(bool loaded, bool playing, uint mediaId)
        {
        }

        private void ApplyActivePanelInputAccess()
        {
            var panel = _panelManager.ActivePanel;
            _input.SetPanelInputAccess(panel.AllowsDrivingInput, panel.AllowsAuxiliaryInput);
        }

        private uint NextLocalMediaId()
        {
            _nextMediaId++;
            if (_nextMediaId == 0)
                _nextMediaId = 1;
            return _nextMediaId;
        }

        private void HandleLocalRadioMediaLoaded(uint mediaId, string mediaPath)
        {
            OnLocalRadioMediaLoaded(mediaId, mediaPath);
        }

        private void HandleLocalRadioPlaybackChanged(bool loaded, bool playing, uint mediaId)
        {
            OnLocalRadioPlaybackChanged(loaded, playing, mediaId);
        }

        private static string FormatPanelAnnouncement(string panelName)
        {
            if (string.IsNullOrWhiteSpace(panelName))
                return "Panel";
            if (panelName.EndsWith("panel", StringComparison.OrdinalIgnoreCase))
                return panelName;
            return panelName + " panel";
        }
    }
}

