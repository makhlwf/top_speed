using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class EnginePitchTests
    {
        [Fact]
        public void FromRpm_AtIdle_UsesIdleFrequency()
        {
            var frequency = EnginePitch.FromRpm(
                rpm: 700f,
                stallRpm: 385f,
                idleRpm: 700f,
                revLimiter: 7000f,
                idleFreq: 420,
                topFreq: 2200,
                pitchCurveExponent: 1f);

            Assert.Equal(420, frequency);
        }

        [Fact]
        public void FromRpm_AtStall_UsesSubIdleFloor()
        {
            var frequency = EnginePitch.FromRpm(
                rpm: 385f,
                stallRpm: 385f,
                idleRpm: 700f,
                revLimiter: 7000f,
                idleFreq: 420,
                topFreq: 2200,
                pitchCurveExponent: 1f);

            Assert.Equal(231, frequency);
        }

        [Fact]
        public void FromRpm_BetweenStallAndIdle_IsBelowIdleFrequency()
        {
            var frequency = EnginePitch.FromRpm(
                rpm: 540f,
                stallRpm: 385f,
                idleRpm: 700f,
                revLimiter: 7000f,
                idleFreq: 420,
                topFreq: 2200,
                pitchCurveExponent: 1f);

            Assert.InRange(frequency, 232, 419);
        }
    }
}

