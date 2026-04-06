using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Regression")]
public sealed class WheelPedalRegressionTests
{
    [Fact]
    public Task CalibrationTraces_ShouldMatchSnapshot()
    {
        var traces = new object[]
        {
            InputHarness.SimulateFullRangeCalibration(),
            InputHarness.SimulatePartialRangeCalibration()
        };

        return Verifier.Verify(traces, SnapshotSettings.Create("Client", "Input"));
    }
}
