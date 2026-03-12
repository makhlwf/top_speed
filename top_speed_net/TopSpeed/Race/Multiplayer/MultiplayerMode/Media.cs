using TopSpeed.Protocol;

namespace TopSpeed.Race
{
    internal sealed partial class MultiplayerMode
    {
        public void ApplyRemoteMediaBegin(PacketPlayerMediaBegin media)
        {
            if (media.PlayerNumber == _playerNumber)
                return;
            if (media.PlayerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[media.PlayerNumber])
                return;
            if (media.TotalBytes == 0 || media.TotalBytes > ProtocolConstants.MaxMediaBytes)
                return;

            _remoteMediaTransfers[media.PlayerNumber] = new Multiplayer.MediaTransfer
            {
                MediaId = media.MediaId,
                Extension = media.FileExtension,
                Data = new byte[media.TotalBytes],
                Offset = 0,
                NextChunkIndex = 0
            };
        }

        public void ApplyRemoteMediaChunk(PacketPlayerMediaChunk media)
        {
            if (media.PlayerNumber == _playerNumber)
                return;
            if (media.PlayerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[media.PlayerNumber])
                return;
            if (!_remoteMediaTransfers.TryGetValue(media.PlayerNumber, out var transfer))
                return;
            if (transfer.MediaId != media.MediaId)
                return;
            if (transfer.NextChunkIndex != media.ChunkIndex)
                return;
            if (media.Data == null || media.Data.Length == 0)
                return;

            var remaining = transfer.Data.Length - transfer.Offset;
            if (media.Data.Length > remaining)
            {
                _remoteMediaTransfers.Remove(media.PlayerNumber);
                return;
            }

            System.Buffer.BlockCopy(media.Data, 0, transfer.Data, transfer.Offset, media.Data.Length);
            transfer.Offset += media.Data.Length;
            transfer.NextChunkIndex++;
        }

        public void ApplyRemoteMediaEnd(PacketPlayerMediaEnd media)
        {
            if (media.PlayerNumber == _playerNumber)
                return;
            if (media.PlayerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[media.PlayerNumber])
                return;
            if (!_remoteMediaTransfers.TryGetValue(media.PlayerNumber, out var transfer))
                return;
            if (transfer.MediaId != media.MediaId)
                return;
            if (!transfer.IsComplete)
                return;
            if (_remoteLiveStates.TryGetValue(media.PlayerNumber, out var live) && live.StreamId != 0)
                return;
            if (!_remotePlayers.TryGetValue(media.PlayerNumber, out var remote))
                return;

            remote.Player.ApplyRadioMedia(transfer.MediaId, transfer.Extension, transfer.Data);
            _remoteMediaTransfers.Remove(media.PlayerNumber);
        }
    }
}

