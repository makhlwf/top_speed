using System.Net;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void RegisterRoomPackets()
        {
            _pktReg.Add("room", Command.RoomListRequest, (player, _, _) => SendRoomList(player));
            _pktReg.Add("room", Command.RoomStateRequest, (player, _, _) => HandleRoomStateRequest(player));
            _pktReg.Add("room", Command.RoomGetRequest, (player, payload, endPoint) =>
            {
                if (PacketSerializer.TryReadRoomGetRequest(payload, out var get))
                    HandleRoomGetRequest(player, get);
                else
                    PacketFail(endPoint, Command.RoomGetRequest);
            });
            _pktReg.Add("room", Command.RoomCreate, (player, payload, endPoint) =>
            {
                if (PacketSerializer.TryReadRoomCreate(payload, out var create))
                    HandleCreateRoom(player, create);
                else
                    PacketFail(endPoint, Command.RoomCreate);
            });
            _pktReg.Add("room", Command.RoomJoin, (player, payload, endPoint) =>
            {
                if (PacketSerializer.TryReadRoomJoin(payload, out var join))
                    HandleJoinRoom(player, join);
                else
                    PacketFail(endPoint, Command.RoomJoin);
            });
            _pktReg.Add("room", Command.RoomLeave, (player, _, _) => HandleLeaveRoom(player, true));
            _pktReg.Add("room", Command.RoomSetTrack, (player, payload, endPoint) =>
            {
                if (PacketSerializer.TryReadRoomSetTrack(payload, out var track))
                    HandleSetTrack(player, track);
                else
                    PacketFail(endPoint, Command.RoomSetTrack);
            });
            _pktReg.Add("room", Command.RoomSetLaps, (player, payload, endPoint) =>
            {
                if (PacketSerializer.TryReadRoomSetLaps(payload, out var laps))
                    HandleSetLaps(player, laps);
                else
                    PacketFail(endPoint, Command.RoomSetLaps);
            });
            _pktReg.Add("room", Command.RoomStartRace, (player, _, _) => HandleStartRace(player));
            _pktReg.Add("room", Command.RoomSetPlayersToStart, (player, payload, endPoint) =>
            {
                if (PacketSerializer.TryReadRoomSetPlayersToStart(payload, out var setPlayers))
                    HandleSetPlayersToStart(player, setPlayers);
                else
                    PacketFail(endPoint, Command.RoomSetPlayersToStart);
            });
            _pktReg.Add("room", Command.RoomAddBot, (player, _, _) => HandleAddBot(player));
            _pktReg.Add("room", Command.RoomRemoveBot, (player, _, _) => HandleRemoveBot(player));
            _pktReg.Add("room", Command.RoomPlayerReady, (player, payload, endPoint) =>
            {
                if (PacketSerializer.TryReadRoomPlayerReady(payload, out var ready))
                    HandlePlayerReady(player, ready);
                else
                    PacketFail(endPoint, Command.RoomPlayerReady);
            });
        }
    }
}
