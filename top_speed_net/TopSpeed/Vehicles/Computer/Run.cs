using TopSpeed.Audio;
using TopSpeed.Bots;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Tracks;

namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
        public void Run(float elapsed, float playerX, float playerY)
        {
            RefreshCategoryVolumes();
            if (_positionY < 0f)
                _positionY = 0f;

            _diffX = _positionX - playerX;
            _diffY = _positionY - playerY;
            _diffY = ((_diffY % _trackLength) + _trackLength) % _trackLength;
            if (_diffY > _trackLength / 2)
                _diffY = (_diffY - _trackLength) % _trackLength;

            if (!_horning && _diffY < -100.0f)
            {
                if (Algorithm.RandomInt(2500) == 1)
                {
                    var duration = Algorithm.RandomInt(80);
                    _horning = true;
                    PushEvent(BotEventType.StopHorn, 0.2f + (duration / 80.0f));
                }
            }

            if (_state == ComputerState.Running && _started())
            {
                AI();
                if (_currentBrake != 0 && _surface == TrackSurface.Asphalt)
                {
                    if (!_soundBrake.IsPlaying)
                        _soundBrake.Play(loop: true);
                }
                else if (_soundBrake.IsPlaying)
                {
                    _soundBrake.Stop();
                }

                var beforeSpeed = _speed;
                var physicsState = new BotPhysicsState
                {
                    PositionX = _positionX,
                    PositionY = _positionY,
                    SpeedKph = _speed,
                    LateralVelocityMps = _lateralVelocityMps,
                    YawRateRad = _yawRateRad,
                    Gear = _gear,
                    AutoShiftCooldownSeconds = _autoShiftCooldown
                };
                var physicsInput = new BotPhysicsInput(elapsed, _surface, _currentThrottle, _currentBrake, _currentSteering);
                BotPhysics.Step(_physicsConfig, ref physicsState, physicsInput);

                _positionX = physicsState.PositionX;
                _positionY = physicsState.PositionY;
                _speed = physicsState.SpeedKph;
                _lateralVelocityMps = physicsState.LateralVelocityMps;
                _yawRateRad = physicsState.YawRateRad;
                _gear = physicsState.Gear;
                _autoShiftCooldown = physicsState.AutoShiftCooldownSeconds;
                _speedDiff = _speed - beforeSpeed;

                _engine.SyncFromSpeed(_speed, _gear, elapsed, _currentThrottle, inReverse: false);

                if (_frame % 4 == 0)
                {
                    _frame = 0;
                    var speedRatio = NormalizeSpeedByTopSpeed(_speed, 1f);
                    _brakeFrequency = (int)(11025 + (22050 * speedRatio));
                    if (_brakeFrequency != _prevBrakeFrequency)
                    {
                        _soundBrake.SetFrequency(_brakeFrequency);
                        _prevBrakeFrequency = _brakeFrequency;
                    }
                    UpdateEngineFreq();
                }

                var road = _track.RoadComputer(_positionY);
                if (!_finished)
                    Evaluate(road);
            }
            else if (_state == ComputerState.Stopping)
            {
                _speed -= (elapsed * 100 * _deceleration);
                if (_speed < 0)
                    _speed = 0;
                if (_frame % 4 == 0)
                {
                    _frame = 0;
                    UpdateEngineFreq();
                }
                _frame++;
            }

            if (_horning && _state == ComputerState.Running)
            {
                if (!_soundHorn.IsPlaying)
                    _soundHorn.Play(loop: true);
            }
            else
            {
                if (_soundHorn.IsPlaying)
                    _soundHorn.Stop();
            }

            if (_crashLateralAnchored && !_soundCrash.IsPlaying)
                _crashLateralAnchored = false;

            for (var i = _events.Count - 1; i >= 0; i--)
            {
                var e = _events[i];
                if (e.Time < _currentTime())
                {
                    _events.RemoveAt(i);
                    switch (e.Type)
                    {
                        case BotEventType.CarStart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarStart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} engine start.");
                            _soundEngine.SetFrequency(_idleFreq);
                            _soundEngine.Play(loop: true);
                            _state = ComputerState.Running;
                            break;
                        case BotEventType.CarComputerStart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarComputerStart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} start trigger.");
                            Start();
                            break;
                        case BotEventType.CarRestart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarRestart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} restart trigger.");
                            Start();
                            break;
                        case BotEventType.InGear:
                            _switchingGear = 0;
                            break;
                        case BotEventType.StopHorn:
                            _horning = false;
                            break;
                        case BotEventType.StartHorn:
                            _horning = true;
                            break;
                    }
                }
            }

            UpdateSpatialAudio(playerX, playerY, _trackLength, elapsed);
        }

        public void Evaluate(Track.Road road)
        {
            if (_state == ComputerState.Running && _started())
            {
                if (_frame % 4 == 0)
                {
                    var laneHalfWidth = System.Math.Max(0.1f, System.Math.Abs(road.Right - road.Left) * 0.5f);
                    _relPos = BotRaceRules.CalculateRelativeLanePosition(_positionX, road.Left, laneHalfWidth);
                    if (BotRaceRules.IsOutsideRoad(_relPos))
                    {
                        var fullCrash = BotRaceRules.IsFullCrash(_gear, _speed);
                        if (fullCrash)
                            Crash(BotRaceRules.RoadCenter(road.Left, road.Right));
                        else
                            MiniCrash(BotRaceRules.RoadCenter(road.Left, road.Right));
                    }
                }
            }

            _surface = road.Surface;
            _frame++;
        }

        private void PushEvent(BotEventType type, float time)
        {
            _events.Add(new BotEvent { Type = type, Time = _currentTime() + time });
        }
    }
}
