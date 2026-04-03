using System;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed class MultiplayerRaceBinding
    {
        public TrackData? PendingTrack;
        public string PendingTrackName = string.Empty;
        public int PendingLaps;
        public bool PendingStart;
        public uint ActiveRoomId;
        public uint RoomId;
        public uint RaceInstanceId;
        public int VehicleIndex;
        public bool AutomaticTransmission = true;

        public void ResetSession()
        {
            ActiveRoomId = 0;
            RoomId = 0;
            RaceInstanceId = 0;
        }

        public void ResetPending()
        {
            PendingTrack = null;
            PendingTrackName = string.Empty;
            PendingLaps = 0;
            PendingStart = false;
            VehicleIndex = 0;
            AutomaticTransmission = true;
        }

        public void SetLoadout(int vehicleCount, int vehicleIndex, bool automaticTransmission)
        {
            VehicleIndex = Math.Max(0, Math.Min(vehicleCount - 1, vehicleIndex));
            AutomaticTransmission = automaticTransmission;
        }

        public void SetTrack(TrackData track, string trackName, int laps)
        {
            PendingTrack = track;
            PendingTrackName = string.IsNullOrWhiteSpace(trackName) ? "custom" : trackName;
            PendingLaps = laps;
        }

        public bool WaitForTrack()
        {
            if (PendingTrack != null)
                return false;

            PendingStart = true;
            return true;
        }

        public void ClearPendingStart()
        {
            PendingStart = false;
        }

        public void ApplyRoomState(PacketRoomState roomState)
        {
            ActiveRoomId = roomState.InRoom ? roomState.RoomId : 0;

            if (roomState.InRoom
                && roomState.RoomId != 0
                && roomState.RaceInstanceId != 0
                && (roomState.RaceState == RoomRaceState.Preparing || roomState.RaceState == RoomRaceState.Racing))
            {
                RoomId = roomState.RoomId;
                RaceInstanceId = roomState.RaceInstanceId;
            }
        }

        public void ApplyRaceState(PacketRoomRaceStateChanged changed)
        {
            if (changed.State != RoomRaceState.Racing || changed.RoomId == 0 || changed.RaceInstanceId == 0)
                return;

            RoomId = changed.RoomId;
            RaceInstanceId = changed.RaceInstanceId;
        }

        public void BindStartedRace()
        {
            if (RoomId != 0 && RoomId != ActiveRoomId)
                RaceInstanceId = 0;
            RoomId = ActiveRoomId;
        }

        public void ClearRaceBinding()
        {
            RoomId = 0;
            RaceInstanceId = 0;
        }

        public bool MatchesRoom(uint roomId)
        {
            if (roomId == 0)
                return true;
            if (RoomId == 0)
            {
                RoomId = roomId;
                return true;
            }

            return RoomId == roomId;
        }

        public bool MatchesContext(uint roomId, uint raceInstanceId, bool allowBindRaceInstance)
        {
            if (!MatchesRoom(roomId))
                return false;
            if (raceInstanceId == 0)
                return true;
            if (RaceInstanceId == 0)
            {
                if (!allowBindRaceInstance)
                    return false;
                RaceInstanceId = raceInstanceId;
                return true;
            }

            return RaceInstanceId == raceInstanceId;
        }
    }
}
