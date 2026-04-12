using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using MiniAudioEx.Native;

namespace TS.Audio
{
    public sealed partial class AudioSourceHandle
    {
        public void SetPosition(Vector3 position)
        {
            ThrowIfDisposed();
            if (!_spatialize)
                return;

            Volatile.Write(ref _spatial.PosX, position.X);
            Volatile.Write(ref _spatial.PosY, position.Y);
            Volatile.Write(ref _spatial.PosZ, position.Z);

            if (!_graph.UsesHrtf)
            {
                var mapped = ToMaVec3(position);
                MiniAudioNative.ma_sound_group_set_position(_group, mapped.x, mapped.y, mapped.z);
            }

            Emit(
                AudioDiagnosticLevel.Trace,
                AudioDiagnosticKind.SourcePositionChanged,
                "Audio source position changed.",
                new Dictionary<string, object?>
                {
                    ["x"] = position.X,
                    ["y"] = position.Y,
                    ["z"] = position.Z
                });
        }

        public void SetVelocity(Vector3 velocity)
        {
            ThrowIfDisposed();
            if (!_spatialize)
                return;

            Volatile.Write(ref _spatial.VelX, velocity.X);
            Volatile.Write(ref _spatial.VelY, velocity.Y);
            Volatile.Write(ref _spatial.VelZ, velocity.Z);

            if (!_graph.UsesHrtf)
            {
                var mapped = ToMaVec3(velocity);
                MiniAudioNative.ma_sound_group_set_velocity(_group, mapped.x, mapped.y, mapped.z);
            }

            Emit(
                AudioDiagnosticLevel.Trace,
                AudioDiagnosticKind.SourceVelocityChanged,
                "Audio source velocity changed.",
                new Dictionary<string, object?>
                {
                    ["x"] = velocity.X,
                    ["y"] = velocity.Y,
                    ["z"] = velocity.Z
                });
        }

        public void SetDistanceModel(DistanceModel model, float refDistance, float maxDistance, float rolloff)
        {
            ThrowIfDisposed();
            if (!_spatialize)
                return;

            if (refDistance <= 0f)
                refDistance = 0.0001f;
            if (maxDistance <= 0f)
                maxDistance = MaxDistanceInfinite;
            if (maxDistance < refDistance)
                maxDistance = refDistance;

            Volatile.Write(ref _spatial.RefDistance, refDistance);
            Volatile.Write(ref _spatial.MaxDistance, maxDistance);
            Volatile.Write(ref _spatial.RollOff, rolloff);
            _spatial.DistanceModel = model;

            MiniAudioNative.ma_sound_group_set_min_distance(_group, refDistance);
            MiniAudioNative.ma_sound_group_set_max_distance(_group, maxDistance);
            MiniAudioNative.ma_sound_group_set_rolloff(_group, rolloff);
            MiniAudioNative.ma_sound_group_set_attenuation_model(_group, ToMaAttenuationModel(model));
            Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceDistanceModelChanged,
                "Audio source distance model changed.",
                new Dictionary<string, object?>
                {
                    ["model"] = model.ToString(),
                    ["refDistance"] = refDistance,
                    ["maxDistance"] = maxDistance,
                    ["rollOff"] = rolloff
                });
        }

        public void SetRoomAcoustics(RoomAcoustics acoustics)
        {
            Volatile.Write(ref _spatial.RoomFlags, acoustics.HasRoom ? AudioSourceSpatialParams.RoomHasProfile : 0);
            Volatile.Write(ref _spatial.RoomReverbTimeSeconds, acoustics.ReverbTimeSeconds);
            Volatile.Write(ref _spatial.RoomReverbGain, acoustics.ReverbGain);
            Volatile.Write(ref _spatial.RoomHfDecayRatio, acoustics.HfDecayRatio);
            Volatile.Write(ref _spatial.RoomLateReverbGain, acoustics.LateReverbGain);
            Volatile.Write(ref _spatial.RoomDiffusion, acoustics.Diffusion);
            Volatile.Write(ref _spatial.RoomAirAbsorptionScale, acoustics.AirAbsorptionScale);
            Volatile.Write(ref _spatial.RoomOcclusionScale, acoustics.OcclusionScale);
            Volatile.Write(ref _spatial.RoomTransmissionScale, acoustics.TransmissionScale);
            Volatile.Write(ref _spatial.RoomOcclusionOverride, acoustics.OcclusionOverride ?? float.NaN);
            Volatile.Write(ref _spatial.RoomTransmissionOverrideLow, acoustics.TransmissionOverrideLow ?? float.NaN);
            Volatile.Write(ref _spatial.RoomTransmissionOverrideMid, acoustics.TransmissionOverrideMid ?? float.NaN);
            Volatile.Write(ref _spatial.RoomTransmissionOverrideHigh, acoustics.TransmissionOverrideHigh ?? float.NaN);
            Volatile.Write(ref _spatial.RoomAirAbsorptionOverrideLow, acoustics.AirAbsorptionOverrideLow ?? float.NaN);
            Volatile.Write(ref _spatial.RoomAirAbsorptionOverrideMid, acoustics.AirAbsorptionOverrideMid ?? float.NaN);
            Volatile.Write(ref _spatial.RoomAirAbsorptionOverrideHigh, acoustics.AirAbsorptionOverrideHigh ?? float.NaN);
            Emit(
                AudioDiagnosticLevel.Trace,
                AudioDiagnosticKind.SourceRoomAcousticsChanged,
                "Audio source room acoustics changed.",
                new Dictionary<string, object?>
                {
                    ["hasRoom"] = acoustics.HasRoom,
                    ["reverbTimeSeconds"] = acoustics.ReverbTimeSeconds,
                    ["occlusionScale"] = acoustics.OcclusionScale
                });
        }

        internal void ApplyDirectSimulation(float occlusion, float airLow, float airMid, float airHigh, float transLow, float transMid, float transHigh)
        {
            Volatile.Write(ref _spatial.Occlusion, occlusion);
            Volatile.Write(ref _spatial.AirAbsLow, airLow);
            Volatile.Write(ref _spatial.AirAbsMid, airMid);
            Volatile.Write(ref _spatial.AirAbsHigh, airHigh);
            Volatile.Write(ref _spatial.TransLow, transLow);
            Volatile.Write(ref _spatial.TransMid, transMid);
            Volatile.Write(ref _spatial.TransHigh, transHigh);
            Volatile.Write(ref _spatial.SimulationFlags,
                AudioSourceSpatialParams.SimOcclusion |
                AudioSourceSpatialParams.SimTransmission |
                AudioSourceSpatialParams.SimAirAbsorption);
        }

        internal void ApplyReverbSimulation(float timeLow, float timeMid, float timeHigh, float eqLow, float eqMid, float eqHigh, int delay)
        {
            Volatile.Write(ref _spatial.ReverbTimeLow, timeLow);
            Volatile.Write(ref _spatial.ReverbTimeMid, timeMid);
            Volatile.Write(ref _spatial.ReverbTimeHigh, timeHigh);
            Volatile.Write(ref _spatial.ReverbEqLow, eqLow);
            Volatile.Write(ref _spatial.ReverbEqMid, eqMid);
            Volatile.Write(ref _spatial.ReverbEqHigh, eqHigh);
            Volatile.Write(ref _spatial.ReverbDelay, delay);
            Volatile.Write(ref _spatial.SimulationFlags, _spatial.SimulationFlags | AudioSourceSpatialParams.SimReflections);
        }

        public void ApplyCurveDistanceScaler(float curveDistanceScaler)
        {
            if (!_spatialize)
                return;

            if (curveDistanceScaler <= 0f)
                curveDistanceScaler = 0.0001f;

            SetDistanceModel(DistanceModel.Inverse, curveDistanceScaler, MaxDistanceInfinite, 1.0f);
        }

        public void SetDopplerFactor(float dopplerFactor)
        {
            if (!_spatialize)
                return;

            _dopplerFactor = Math.Max(0f, dopplerFactor);
            MiniAudioNative.ma_sound_group_set_doppler_factor(_group, _dopplerFactor);
            Emit(
                AudioDiagnosticLevel.Trace,
                AudioDiagnosticKind.SourceDopplerChanged,
                "Audio source doppler factor changed.",
                new Dictionary<string, object?>
                {
                    ["dopplerFactor"] = _dopplerFactor
                });
        }

        public void UpdateDoppler(Vector3 listenerPos, Vector3 listenerVel, AudioSystemConfig config)
        {
            if (!_graph.UsesHrtf)
                return;

            var srcPos = new Vector3(
                Volatile.Read(ref _spatial.PosX),
                Volatile.Read(ref _spatial.PosY),
                Volatile.Read(ref _spatial.PosZ));

            var srcVel = new Vector3(
                Volatile.Read(ref _spatial.VelX),
                Volatile.Read(ref _spatial.VelY),
                Volatile.Read(ref _spatial.VelZ));

            var rel = srcPos - listenerPos;
            var distance = rel.Length();
            if (distance <= 0.0001f)
            {
                MiniAudioNative.ma_sound_group_set_pitch(_group, _basePitch);
                return;
            }

            var dir = rel / distance;
            float vL = Vector3.Dot(listenerVel, dir);
            float vS = Vector3.Dot(srcVel, dir);

            float c = config.SpeedOfSound;
            var dopplerFactor = config.DopplerFactor * _dopplerFactor;
            if (dopplerFactor <= 0f)
            {
                MiniAudioNative.ma_sound_group_set_pitch(_group, _basePitch);
                return;
            }

            float doppler = (c + dopplerFactor * vL) / (c + dopplerFactor * vS);
            if (doppler < 0.5f) doppler = 0.5f;
            if (doppler > 2.0f) doppler = 2.0f;

            MiniAudioNative.ma_sound_group_set_pitch(_group, _basePitch * doppler);
        }

        private static ma_attenuation_model ToMaAttenuationModel(DistanceModel model)
        {
            switch (model)
            {
                case DistanceModel.Linear:
                    return ma_attenuation_model.linear;
                case DistanceModel.Exponential:
                    return ma_attenuation_model.exponential;
                default:
                    return ma_attenuation_model.inverse;
            }
        }

        private static ma_vec3f ToMaVec3(Vector3 value)
        {
            return new ma_vec3f { x = value.X, y = value.Y, z = -value.Z };
        }
    }
}
