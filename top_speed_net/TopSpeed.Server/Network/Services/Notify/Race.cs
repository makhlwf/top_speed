using System;
using System.Linq;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Notify
        {
            public void RaceStateChanged(RaceRoom room)
            {
                var payload = PacketSerializer.WriteRoomRaceStateChanged(new PacketRoomRaceStateChanged
                {
                    RoomId = room.Id,
                    RoomVersion = room.Version,
                    RaceInstanceId = room.RaceInstanceId,
                    State = room.RaceState
                });
                _owner.SendToRoomOnStream(room, payload, PacketStream.Room);
            }

            public void RacePlayerFinished(RaceRoom room, uint playerId, byte playerNumber, byte finishOrder, int timeMs)
            {
                var payload = PacketSerializer.WriteRoomRacePlayerFinished(new PacketRoomRacePlayerFinished
                {
                    RoomId = room.Id,
                    RaceInstanceId = room.RaceInstanceId,
                    PlayerId = playerId,
                    PlayerNumber = playerNumber,
                    FinishOrder = finishOrder,
                    TimeMs = Math.Max(0, timeMs)
                });
                _owner.SendToRoomOnStream(room, payload, PacketStream.Room);
            }

            public void RaceCompleted(RaceRoom room)
            {
                var packet = BuildRoomRaceCompleted(room);
                _owner._logger.Debug(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Room race completed emit: room={0}, raceInstance={1}, results={2}.",
                    room.Id,
                    room.RaceInstanceId,
                    packet.Results.Length));
                var payload = PacketSerializer.WriteRoomRaceCompleted(packet);
                _owner.SendToRoomOnStream(room, payload, PacketStream.Room);
            }

            public void RaceAborted(RaceRoom room, RoomRaceAbortReason reason)
            {
                var payload = PacketSerializer.WriteRoomRaceAborted(new PacketRoomRaceAborted
                {
                    RoomId = room.Id,
                    RoomVersion = room.Version,
                    RaceInstanceId = room.RaceInstanceId,
                    Reason = reason
                });
                _owner.SendToRoomOnStream(room, payload, PacketStream.Room);
            }

            private PacketRoomRaceCompleted BuildRoomRaceCompleted(RaceRoom room)
            {
                var ordered = room.RaceParticipantResults.Values
                    .OrderBy(result => result.Status == RoomRaceResultStatus.Finished ? 0 : 1)
                    .ThenBy(result => result.Status == RoomRaceResultStatus.Finished ? result.FinishOrder : byte.MaxValue)
                    .ThenBy(result => result.PlayerNumber)
                    .Take(ProtocolConstants.MaxPlayers)
                    .ToArray();

                var results = new PacketRoomRaceResultEntry[ordered.Length];
                for (var i = 0; i < ordered.Length; i++)
                {
                    var item = ordered[i];
                    var status = item.Status;
                    if (status != RoomRaceResultStatus.Finished && status != RoomRaceResultStatus.Dnf)
                        status = RoomRaceResultStatus.Dnf;

                    results[i] = new PacketRoomRaceResultEntry
                    {
                        PlayerId = item.PlayerId,
                        PlayerNumber = item.PlayerNumber,
                        FinishOrder = status == RoomRaceResultStatus.Finished ? item.FinishOrder : (byte)0,
                        TimeMs = status == RoomRaceResultStatus.Finished ? Math.Max(0, item.TimeMs) : 0,
                        Status = status
                    };
                }

                return new PacketRoomRaceCompleted
                {
                    RoomId = room.Id,
                    RoomVersion = room.Version,
                    RaceInstanceId = room.RaceInstanceId,
                    Results = results
                };
            }
        }
    }
}
