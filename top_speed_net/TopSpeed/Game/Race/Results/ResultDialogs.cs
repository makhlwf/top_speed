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

            var items = new List<DialogItem>();
            AppendTimeTrialRunSummary(items, summary);
            AppendTimeTrialLapSummary(items, summary);

            var dialog = new Dialog(
                title,
                caption,
                QuestionId.Close,
                items,
                onResult: null,
                new DialogButton(QuestionId.Close, LocalizationService.Mark("Close")));

            return new ResultPlan(dialog, playWin: beatRecord);
        }

        private void AppendTimeTrialRunSummary(List<DialogItem> items, RaceResultSummary summary)
        {
            items.Add(new DialogItem(LocalizationService.Format(
                _pick.One(ResultCatalog.TimeTrialCurrentLineTemplates),
                _fmt.Time(summary.TimeTrialCurrentRunMs))));

            if (summary.TimeTrialBestRunMs > 0)
            {
                items.Add(new DialogItem(LocalizationService.Format(
                    _pick.One(ResultCatalog.TimeTrialBestRunLineTemplates),
                    _fmt.Time(summary.TimeTrialBestRunMs))));
            }

            if (summary.TimeTrialAverageRunMs > 0 && summary.TimeTrialLapCount > 0)
            {
                items.Add(new DialogItem(LocalizationService.Format(
                    _pick.One(ResultCatalog.TimeTrialAverageRunLineTemplates),
                    summary.TimeTrialLapCount,
                    _fmt.Time(summary.TimeTrialAverageRunMs))));
            }
        }

        private void AppendTimeTrialLapSummary(List<DialogItem> items, RaceResultSummary summary)
        {
            var lapItems = new List<DialogItem>();
            if (summary.TimeTrialBestLapThisRunMs > 0)
            {
                lapItems.Add(new DialogItem(LocalizationService.Format(
                    _pick.One(ResultCatalog.TimeTrialRunBestLapLineTemplates),
                    _fmt.Time(summary.TimeTrialBestLapThisRunMs))));
            }

            if (summary.TimeTrialBestLapMs > 0)
            {
                lapItems.Add(new DialogItem(LocalizationService.Format(
                    _pick.One(ResultCatalog.TimeTrialBestLapLineTemplates),
                    _fmt.Time(summary.TimeTrialBestLapMs))));
            }

            if (summary.TimeTrialAverageLapMs > 0)
            {
                lapItems.Add(new DialogItem(LocalizationService.Format(
                    _pick.One(ResultCatalog.TimeTrialAverageLapLineTemplates),
                    _fmt.Time(summary.TimeTrialAverageLapMs))));
            }

            if (lapItems.Count == 0)
                return;

            items.Add(new DialogItem(_pick.One(ResultCatalog.TimeTrialLapSummaryTitles)));
            items.AddRange(lapItems);
        }
    }
}

