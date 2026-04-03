using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Race.Multiplayer;
using TopSpeed.Vehicles;
using System;

namespace TopSpeed.Race
{
    internal sealed partial class MultiplayerMode
    {
        public void ApplyRemoteData(PacketPlayerData data)
        {
            ApplyRemoteDataCore(
                data.PlayerNumber,
                data.Car,
                data.State,
                data.RaceData.PositionX,
                data.RaceData.PositionY,
                data.RaceData.Speed,
                data.RaceData.Frequency,
                data.EngineRunning,
                data.Braking,
                data.Horning,
                data.Backfiring,
                data.MediaLoaded,
                data.MediaPlaying,
                data.MediaId);
        }

        public void ApplyBump(PacketPlayerBumped bump)
        {
            if (bump.PlayerNumber != LocalPlayerNumber)
                return;
            _car.Bump(bump.BumpX, bump.BumpY, bump.SpeedDeltaKph);
        }

        public void ApplyRemoteCrash(PacketPlayer crashed)
        {
            if (crashed.PlayerNumber == LocalPlayerNumber)
                return;
            if (crashed.PlayerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[crashed.PlayerNumber])
                return;
            if (_remotePlayers.TryGetValue(crashed.PlayerNumber, out var remote))
                remote.Player.Crash(remote.Player.PositionX, scheduleRestart: false);
        }

        public void ApplyRemoteFinish(byte playerNumber, byte finishOrder)
        {
            if (playerNumber == LocalPlayerNumber)
                return;
            if (playerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[playerNumber])
                return;
            if (!_remotePlayers.TryGetValue(playerNumber, out var remote))
                return;
            if (remote.Finished)
                return;

            remote.Finished = true;
            remote.State = PlayerState.Finished;

            if (finishOrder > 0)
            {
                var expectedIndex = Math.Max(0, finishOrder - 1);
                if (expectedIndex > _positionFinish)
                    _positionFinish = expectedIndex;
            }

            AnnounceFinishOrder(_soundPlayerNr, _soundFinished, playerNumber, ref _positionFinish);
        }

        public void RemoveRemotePlayer(byte playerNumber, bool markDisconnected = true)
        {
            if (markDisconnected && playerNumber < _disconnectedPlayerSlots.Length)
                _disconnectedPlayerSlots[playerNumber] = true;

            _remoteMediaTransfers.Remove(playerNumber);
            _remoteLiveStates.Remove(playerNumber);
            if (_remotePlayers.TryGetValue(playerNumber, out var remote))
            {
                remote.Player.StopLiveStream();
                remote.Player.FinalizePlayer();
                remote.Player.Dispose();
                _remotePlayers.Remove(playerNumber);
            }

            RemovePlayerFromSnapshotFrames(playerNumber);
        }

        public void HandleServerRaceCompleted(PacketRoomRaceCompleted packet)
        {
            FinalizeServerRace(BuildResultSummary(packet));
        }

        public void HandleServerRaceAborted()
        {
            FinalizeServerRace(null);
        }

        private void FinalizeServerRace(RaceResultSummary? summary)
        {
            if (_serverStopReceived)
                return;

            _serverStopReceived = true;
            _snapshotFrames.Clear();
            _hasSnapshotTickNow = false;
            _missingSnapshotPlayers.Clear();
            foreach (var number in _remotePlayers.Keys)
                _missingSnapshotPlayers.Add(number);
            for (var i = 0; i < _missingSnapshotPlayers.Count; i++)
                RemoveRemotePlayer(_missingSnapshotPlayers[i]);
            _remoteLiveStates.Clear();
            if (!_sentFinish)
            {
                _sentFinish = true;
                _currentState = PlayerState.Finished;
                TrySendRace(_session.SendPlayerState(_raceInstanceId, _currentState));
            }

            if (summary != null)
                SetResultSummary(summary);
            RequestExitWhenQueueIdle();
        }

        public void SyncParticipants(PacketRoomState roomState)
        {
            if (roomState == null)
                return;

            _missingSnapshotPlayers.Clear();
            foreach (var number in _remotePlayers.Keys)
                _missingSnapshotPlayers.Add(number);

            var participants = roomState.Players ?? Array.Empty<PacketRoomPlayer>();
            for (var i = 0; i < participants.Length; i++)
            {
                var number = participants[i].PlayerNumber;
                if (number == LocalPlayerNumber)
                    continue;
                _missingSnapshotPlayers.Remove(number);
            }

            if (_missingSnapshotPlayers.Count == 0)
                return;

            for (var i = 0; i < _missingSnapshotPlayers.Count; i++)
                RemoveRemotePlayer(_missingSnapshotPlayers[i]);
        }

        private RemotePlayer GetOrCreateRemotePlayer(byte playerNumber, CarType car, float positionX, float positionY)
        {
            if (_remotePlayers.TryGetValue(playerNumber, out var existing))
                return existing;

            var vehicleIndex = car == CarType.CustomVehicle ? 0 : (int)car;
            var bot = new ComputerPlayer(_audio, _track, _settings, vehicleIndex, playerNumber, () => _elapsedTotal, () => _started);
            bot.Initialize(positionX, positionY, GetSpatialTrackLength());
            var remote = new RemotePlayer(bot);
            _remotePlayers[playerNumber] = remote;
            return remote;
        }

        private void TryApplyPendingRemoteMedia(byte playerNumber, RemotePlayer remote)
        {
            if (_remoteLiveStates.TryGetValue(playerNumber, out var live) && live.StreamId != 0)
                return;
            if (!_remoteMediaTransfers.TryGetValue(playerNumber, out var transfer))
                return;
            if (!transfer.IsComplete)
                return;

            remote.Player.ApplyRadioMedia(transfer.MediaId, transfer.Extension, transfer.Data);
            _remoteMediaTransfers.Remove(playerNumber);
        }

        private void ApplyRemoteDataCore(
            byte playerNumber,
            CarType car,
            PlayerState state,
            float positionX,
            float positionY,
            ushort speed,
            int frequency,
            bool engineRunning,
            bool braking,
            bool horning,
            bool backfiring,
            bool mediaLoaded,
            bool mediaPlaying,
            uint mediaId)
        {
            if (playerNumber == LocalPlayerNumber)
                return;
            if (playerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[playerNumber])
                return;

            var remote = GetOrCreateRemotePlayer(playerNumber, car, positionX, positionY);
            remote.State = state;
            if (state == PlayerState.Finished && !remote.Finished)
            {
                remote.Finished = true;
                AnnounceFinishOrder(_soundPlayerNr, _soundFinished, playerNumber, ref _positionFinish);
            }

            remote.Player.ApplyNetworkState(
                positionX,
                positionY,
                speed,
                frequency,
                engineRunning,
                braking,
                horning,
                backfiring,
                mediaLoaded,
                mediaPlaying,
                mediaId,
                _car.PositionX,
                _car.PositionY,
                GetSpatialTrackLength());
            TryApplyPendingRemoteMedia(playerNumber, remote);
        }
    }
}

