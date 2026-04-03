using TopSpeed.Data;
using TS.Audio;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        public void Initialize()
        {
            _lapDistance = 0;
            for (var i = 0; i < _segmentCount; i++)
            {
                _segmentStartDistances[i] = _lapDistance;
                _lapDistance += _definition[i].Length;
            }

            _roadModel = new RoadModel(_definition, _laneWidth);
            _lapDistance = _roadModel.LapDistance;
            _lapCenter = _roadModel.LapCenter;

            if (_weather == TrackWeather.Rain)
                _soundRain?.Play(loop: true);
            else if (_weather == TrackWeather.Wind)
                _soundWind?.Play(loop: true);
            else if (_weather == TrackWeather.Storm)
                _soundStorm?.Play(loop: true);

            if (_ambience == TrackAmbience.Desert)
                _soundDesert?.Play(loop: true);
            else if (_ambience == TrackAmbience.Airport)
                _soundAirport?.Play(loop: true);

            if (_segmentCount > 0)
                ApplySegmentAcoustics(0);
            ActivateTrackSoundsForPosition(0f, 0);
        }

        public void FinalizeTrack()
        {
            if (_weather == TrackWeather.Rain)
                _soundRain?.Stop();
            else if (_weather == TrackWeather.Wind)
                _soundWind?.Stop();
            else if (_weather == TrackWeather.Storm)
                _soundStorm?.Stop();

            if (_ambience == TrackAmbience.Desert)
                _soundDesert?.Stop();
            else if (_ambience == TrackAmbience.Airport)
                _soundAirport?.Stop();

            for (var i = 0; i < _allTrackSounds.Count; i++)
                _allTrackSounds[i].Stop();

            DisposePendingHandleStops();

            _activeAudioSegmentIndex = -1;
            _activeRoomAcoustics = RoomAcoustics.Default;
            _audio.SetRoomAcoustics(RoomAcoustics.Default);
        }

        public void Run(float position)
        {
            UpdatePendingHandleStops();

            if (_noisePlaying && position > _noiseEndPos)
                _noisePlaying = false;

            if (_segmentCount == 0)
                return;

            var segmentIndex = RoadIndexAt(position);
            if (segmentIndex >= 0)
            {
                if (segmentIndex != _activeAudioSegmentIndex)
                    ApplySegmentAcoustics(segmentIndex);
                ActivateTrackSoundsForPosition(position, segmentIndex);
            }

            switch (_definition[_currentRoad].Noise)
            {
                case TrackNoise.Crowd:
                    UpdateLoopingNoise(_soundCrowd, position);
                    break;
                case TrackNoise.Ocean:
                    UpdateLoopingNoise(_soundOcean, position, pan: -10);
                    break;
                case TrackNoise.Runway:
                    PlayIfNotPlaying(_soundAirplane);
                    break;
                case TrackNoise.Clock:
                    UpdateLoopingNoise(_soundClock, position, pan: 25);
                    break;
                case TrackNoise.Jet:
                    PlayIfNotPlaying(_soundJet);
                    break;
                case TrackNoise.Thunder:
                    PlayIfNotPlaying(_soundThunder);
                    break;
                case TrackNoise.Pile:
                    UpdateLoopingNoise(_soundPile, position);
                    break;
                case TrackNoise.Construction:
                    UpdateLoopingNoise(_soundConstruction, position);
                    break;
                case TrackNoise.River:
                    UpdateLoopingNoise(_soundRiver, position);
                    break;
                case TrackNoise.Helicopter:
                    PlayIfNotPlaying(_soundHelicopter);
                    break;
                case TrackNoise.Owl:
                    PlayIfNotPlaying(_soundOwl);
                    break;
                default:
                    _soundCrowd?.Stop();
                    _soundOcean?.Stop();
                    _soundClock?.Stop();
                    _soundPile?.Stop();
                    _soundConstruction?.Stop();
                    _soundRiver?.Stop();
                    break;
            }
        }
    }
}

