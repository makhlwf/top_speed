using System;
using TopSpeed.Data;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        public virtual void BrakeSound()
        {
            switch (_surface)
            {
                case TrackSurface.Asphalt:
                    if (!_soundBrake.IsPlaying)
                    {
                        SetSurfaceLoopVolumePercent(_soundAsphalt, 90);
                        _soundBrake.Play(loop: true);
                    }
                    break;
                case TrackSurface.Gravel:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    if (_speed <= 50.0f)
                        SetSurfaceLoopVolumePercent(_soundGravel, (int)(100 - (10 - (_speed / 5))));
                    else
                        SetSurfaceLoopVolumePercent(_soundGravel, 100);
                    break;
                case TrackSurface.Water:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    if (_speed <= 50.0f)
                        SetSurfaceLoopVolumePercent(_soundWater, (int)(100 - (10 - (_speed / 5))));
                    else
                        SetSurfaceLoopVolumePercent(_soundWater, 100);
                    break;
                case TrackSurface.Sand:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    if (_speed <= 50.0f)
                        SetSurfaceLoopVolumePercent(_soundSand, (int)(100 - (10 - (_speed / 5))));
                    else
                        SetSurfaceLoopVolumePercent(_soundSand, 100);
                    break;
                case TrackSurface.Snow:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    if (_speed <= 50.0f)
                        SetSurfaceLoopVolumePercent(_soundSnow, (int)(100 - (10 - (_speed / 5))));
                    else
                        SetSurfaceLoopVolumePercent(_soundSnow, 100);
                    break;
            }
        }

        public virtual void BrakeCurveSound()
        {
            switch (_surface)
            {
                case TrackSurface.Asphalt:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    SetSurfaceLoopVolumePercent(_soundAsphalt, 92 * Math.Abs(_currentSteering) / 100);
                    break;
                case TrackSurface.Gravel:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    SetSurfaceLoopVolumePercent(_soundGravel, 92 * Math.Abs(_currentSteering) / 100);
                    break;
                case TrackSurface.Water:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    SetSurfaceLoopVolumePercent(_soundWater, 92 * Math.Abs(_currentSteering) / 100);
                    break;
                case TrackSurface.Sand:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    SetSurfaceLoopVolumePercent(_soundSand, 92 * Math.Abs(_currentSteering) / 100);
                    break;
                case TrackSurface.Snow:
                    if (_soundBrake.IsPlaying)
                        _soundBrake.Stop();
                    SetSurfaceLoopVolumePercent(_soundSnow, 92 * Math.Abs(_currentSteering) / 100);
                    break;
            }
        }

        public virtual bool Backfiring() => AnyBackfirePlaying();

        public virtual void Pause()
        {
            if (_soundStop != null && _soundStop.IsPlaying)
                _soundStop.Stop();
            _audioFlow.Pause(
                _surface,
                _soundEngine,
                _soundThrottle,
                _soundBrake,
                _soundHorn,
                _soundWipers,
                _soundAsphalt,
                _soundGravel,
                _soundWater,
                _soundSand,
                _soundSnow,
                StopResetBackfireVariants);
        }

        public virtual void Unpause()
        {
            _audioFlow.Unpause(
                _surface,
                _soundEngine,
                _soundThrottle,
                _soundWipers,
                _soundAsphalt,
                _soundGravel,
                _soundWater,
                _soundSand,
                _soundSnow);
        }
    }
}

