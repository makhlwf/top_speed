using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            private readonly RaceServer _owner;

            public Room(RaceServer owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public void RegisterPackets(ServerPktReg registry)
            {
                registry.Add("room", Command.RoomListRequest, (player, _, _) => _owner._notify.SendRoomList(player));
                registry.Add("room", Command.RoomStateRequest, (player, _, _) => HandleStateRequest(player));
                registry.Add("room", Command.OnlinePlayersRequest, (player, _, _) => _owner.HandleOnlinePlayersRequest(player));
                registry.Add("room", Command.RoomGetRequest, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomGetRequest(payload, out var get))
                        HandleGetRequest(player, get);
                    else
                        _owner.PacketFail(endPoint, Command.RoomGetRequest);
                });
                registry.Add("room", Command.RoomCreate, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomCreate(payload, out var create))
                        Create(player, create);
                    else
                        _owner.PacketFail(endPoint, Command.RoomCreate);
                });
                registry.Add("room", Command.RoomJoin, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomJoin(payload, out var join))
                        Join(player, join);
                    else
                        _owner.PacketFail(endPoint, Command.RoomJoin);
                });
                registry.Add("room", Command.RoomLeave, (player, _, _) => Leave(player, true));
                registry.Add("room", Command.RoomSetTrack, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomSetTrack(payload, out var track))
                        SetTrack(player, track);
                    else
                        _owner.PacketFail(endPoint, Command.RoomSetTrack);
                });
                registry.Add("room", Command.RoomSetLaps, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomSetLaps(payload, out var laps))
                        SetLaps(player, laps);
                    else
                        _owner.PacketFail(endPoint, Command.RoomSetLaps);
                });
                registry.Add("room", Command.RoomStartRace, (player, _, _) => StartRace(player));
                registry.Add("room", Command.RoomSetPlayersToStart, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomSetPlayersToStart(payload, out var setPlayers))
                        SetPlayersToStart(player, setPlayers);
                    else
                        _owner.PacketFail(endPoint, Command.RoomSetPlayersToStart);
                });
                registry.Add("room", Command.RoomSetGameRules, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomSetGameRules(payload, out var gameRules))
                        SetGameRules(player, gameRules);
                    else
                        _owner.PacketFail(endPoint, Command.RoomSetGameRules);
                });
                registry.Add("room", Command.RoomAddBot, (player, _, _) => AddBot(player));
                registry.Add("room", Command.RoomRemoveBot, (player, _, _) => RemoveBot(player));
                registry.Add("room", Command.RoomPlayerReady, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRoomPlayerReady(payload, out var ready))
                        PlayerReady(player, ready);
                    else
                        _owner.PacketFail(endPoint, Command.RoomPlayerReady);
                });
                registry.Add("room", Command.RoomPlayerWithdraw, (player, _, _) => PlayerWithdraw(player));
            }
        }
    }
}
