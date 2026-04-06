using TopSpeed.Game;
using TopSpeed.Race;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class RaceResultsBehaviorTests
{
    [Fact]
    public void Time_Formats_Minutes_And_Seconds()
    {
        var fmt = new ResultFmt(new Pick(_ => 0));

        fmt.Time(61000).Should().Be("1 minute and 1 second");
        fmt.Time(59000).Should().Be("59 seconds");
    }

    [Fact]
    public void Line_Uses_First_Template_With_Deterministic_Pick()
    {
        var fmt = new ResultFmt(new Pick(_ => 0));
        var entry = new RaceResultEntry
        {
            Name = "Alice",
            Position = 1,
            TimeMs = 61000
        };

        fmt.Line(entry).Should().Be("Alice: position 1, time 1 minute and 1 second.");
    }

    [Fact]
    public void Build_Race_Winner_Dialog_Plays_Win()
    {
        var dialogs = new ResultDialogs(new Pick(_ => 0), new ResultFmt(new Pick(_ => 0)));
        var summary = new RaceResultSummary
        {
            Mode = RaceResultMode.Race,
            LocalPosition = 1,
            Entries = new[]
            {
                new RaceResultEntry
                {
                    Name = "Alice",
                    Position = 1,
                    TimeMs = 61000
                }
            }
        };

        var plan = dialogs.Build(summary);

        plan.PlayWin.Should().BeTrue();
        plan.Dialog.Title.Should().Be("Congratulations! You have made it to the first position.");
        plan.Dialog.Caption.Should().Be("The following are the details of all players.");
        plan.Dialog.Items.Should().ContainSingle();
    }

    [Fact]
    public void Build_TimeTrial_NoRecord_Includes_PreviousBest()
    {
        var dialogs = new ResultDialogs(new Pick(_ => 0), new ResultFmt(new Pick(_ => 0)));
        var summary = new RaceResultSummary
        {
            Mode = RaceResultMode.TimeTrial,
            TimeTrialBeatRecord = false,
            TimeTrialCurrentTimeMs = 61000,
            TimeTrialPreviousBestTimeMs = 72000
        };

        var plan = dialogs.Build(summary);

        plan.PlayWin.Should().BeFalse();
        plan.Dialog.Title.Should().Be("Time trial complete.");
        plan.Dialog.Caption.Should().Be("Summary of this run and your previous best:");
        plan.Dialog.Items.Should().HaveCount(2);
        plan.Dialog.Items[0].Text.Should().Be("Your time: 1 minute and 1 second.");
        plan.Dialog.Items[1].Text.Should().Be("Your previous best record was: 1 minute and 12 seconds.");
    }

    [Fact]
    public void Show_Triggers_Sound_Only_When_Plan_Requests_It()
    {
        var dialogs = new ResultDialogs(new Pick(_ => 0), new ResultFmt(new Pick(_ => 0)));
        var shownCount = 0;
        var soundCount = 0;
        var show = new ResultShow(_ => shownCount++, () => soundCount++, dialogs);

        show.Show(new RaceResultSummary
        {
            Mode = RaceResultMode.Race,
            LocalPosition = 2,
            Entries = new[]
            {
                new RaceResultEntry
                {
                    Name = "Alice",
                    Position = 2,
                    TimeMs = 61000
                }
            }
        });
        show.Show(new RaceResultSummary
        {
            Mode = RaceResultMode.TimeTrial,
            TimeTrialBeatRecord = true,
            TimeTrialCurrentTimeMs = 61000
        });
        show.Show(null);

        shownCount.Should().Be(2);
        soundCount.Should().Be(1);
    }
}
