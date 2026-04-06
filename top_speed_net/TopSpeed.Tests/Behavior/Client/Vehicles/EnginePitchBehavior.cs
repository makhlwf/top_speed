using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class EnginePitchBehaviorTests
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

        frequency.Should().Be(420);
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

        frequency.Should().Be(231);
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

        frequency.Should().BeInRange(232, 419);
    }
}
