using System;
using System.Numerics;
using TopSpeed.Audio;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
        private void UpdateSpatialAudio(float listenerX, float listenerY, float trackLength, float elapsed)
        {
            var road = _track.RoadComputer(_positionY);
            var laneHalfWidth = Math.Max(0.1f, Math.Abs(road.Right - road.Left) * 0.5f);
            var roadCenterX = (road.Left + road.Right) * 0.5f;

            var lateralFromCenter = _positionX - roadCenterX;
            var dz = _positionY - listenerY;
            var normalizedLateral = (lateralFromCenter / laneHalfWidth) * AudioLateralBoost;
            if (normalizedLateral < -1f)
                normalizedLateral = -1f;
            else if (normalizedLateral > 1f)
                normalizedLateral = 1f;

            var absDz = Math.Abs(dz);
            var radialDistance = absDz < 1f ? 1f : absDz;
            var lateralAngle = normalizedLateral * (float)(Math.PI / 2.0);
            var worldX = listenerX + (float)Math.Sin(lateralAngle) * radialDistance;
            var worldZ = listenerY + ((dz < 0f ? -1f : 1f) * (float)Math.Cos(lateralAngle) * radialDistance);

            var position = AudioWorld.Position(worldX, worldZ);
            var crashNormalizedLateral = normalizedLateral;
            if (_crashLateralAnchored)
            {
                crashNormalizedLateral = (_crashLateralFromCenter / laneHalfWidth) * AudioLateralBoost;
                if (crashNormalizedLateral < -1f)
                    crashNormalizedLateral = -1f;
                else if (crashNormalizedLateral > 1f)
                    crashNormalizedLateral = 1f;
            }
            var crashAngle = crashNormalizedLateral * (float)(Math.PI / 2.0);
            var crashWorldX = listenerX + (float)Math.Sin(crashAngle) * radialDistance;
            var crashWorldZ = listenerY + ((dz < 0f ? -1f : 1f) * (float)Math.Cos(crashAngle) * radialDistance);
            var crashPosition = AudioWorld.Position(crashWorldX, crashWorldZ);

            var velocity = Vector3.Zero;
            var velUnits = Vector3.Zero;
            if (_audioInitialized && elapsed > 0f)
            {
                velUnits = new Vector3((worldX - _lastAudioPosition.X) / elapsed, 0f, (worldZ - _lastAudioPosition.Z) / elapsed);
                velocity = AudioWorld.ToMeters(velUnits);
            }
            _lastAudioPosition = new Vector3(worldX, 0f, worldZ);
            _audioInitialized = true;

            SetSpatial(_soundEngine, position, velocity);
            SetSpatial(_soundStart, position, velocity);
            SetSpatial(_soundHorn, position, velocity);
            SetSpatial(_soundCrash, crashPosition, velocity);
            SetSpatial(_soundBrake, position, velocity);
            SetSpatial(_soundBackfire, position, velocity);
            SetSpatial(_soundBump, position, velocity);
            SetSpatial(_soundMiniCrash, position, velocity);
            _radio.UpdateSpatial(worldX, worldZ, velUnits);
            _liveRadio.UpdateSpatial(position, velocity);
        }

        private static void SetSpatial(AudioSourceHandle? sound, Vector3 position, Vector3 velocity)
        {
            if (sound == null)
                return;
            sound.SetPosition(position);
            sound.SetVelocity(velocity);
        }
    }
}
