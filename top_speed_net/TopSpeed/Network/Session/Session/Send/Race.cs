using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed partial class MultiplayerSession
    {
        public bool SendPlayerState(PlayerState state)
        {
            var payload = ClientPacketSerializer.WritePlayerState(Command.PlayerState, PlayerId, PlayerNumber, state);
            return _sender.TrySend(payload, PacketStream.Control);
        }

        public bool SendPlayerData(
            PlayerRaceData raceData,
            CarType car,
            PlayerState state,
            bool engine,
            bool braking,
            bool horning,
            bool backfiring,
            bool radioLoaded,
            bool radioPlaying,
            uint radioMediaId)
        {
            var payload = ClientPacketSerializer.WritePlayerDataToServer(
                PlayerId,
                PlayerNumber,
                car,
                raceData,
                state,
                engine,
                braking,
                horning,
                backfiring,
                radioLoaded,
                radioPlaying,
                radioMediaId);
            return _sender.TrySend(payload, PacketStream.RaceState, PacketDeliveryKind.Sequenced);
        }

        public bool SendPlayerStarted()
        {
            return _sender.TrySend(
                ClientPacketSerializer.WritePlayer(Command.PlayerStarted, PlayerId, PlayerNumber),
                PacketStream.RaceEvent);
        }

        public bool SendPlayerFinished()
        {
            return _sender.TrySend(
                ClientPacketSerializer.WritePlayer(Command.PlayerFinished, PlayerId, PlayerNumber),
                PacketStream.RaceEvent);
        }

        public bool SendPlayerFinalize(PlayerState state)
        {
            return _sender.TrySend(
                ClientPacketSerializer.WritePlayerState(Command.PlayerFinalize, PlayerId, PlayerNumber, state),
                PacketStream.Control);
        }

        public bool SendPlayerCrashed()
        {
            return _sender.TrySend(
                ClientPacketSerializer.WritePlayer(Command.PlayerCrashed, PlayerId, PlayerNumber),
                PacketStream.RaceEvent);
        }
    }
}
