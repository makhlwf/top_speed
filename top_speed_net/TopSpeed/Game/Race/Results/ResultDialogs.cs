using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Race;

namespace TopSpeed.Game
{
    internal sealed class ResultDialogs
    {
        private readonly Pick _pick;
        private readonly ResultFmt _fmt;

        public ResultDialogs(Pick pick, ResultFmt fmt)
        {
            _pick = pick ?? throw new ArgumentNullException(nameof(pick));
            _fmt = fmt ?? throw new ArgumentNullException(nameof(fmt));
        }

        public ResultPlan Build(RaceResultSummary summary)
        {
            if (summary == null)
                throw new ArgumentNullException(nameof(summary));

            if (summary.Mode == RaceResultMode.TimeTrial)
                return BuildTimeTrial(summary);
            return BuildRace(summary);
        }

        private ResultPlan BuildRace(RaceResultSummary summary)
        {
            var localWon = summary.LocalPosition == 1;
            var title = _pick.One(localWon ? ResultCatalog.WinnerTitles : ResultCatalog.NonWinnerTitles);
            var caption = _pick.One(localWon ? ResultCatalog.WinnerCaptions : ResultCatalog.NonWinnerCaptions);

            var items = new List<DialogItem>();
            var entries = summary.Entries ?? Array.Empty<RaceResultEntry>();
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null)
                    continue;
                items.Add(new DialogItem(_fmt.Line(entry)));
            }

            var dialog = new Dialog(
                title,
                caption,
                QuestionId.Close,
                items,
                onResult: null,
                new DialogButton(QuestionId.Close, LocalizationService.Mark("Close")));

            return new ResultPlan(dialog, playWin: localWon);
        }

        private ResultPlan BuildTimeTrial(RaceResultSummary summary)
        {
            var beatRecord = summary.TimeTrialBeatRecord;
            var title = _pick.One(beatRecord ? ResultCatalog.TimeTrialRecordTitles : ResultCatalog.TimeTrialNoRecordTitles);
            var caption = _pick.One(beatRecord ? ResultCatalog.TimeTrialRecordCaptions : ResultCatalog.TimeTrialNoRecordCaptions);

            var items = new List<DialogItem>
            {
                new DialogItem(LocalizationService.Format(
                    _pick.One(ResultCatalog.TimeTrialCurrentLineTemplates),
                    _fmt.Time(summary.TimeTrialCurrentTimeMs)))
            };

            if (!beatRecord && summary.TimeTrialPreviousBestTimeMs > 0)
            {
                items.Add(new DialogItem(LocalizationService.Format(
                    _pick.One(ResultCatalog.TimeTrialPreviousBestLineTemplates),
                    _fmt.Time(summary.TimeTrialPreviousBestTimeMs))));
            }

            var dialog = new Dialog(
                title,
                caption,
                QuestionId.Close,
                items,
                onResult: null,
                new DialogButton(QuestionId.Close, LocalizationService.Mark("Close")));

            return new ResultPlan(dialog, playWin: beatRecord);
        }
    }
}

