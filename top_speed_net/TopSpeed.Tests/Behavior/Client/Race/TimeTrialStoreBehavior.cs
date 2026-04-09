using System;
using System.IO;
using TopSpeed.Race.TimeTrial;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TimeTrialStoreBehaviorTests : IDisposable
{
    private readonly string _directory;
    private readonly string _path;

    public TimeTrialStoreBehaviorTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "topspeed-time-trial-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "time_trial.dat");
    }

    [Fact]
    public void RecordRun_Persists_Run_And_Lap_Stats()
    {
        var store = new Store(_path);

        var snapshot = store.RecordRun("builtin:france", "France", 3, 60000, new[] { 21000, 20000, 19000 });

        snapshot.RunBestMs.Should().Be(60000);
        snapshot.RunAverageMs.Should().Be(60000);
        snapshot.RunCount.Should().Be(1);
        snapshot.LapBestMs.Should().Be(19000);
        snapshot.LapAverageMs.Should().Be(20000);
        snapshot.LapCount.Should().Be(3);
    }

    [Fact]
    public void Read_Keeps_Run_Averages_Per_Lap_Count()
    {
        var store = new Store(_path);
        store.RecordRun("builtin:france", "France", 3, 60000, new[] { 21000, 20000, 19000 });
        store.RecordRun("builtin:france", "France", 3, 63000, new[] { 22000, 21000, 20000 });
        store.RecordRun("builtin:france", "France", 5, 102000, new[] { 21000, 20500, 20000, 19500, 19000 });

        var threeLap = store.Read("builtin:france", 3);
        var fiveLap = store.Read("builtin:france", 5);

        threeLap.RunBestMs.Should().Be(60000);
        threeLap.RunAverageMs.Should().Be(61500);
        threeLap.RunCount.Should().Be(2);
        fiveLap.RunBestMs.Should().Be(102000);
        fiveLap.RunAverageMs.Should().Be(102000);
        fiveLap.RunCount.Should().Be(1);
        fiveLap.LapBestMs.Should().Be(19000);
        fiveLap.LapCount.Should().Be(11);
    }

    [Fact]
    public void Read_Corrupt_File_Resets_To_Empty_And_Quarantines_Source()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllBytes(_path, new byte[] { 1, 2, 3, 4, 5 });

        var store = new Store(_path);
        var snapshot = store.Read("builtin:france", 3);

        snapshot.RunBestMs.Should().Be(0);
        Directory.GetFiles(_directory, "*.broken").Should().ContainSingle();
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
