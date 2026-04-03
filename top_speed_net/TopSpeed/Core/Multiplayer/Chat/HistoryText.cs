using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer.Chat
{
    internal static class HistoryText
    {
        public static string JoinedRoom(string roomName)
        {
            return LocalizationService.Format(
                LocalizationService.Mark("You joined {0}."),
                NormalizeRoomName(roomName));
        }

        public static string LeftRoom()
        {
            return LocalizationService.Mark("You left the game room.");
        }

        public static string BecameHost()
        {
            return LocalizationService.Mark("You are now host of this game.");
        }

        public static string ParticipantJoined(RoomEventInfo roomEvent)
        {
            return LocalizationService.Format(
                LocalizationService.Mark("{0} joined the current room."),
                ResolvePlayerName(roomEvent));
        }

        public static string ParticipantLeft(RoomEventInfo roomEvent)
        {
            return LocalizationService.Format(
                LocalizationService.Mark("{0} left the current room."),
                ResolvePlayerName(roomEvent));
        }

        public static string FromRoomEvent(RoomEventInfo roomEvent)
        {
            switch (roomEvent.Kind)
            {
                default:
                    return string.Empty;
            }
        }

        private static string ResolvePlayerName(RoomEventInfo roomEvent)
        {
            if (!string.IsNullOrWhiteSpace(roomEvent.SubjectPlayerName))
                return roomEvent.SubjectPlayerName.Trim();
            return LocalizationService.Format(
                LocalizationService.Mark("Player {0}"),
                roomEvent.SubjectPlayerNumber + 1);
        }

        private static string NormalizeRoomName(string roomName)
        {
            if (!string.IsNullOrWhiteSpace(roomName))
                return roomName.Trim();
            return LocalizationService.Translate(LocalizationService.Mark("game room"));
        }
    }
}

