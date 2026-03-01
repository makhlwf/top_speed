using System;
using System.Collections.Generic;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Speech;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void RebuildCreateRoomMenu()
        {
            RebuildCreateRoomMenu(preserveSelection: false);
        }

        private void RebuildCreateRoomMenu(bool preserveSelection)
        {
            var maxPlayersItem = new RadioButton(
                "Maximum players allowed in this room",
                RoomCapacityOptions,
                GetCreateRoomPlayersToStartIndex,
                SetCreateRoomPlayersToStart,
                hint: "Choose the player capacity from 2 to 10. Use LEFT or RIGHT to change.")
            {
                Hidden = _createRoomType == GameRoomType.OneOnOne
            };

            var items = new List<MenuItem>
            {
                new RadioButton(
                    "Game type",
                    RoomTypeOptions,
                    GetCreateRoomTypeIndex,
                    SetCreateRoomType,
                    hint: "Choose whether this room is a race with bots, a multiplayer race without bots, or a one-on-one game. Use LEFT or RIGHT to change."),
                maxPlayersItem,
                new MenuItem(
                    () => string.IsNullOrWhiteSpace(_createRoomName)
                        ? "Room name, currently automatic"
                        : $"Room name, currently {_createRoomName}",
                    MenuAction.None,
                    onActivate: UpdateCreateRoomName,
                    hint: "Press ENTER to enter a room name. Leave it empty to use an automatic name."),
                new MenuItem("Create this game room", MenuAction.None, onActivate: ConfirmCreateRoom),
                new MenuItem("Cancel room creation", MenuAction.Back)
            };

            _menu.UpdateItems(MultiplayerCreateRoomMenuId, items, preserveSelection);
        }

        private void OpenCreateRoomMenu()
        {
            if (SessionOrNull() == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            ResetCreateRoomDraft();
            RebuildCreateRoomMenu();
            _menu.Push(MultiplayerCreateRoomMenuId);
        }

        private void UpdateCreateRoomName()
        {
            _promptTextInput(
                "Enter a room name. Leave this field empty to use an automatic room name.",
                _createRoomName,
                SpeechService.SpeakFlag.None,
                true,
                result =>
                {
                    if (result.Cancelled)
                        return;

                    _createRoomName = (result.Text ?? string.Empty).Trim();
                    RebuildCreateRoomMenu();

                    if (string.IsNullOrWhiteSpace(_createRoomName))
                    {
                        _speech.Speak("Automatic room name selected.");
                        return;
                    }

                    _speech.Speak($"Room name set to {_createRoomName}.");
                });
        }

        private void ConfirmCreateRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            var playersToStart = _createRoomPlayersToStart;
            if (playersToStart < 2 || playersToStart > ProtocolConstants.MaxRoomPlayersToStart)
                playersToStart = 2;
            if (_createRoomType == GameRoomType.OneOnOne)
                playersToStart = 2;

            if (!TrySend(session.SendRoomCreate(_createRoomName, _createRoomType, playersToStart), "room create request"))
                return;
            _menu.ShowRoot(MultiplayerLobbyMenuId);
        }

        private int GetCreateRoomTypeIndex()
        {
            return _createRoomType switch
            {
                GameRoomType.PlayersRace => 1,
                GameRoomType.OneOnOne => 2,
                _ => 0
            };
        }

        private void SetCreateRoomType(int index)
        {
            _createRoomType = index switch
            {
                2 => GameRoomType.OneOnOne,
                1 => GameRoomType.PlayersRace,
                _ => GameRoomType.BotsRace
            };

            if (_createRoomType == GameRoomType.OneOnOne)
                _createRoomPlayersToStart = 2;

            if (string.Equals(_menu.CurrentId, MultiplayerCreateRoomMenuId, StringComparison.Ordinal))
                RebuildCreateRoomMenu(preserveSelection: true);
        }

        private int GetCreateRoomPlayersToStartIndex()
        {
            var playersToStart = _createRoomPlayersToStart;
            if (_createRoomType == GameRoomType.OneOnOne)
                playersToStart = 2;
            if (playersToStart < 2 || playersToStart > ProtocolConstants.MaxRoomPlayersToStart)
                playersToStart = 2;
            return playersToStart - 2;
        }

        private void SetCreateRoomPlayersToStart(int index)
        {
            if (_createRoomType == GameRoomType.OneOnOne)
            {
                _createRoomPlayersToStart = 2;
                return;
            }

            var playersToStart = (byte)(index + 2);
            if (playersToStart < 2 || playersToStart > ProtocolConstants.MaxRoomPlayersToStart)
                return;
            _createRoomPlayersToStart = playersToStart;
        }

        private void ResetCreateRoomDraft()
        {
            _createRoomType = GameRoomType.BotsRace;
            _createRoomPlayersToStart = 2;
            _createRoomName = string.Empty;
        }
    }
}
