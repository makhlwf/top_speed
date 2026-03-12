using TopSpeed.Speech;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void AddGlobalChatMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _historyBuffers.AddGlobalChat(text);
            UpdateHistoryScreens();
        }

        private void AddRoomChatMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _historyBuffers.AddRoomChat(text);
            UpdateHistoryScreens();
        }

        private void AddConnectionMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _historyBuffers.AddConnection(text);
            UpdateHistoryScreens();
        }

        private void AddRoomEventMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _historyBuffers.AddRoomEvent(text);
            UpdateHistoryScreens();
        }

        internal void NextChatCategory()
        {
            _historyBuffers.MoveToNext();
            PlayNetworkSound("buffer_switch.ogg");
            UpdateHistoryScreens();
            _speech.Speak(_historyBuffers.CategoryLabel(), SpeechService.SpeakFlag.None);
        }

        internal void PreviousChatCategory()
        {
            _historyBuffers.MoveToPrevious();
            PlayNetworkSound("buffer_switch.ogg");
            UpdateHistoryScreens();
            _speech.Speak(_historyBuffers.CategoryLabel(), SpeechService.SpeakFlag.None);
        }
    }
}
