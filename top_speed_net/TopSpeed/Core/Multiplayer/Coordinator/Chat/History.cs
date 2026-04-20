using TopSpeed.Speech;
using TopSpeed.Menu;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void AddGlobalChatMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _state.Chat.History.AddGlobalChat(text);
            UpdateHistoryScreens();
        }

        private void AddRoomChatMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _state.Chat.History.AddRoomChat(text);
            UpdateHistoryScreens();
        }

        private void AddConnectionMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _state.Chat.History.AddConnection(text);
            UpdateHistoryScreens();
        }

        private void AddRoomEventMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _state.Chat.History.AddRoomEvent(text);
            UpdateHistoryScreens();
        }

        internal void NextChatCategory()
        {
            _chatFlow.NextCategory();
        }

        internal void NextChatCategoryCore()
        {
            _state.Chat.History.MoveToNext();
            PlayNetworkSound("buffer_switch.ogg");
            UpdateHistoryScreens();
            _speech.Speak(_state.Chat.History.CategoryLabel(), SpeechService.SpeakFlag.None);
        }

        internal void PreviousChatCategory()
        {
            _chatFlow.PreviousCategory();
        }

        internal void PreviousChatCategoryCore()
        {
            _state.Chat.History.MoveToPrevious();
            PlayNetworkSound("buffer_switch.ogg");
            UpdateHistoryScreens();
            _speech.Speak(_state.Chat.History.CategoryLabel(), SpeechService.SpeakFlag.None);
        }

        internal void NextChatItem()
        {
            _chatFlow.NextItem();
        }

        internal void NextChatItemCore()
        {
            var result = _state.Chat.History.MoveCurrentItem(1, _menu.IsWrapNavigationEnabled);
            if (InteractionHints.IsTouchPlatform())
                PlayChatItemNavigationFeedback(result);
            _speech.Speak(result.Text, SpeechService.SpeakFlag.None);
        }

        internal void PreviousChatItem()
        {
            _chatFlow.PreviousItem();
        }

        internal void PreviousChatItemCore()
        {
            var result = _state.Chat.History.MoveCurrentItem(-1, _menu.IsWrapNavigationEnabled);
            if (InteractionHints.IsTouchPlatform())
                PlayChatItemNavigationFeedback(result);
            _speech.Speak(result.Text, SpeechService.SpeakFlag.None);
        }

        private void PlayChatItemNavigationFeedback(Chat.HistoryMoveResult result)
        {
            var menuId = _menu.CurrentId ?? string.Empty;
            if (result.Moved)
                _menu.TryPlayNavigateCue(menuId);

            if (result.Wrapped)
                _menu.TryPlayWrapCue(menuId);
            else if (result.EdgeReached)
                _menu.TryPlayEdgeCue(menuId);
        }
    }
}



