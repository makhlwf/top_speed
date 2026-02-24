using System;
using TopSpeed.Data;
using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Core
{
    internal sealed partial class Game
    {
        private void RegisterMultiplayerRoomPacketHandlers()
        {
            _mpPktReg.Add("room", Command.PlayerJoined, HandleMpPlayerJoinedPacket);
            _mpPktReg.Add("room", Command.LoadCustomTrack, HandleMpLoadCustomTrackPacket);
            _mpPktReg.Add("room", Command.RoomList, HandleMpRoomListPacket);
            _mpPktReg.Add("room", Command.RoomState, HandleMpRoomStatePacket);
            _mpPktReg.Add("room", Command.RoomEvent, HandleMpRoomEventPacket);
        }

        private bool HandleMpPlayerJoinedPacket(IncomingPacket packet)
        {
            var session = _session;
            if (session == null)
                return false;

            if (ClientPacketSerializer.TryReadPlayerJoined(packet.Payload, out var joined))
            {
                if (joined.PlayerNumber != session.PlayerNumber)
                {
                    var name = string.IsNullOrWhiteSpace(joined.Name)
                        ? $"Player {joined.PlayerNumber + 1}"
                        : joined.Name;
                    _speech.Speak($"{name} has joined the game.");
                }
            }

            return true;
        }

        private bool HandleMpLoadCustomTrackPacket(IncomingPacket packet)
        {
            if (ClientPacketSerializer.TryReadLoadCustomTrack(packet.Payload, out var track))
            {
                var name = string.IsNullOrWhiteSpace(track.TrackName) ? "custom" : track.TrackName;
                var userDefined = string.Equals(name, "custom", StringComparison.OrdinalIgnoreCase);
                _pendingMultiplayerTrack = new TrackData(userDefined, track.TrackWeather, track.TrackAmbience, track.Definitions);
                _pendingMultiplayerTrackName = name;
                _pendingMultiplayerLaps = track.NrOfLaps;
                if (_pendingMultiplayerStart)
                    StartMultiplayerRace();
            }

            return true;
        }

        private bool HandleMpRoomListPacket(IncomingPacket packet)
        {
            if (ClientPacketSerializer.TryReadRoomList(packet.Payload, out var roomList))
                _multiplayerCoordinator.HandleRoomList(roomList);
            return true;
        }

        private bool HandleMpRoomStatePacket(IncomingPacket packet)
        {
            if (ClientPacketSerializer.TryReadRoomState(packet.Payload, out var roomState))
                _multiplayerCoordinator.HandleRoomState(roomState);
            return true;
        }

        private bool HandleMpRoomEventPacket(IncomingPacket packet)
        {
            if (ClientPacketSerializer.TryReadRoomEvent(packet.Payload, out var roomEvent))
                _multiplayerCoordinator.HandleRoomEvent(roomEvent);
            return true;
        }
    }
}
