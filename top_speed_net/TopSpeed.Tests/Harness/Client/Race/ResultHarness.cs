using System.Linq;
using TopSpeed.Game;
using TopSpeed.Race;

namespace TopSpeed.Tests;

internal static class ResultHarness
{
    public static object BuildSnapshot()
    {
        var dialogs = new ResultDialogs(new Pick(_ => 0), new ResultFmt(new Pick(_ => 0)));

        return new[]
        {
            Project(dialogs.Build(new RaceResultSummary
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
                    },
                    new RaceResultEntry
                    {
                        Name = "Bob",
                        Position = 2,
                        TimeMs = 64500
                    }
                }
            })),
            Project(dialogs.Build(new RaceResultSummary
            {
                Mode = RaceResultMode.TimeTrial,
                TimeTrialBeatRecord = false,
                TimeTrialCurrentTimeMs = 61000,
                TimeTrialPreviousBestTimeMs = 72000
            }))
        };
    }

    private static object Project(ResultPlan plan) => new
    {
        plan.PlayWin,
        Dialog = new
        {
            plan.Dialog.Title,
            plan.Dialog.Caption,
            Items = plan.Dialog.Items.Select(x => new
            {
                x.Text
            }).ToArray()
        }
    };
}
