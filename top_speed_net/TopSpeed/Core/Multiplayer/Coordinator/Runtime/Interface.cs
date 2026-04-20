namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        void IMultiplayerRuntime.NextChatCategory()
        {
            NextChatCategory();
        }

        void IMultiplayerRuntime.PreviousChatCategory()
        {
            PreviousChatCategory();
        }

        void IMultiplayerRuntime.NextChatItem()
        {
            NextChatItem();
        }

        void IMultiplayerRuntime.PreviousChatItem()
        {
            PreviousChatItem();
        }

        void IMultiplayerRuntime.CheckPing()
        {
            CheckCurrentPing();
        }

        void IMultiplayerRuntime.OpenGlobalChatHotkey()
        {
            OpenGlobalChatHotkey();
        }

        void IMultiplayerRuntime.OpenRoomChatHotkey()
        {
            OpenRoomChatHotkey();
        }

        string IMultiplayerRuntime.ResolvePlayerName(byte playerNumber)
        {
            return ResolvePlayerName(playerNumber);
        }
    }
}

