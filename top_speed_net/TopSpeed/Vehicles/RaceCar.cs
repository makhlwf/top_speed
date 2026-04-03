using System;
using TopSpeed.Audio;
using TopSpeed.Input;
using TopSpeed.Tracks;
using TS.Audio;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Vehicles
{
    internal sealed class RaceCar : Car
    {
        public RaceCar(
            AudioManager audio,
            Track track,
            RaceInput input,
            RaceSettings settings,
            int vehicleIndex,
            string? vehicleFile,
            Func<float> currentTime,
            Func<bool> started,
            IVibrationDevice? vibrationDevice = null)
            : base(audio, track, input, settings, vehicleIndex, vehicleFile, currentTime, started, vibrationDevice)
        {
        }
    }
}

