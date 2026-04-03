using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed partial class MultiplayerSession
    {
        public bool SendPing()
        {
            return _sender.TrySend(ClientPacketSerializer.WriteGeneral(Command.Ping), PacketStream.Control);
        }

        public bool SendRoomListRequest()
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomListRequest(), PacketStream.Query);
        }

        public bool SendRoomStateRequest()
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomStateRequest(), PacketStream.Query);
        }

        public bool SendOnlinePlayersRequest()
        {
            return _sender.TrySend(ClientPacketSerializer.WriteOnlinePlayersRequest(), PacketStream.Query);
        }

        public bool SendRoomGetRequest(uint roomId)
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomGetRequest(roomId), PacketStream.Query);
        }

        public bool SendRoomCreate(string roomName, GameRoomType roomType, byte playersToStart)
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomCreate(roomName, roomType, playersToStart), PacketStream.Room);
        }

        public bool SendRoomJoin(uint roomId)
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomJoin(roomId), PacketStream.Room);
        }

        public bool SendRoomLeave()
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomLeave(), PacketStream.Room);
        }

        public bool SendRoomSetTrack(string trackName)
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomSetTrack(trackName), PacketStream.Room);
        }

        public bool SendRoomSetLaps(byte laps)
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomSetLaps(laps), PacketStream.Room);
        }

        public bool SendRoomStartRace()
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomStartRace(), PacketStream.Room);
        }

        public bool SendRoomSetPlayersToStart(byte playersToStart)
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomSetPlayersToStart(playersToStart), PacketStream.Room);
        }

        public bool SendRoomSetGameRules(uint gameRulesFlags)
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomSetGameRules(gameRulesFlags), PacketStream.Room);
        }

        public bool SendRoomAddBot()
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomAddBot(), PacketStream.Room);
        }

        public bool SendRoomRemoveBot()
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomRemoveBot(), PacketStream.Room);
        }

        public bool SendRoomPlayerReady(CarType car, bool automaticTransmission)
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomPlayerReady(car, automaticTransmission), PacketStream.Room);
        }

        public bool SendRoomPlayerWithdraw()
        {
            return _sender.TrySend(ClientPacketSerializer.WriteRoomPlayerWithdraw(), PacketStream.Room);
        }
    }
}

