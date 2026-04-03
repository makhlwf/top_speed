using System;
using System.Collections.Generic;

namespace TopSpeed.Race.Panels
{
    internal interface IVehicleRacePanel : IDisposable
    {
        string Name { get; }
        bool AllowsDrivingInput { get; }
        bool AllowsAuxiliaryInput { get; }
        void Update(float elapsed);
        void Pause();
        void Resume();
    }

    internal sealed class VehiclePanelManager : IDisposable
    {
        private readonly List<IVehicleRacePanel> _panels;
        private int _activeIndex;

        public VehiclePanelManager(IEnumerable<IVehicleRacePanel> panels)
        {
            if (panels == null)
                throw new ArgumentNullException(nameof(panels));

            _panels = new List<IVehicleRacePanel>(panels);
            if (_panels.Count == 0)
                throw new ArgumentException("At least one vehicle panel is required.", nameof(panels));
        }

        public IVehicleRacePanel ActivePanel => _panels[_activeIndex];
        public int Count => _panels.Count;

        public IVehicleRacePanel MoveNext()
        {
            _activeIndex = (_activeIndex + 1) % _panels.Count;
            return ActivePanel;
        }

        public IVehicleRacePanel MovePrevious()
        {
            _activeIndex--;
            if (_activeIndex < 0)
                _activeIndex = _panels.Count - 1;
            return ActivePanel;
        }

        public void Update(float elapsed)
        {
            ActivePanel.Update(elapsed);
        }

        public void Pause()
        {
            ActivePanel.Pause();
        }

        public void Resume()
        {
            ActivePanel.Resume();
        }

        public void Dispose()
        {
            foreach (var panel in _panels)
                panel.Dispose();
            _panels.Clear();
        }
    }
}

