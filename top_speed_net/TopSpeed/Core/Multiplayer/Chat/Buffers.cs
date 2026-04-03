using System.Collections.Generic;
using TopSpeed.Menu;

using TopSpeed.Localization;
namespace TopSpeed.Core.Multiplayer.Chat
{
    internal enum HistoryBuffer
    {
        All = 0,
        GlobalChat = 1,
        RoomChat = 2,
        Connections = 3,
        RoomEvents = 4
    }

    internal sealed class HistoryBuffers
    {
        private readonly Dictionary<HistoryBuffer, List<MenuItem>> _items = new Dictionary<HistoryBuffer, List<MenuItem>>();
        private static readonly HistoryBuffer[] Order =
        {
            HistoryBuffer.All,
            HistoryBuffer.GlobalChat,
            HistoryBuffer.RoomChat,
            HistoryBuffer.Connections,
            HistoryBuffer.RoomEvents
        };
        private readonly int _maxEntries;
        private readonly List<MenuItem> _emptyItems = new List<MenuItem> { new MenuItem(LocalizationService.Mark("No messages yet."), MenuAction.None) };

        public HistoryBuffers(int maxEntries)
        {
            _maxEntries = maxEntries > 0 ? maxEntries : 100;
            for (var i = 0; i < Order.Length; i++)
                _items[Order[i]] = new List<MenuItem>();
        }

        public HistoryBuffer Current { get; private set; } = HistoryBuffer.All;

        public void AddGlobalChat(string text)
        {
            AddTo(HistoryBuffer.All, text);
            AddTo(HistoryBuffer.GlobalChat, text);
        }

        public void AddRoomChat(string text)
        {
            AddTo(HistoryBuffer.All, text);
            AddTo(HistoryBuffer.RoomChat, text);
        }

        public void AddConnection(string text)
        {
            AddTo(HistoryBuffer.All, text);
            AddTo(HistoryBuffer.Connections, text);
        }

        public void AddRoomEvent(string text)
        {
            AddTo(HistoryBuffer.All, text);
            AddTo(HistoryBuffer.RoomEvents, text);
        }

        public IReadOnlyList<MenuItem> GetCurrentItems()
        {
            var items = _items[Current];
            return items.Count > 0 ? items : _emptyItems;
        }

        public void MoveToNext()
        {
            Current = Order[(FindCurrentIndex() + 1) % Order.Length];
        }

        public void MoveToPrevious()
        {
            Current = Order[(FindCurrentIndex() - 1 + Order.Length) % Order.Length];
        }

        public string CategoryLabel()
        {
            return Current switch
            {
                HistoryBuffer.GlobalChat => LocalizationService.Mark("global chat"),
                HistoryBuffer.RoomChat => LocalizationService.Mark("room chat"),
                HistoryBuffer.Connections => LocalizationService.Mark("connections"),
                HistoryBuffer.RoomEvents => LocalizationService.Mark("room events"),
                _ => LocalizationService.Mark("all")
            };
        }

        public void Clear()
        {
            foreach (var entry in _items)
                entry.Value.Clear();
            Current = HistoryBuffer.All;
        }

        private void AddTo(HistoryBuffer buffer, string text)
        {
            var line = Normalize(text);
            if (line.Length == 0)
                return;

            var items = _items[buffer];
            items.Add(new MenuItem(line, MenuAction.None));
            while (items.Count > _maxEntries)
                items.RemoveAt(0);
        }

        private static string Normalize(string text)
        {
            return (text ?? string.Empty).Trim();
        }

        private int FindCurrentIndex()
        {
            for (var i = 0; i < Order.Length; i++)
            {
                if (Order[i] == Current)
                    return i;
            }

            return 0;
        }
    }
}




