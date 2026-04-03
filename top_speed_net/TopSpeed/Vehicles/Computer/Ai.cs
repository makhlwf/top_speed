using System;
using TopSpeed.Bots;
using TopSpeed.Common;

namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
        private void AI()
        {
            var road = _track.RoadComputer(_positionY);
            var laneHalfWidth = Math.Max(0.1f, Math.Abs(road.Right - road.Left) * 0.5f);
            _relPos = BotRaceRules.CalculateRelativeLanePosition(_positionX, road.Left, laneHalfWidth);
            var nextRoad = _track.RoadComputer(_positionY + CallLength);
            var nextLaneHalfWidth = Math.Max(0.1f, Math.Abs(nextRoad.Right - nextRoad.Left) * 0.5f);
            _nextRelPos = BotRaceRules.CalculateRelativeLanePosition(_positionX, nextRoad.Left, nextLaneHalfWidth);
            BotSharedModel.GetControlInputs(_difficulty, _random, road.Type, nextRoad.Type, _relPos, out var throttle, out var steering);
            _currentThrottle = (int)Math.Round(throttle);
            _currentSteering = (int)Math.Round(steering);
        }

        private void Horn()
        {
            var duration = Algorithm.RandomInt(80);
            PushEvent(BotEventType.StartHorn, 0.3f);
            PushEvent(BotEventType.StopHorn, 0.5f + duration / 80.0f);
        }
    }
}

