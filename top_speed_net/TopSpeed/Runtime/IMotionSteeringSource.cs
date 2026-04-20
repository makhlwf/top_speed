using System;

namespace TopSpeed.Runtime
{
    public interface IMotionSteeringSource : IDisposable
    {
        bool IsAvailable { get; }

        void Recenter();

        bool TryGetSteeringAngleRadians(out float angleRadians);
    }
}
