using TopSpeed.Game;
using TopSpeed.Race;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "GameFlow")]
    public sealed class RaceResultsTests
    {
        [Fact]
        public void Time_Formats_Minutes_And_Seconds()
        {
            var fmt = new ResultFmt(new Pick(_ => 0));

            Assert.Equal("1 minute and 1 second", fmt.Time(61000));
            Assert.Equal("59 seconds", fmt.Time(59000));
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

            var line = fmt.Line(entry);

            Assert.Equal("Alice: position 1, time 1 minute and 1 second.", line);
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

            Assert.True(plan.PlayWin);
            Assert.Equal("Congratulations! You have made it to the first position.", plan.Dialog.Title);
            Assert.Equal("The following are the details of all players.", plan.Dialog.Caption);
            Assert.Single(plan.Dialog.Items);
        }

        [Fact]
        public void Build_Time_Trial_No_Record_Includes_Previous_Best()
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

            Assert.False(plan.PlayWin);
            Assert.Equal("Time trial complete.", plan.Dialog.Title);
            Assert.Equal("Summary of this run and your previous best:", plan.Dialog.Caption);
            Assert.Equal(2, plan.Dialog.Items.Count);
            Assert.Equal("Your time: 1 minute and 1 second.", plan.Dialog.Items[0].Text);
            Assert.Equal("Your previous best record was: 1 minute and 12 seconds.", plan.Dialog.Items[1].Text);
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

            Assert.Equal(2, shownCount);
            Assert.Equal(1, soundCount);
        }
    }
}


