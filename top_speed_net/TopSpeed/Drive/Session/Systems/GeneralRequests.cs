using System;
using TopSpeed.Input;
using TopSpeed.Vehicles;

namespace TopSpeed.Drive.Session.Systems
{
    internal sealed class GeneralRequests : Subsystem
    {
        private readonly DriveInput _input;
        private readonly ICar _car;
        private readonly Func<bool> _isStarted;
        private readonly Func<int> _getLap;
        private readonly Func<int> _getLapLimit;
        private readonly Action _requestPause;
        private bool _pauseKeyReleased = true;

        public GeneralRequests(
            string name,
            int order,
            DriveInput input,
            ICar car,
            Func<bool> isStarted,
            Func<int> getLap,
            Func<int> getLapLimit,
            Action requestPause)
            : base(name, order)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _car = car ?? throw new ArgumentNullException(nameof(car));
            _isStarted = isStarted ?? throw new ArgumentNullException(nameof(isStarted));
            _getLap = getLap ?? throw new ArgumentNullException(nameof(getLap));
            _getLapLimit = getLapLimit ?? throw new ArgumentNullException(nameof(getLapLimit));
            _requestPause = requestPause ?? throw new ArgumentNullException(nameof(requestPause));
        }

        public override void Update(SessionContext context, float elapsed)
        {
            if (!_input.Intents.IsTriggered(DriveIntent.Pause) && !_pauseKeyReleased)
            {
                _pauseKeyReleased = true;
                return;
            }

            if (_input.Intents.IsTriggered(DriveIntent.Pause) && _pauseKeyReleased && _isStarted() && _getLap() <= _getLapLimit() && _car.State == CarState.Running)
            {
                _pauseKeyReleased = false;
                _requestPause();
            }
        }

        public void Reset()
        {
            _pauseKeyReleased = true;
        }
    }
}
