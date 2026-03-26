using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Race;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private static readonly string[] WinnerTitles =
        {
            LocalizationService.Mark("Congratulations! You have made it to the first position."),
            LocalizationService.Mark("Outstanding drive. You secured first position."),
            LocalizationService.Mark("First position achieved. Great race.")
        };

        private static readonly string[] NonWinnerTitles =
        {
            LocalizationService.Mark("Race complete."),
            LocalizationService.Mark("The race has finished."),
            LocalizationService.Mark("Final results are ready.")
        };

        private static readonly string[] WinnerCaptions =
        {
            LocalizationService.Mark("The following are the details of all players."),
            LocalizationService.Mark("Here are the final standings for everyone.")
        };

        private static readonly string[] NonWinnerCaptions =
        {
            LocalizationService.Mark("Here are the final standings."),
            LocalizationService.Mark("The following are the race details for all players.")
        };

        private static readonly string[] TimeTrialRecordTitles =
        {
            LocalizationService.Mark("Outstanding run! New personal record."),
            LocalizationService.Mark("Excellent driving. You beat your previous best time."),
            LocalizationService.Mark("Brilliant result! You set a new best time.")
        };

        private static readonly string[] TimeTrialNoRecordTitles =
        {
            LocalizationService.Mark("Time trial complete."),
            LocalizationService.Mark("Run finished. Previous best remains unbeaten."),
            LocalizationService.Mark("Run complete. Better luck on the next attempt.")
        };

        private static readonly string[] TimeTrialRecordCaptions =
        {
            LocalizationService.Mark("Your new result details:"),
            LocalizationService.Mark("Summary of your latest time trial run:")
        };

        private static readonly string[] TimeTrialNoRecordCaptions =
        {
            LocalizationService.Mark("Summary of this run and your previous best:"),
            LocalizationService.Mark("Your latest run did not beat the best record. Details:")
        };

        private static readonly string[] FirstPlaceLineTemplates =
        {
            LocalizationService.Mark("{0}: position {1}, time {2}."),
            LocalizationService.Mark("{0}: finished in position {1} with {2}.")
        };

        private static readonly string[] PodiumLineTemplates =
        {
            LocalizationService.Mark("{0}: position {1}, time {2}."),
            LocalizationService.Mark("{0}: crossed in position {1} after {2}.")
        };

        private static readonly string[] FieldLineTemplates =
        {
            LocalizationService.Mark("{0}: position {1}, time {2}."),
            LocalizationService.Mark("{0}: completed in position {1} with a time of {2}.")
        };

        private static readonly string[] TimeTrialCurrentLineTemplates =
        {
            LocalizationService.Mark("Your time: {0}."),
            LocalizationService.Mark("You finished in {0}.")
        };

        private static readonly string[] TimeTrialPreviousBestLineTemplates =
        {
            LocalizationService.Mark("Your previous best record was: {0}."),
            LocalizationService.Mark("Previous best time: {0}.")
        };

        private void ShowRaceResultDialog(RaceResultSummary summary)
        {
            if (summary == null)
                return;

            if (summary.Mode == RaceResultMode.TimeTrial)
            {
                ShowTimeTrialResultDialog(summary);
                return;
            }

            var localWon = summary.LocalPosition == 1;
            var title = Pick(localWon ? WinnerTitles : NonWinnerTitles);
            var caption = Pick(localWon ? WinnerCaptions : NonWinnerCaptions);

            var items = new List<DialogItem>();
            var entries = summary.Entries ?? Array.Empty<RaceResultEntry>();
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null)
                    continue;
                items.Add(new DialogItem(FormatResultLine(entry)));
            }

            var dialog = new Dialog(
                title,
                caption,
                QuestionId.Close,
                items,
                onResult: null,
                new DialogButton(QuestionId.Close, LocalizationService.Mark("Close")));
            _dialogs.Show(dialog);

            if (localWon)
                PlayRaceWinSound();
        }

        private void ShowTimeTrialResultDialog(RaceResultSummary summary)
        {
            var beatRecord = summary.TimeTrialBeatRecord;
            var title = Pick(beatRecord ? TimeTrialRecordTitles : TimeTrialNoRecordTitles);
            var caption = Pick(beatRecord ? TimeTrialRecordCaptions : TimeTrialNoRecordCaptions);

            var items = new List<DialogItem>
            {
                new DialogItem(LocalizationService.Format(
                    Pick(TimeTrialCurrentLineTemplates),
                    FormatRaceTime(summary.TimeTrialCurrentTimeMs)))
            };

            if (!beatRecord && summary.TimeTrialPreviousBestTimeMs > 0)
            {
                items.Add(new DialogItem(LocalizationService.Format(
                    Pick(TimeTrialPreviousBestLineTemplates),
                    FormatRaceTime(summary.TimeTrialPreviousBestTimeMs))));
            }

            var dialog = new Dialog(
                title,
                caption,
                QuestionId.Close,
                items,
                onResult: null,
                new DialogButton(QuestionId.Close, LocalizationService.Mark("Close")));
            _dialogs.Show(dialog);

            if (beatRecord)
                PlayRaceWinSound();
        }

        private string FormatResultLine(RaceResultEntry entry)
        {
            var template = PickForPosition(entry.Position);
            var playerName = string.IsNullOrWhiteSpace(entry.Name)
                ? LocalizationService.Mark("Player")
                : entry.Name.Trim();
            return LocalizationService.Format(
                template,
                playerName,
                entry.Position,
                FormatRaceTime(entry.TimeMs));
        }

        private static string FormatRaceTime(int timeMs)
        {
            var clamped = Math.Max(0, timeMs);
            var minutes = clamped / 60000;
            var seconds = (clamped % 60000) / 1000;
            var minuteText = LocalizationService.Format(
                minutes == 1
                    ? LocalizationService.Mark("{0} minute")
                    : LocalizationService.Mark("{0} minutes"),
                minutes.ToString(CultureInfo.InvariantCulture));
            var secondText = LocalizationService.Format(
                seconds == 1
                    ? LocalizationService.Mark("{0} second")
                    : LocalizationService.Mark("{0} seconds"),
                seconds.ToString(CultureInfo.InvariantCulture));

            if (minutes > 0)
            {
                return LocalizationService.Format(
                    LocalizationService.Mark("{0} and {1}"),
                    minuteText,
                    secondText);
            }

            return secondText;
        }

        private static string PickForPosition(int position)
        {
            if (position <= 1)
                return Pick(FirstPlaceLineTemplates);
            if (position <= 3)
                return Pick(PodiumLineTemplates);
            return Pick(FieldLineTemplates);
        }

        private static string Pick(string[] options)
        {
            if (options == null || options.Length == 0)
                return string.Empty;
            if (options.Length == 1)
                return options[0];
            var index = Algorithm.RandomInt(options.Length);
            return options[index];
        }

        private void PlayRaceWinSound()
        {
            if (_raceWinSound == null)
            {
                if (!TryLoadRaceWinSound(out var handle))
                    return;
                _raceWinSound = handle;
            }

            try
            {
                var handle = _raceWinSound;
                if (handle == null)
                    return;
                handle.SetVolumePercent(_settings, AudioVolumeCategory.OnlineServerEvents, 100);
                handle.Restart(loop: false);
            }
            catch
            {
            }
        }

        private bool TryLoadRaceWinSound(out TS.Audio.AudioSourceHandle? handle)
        {
            handle = null;
            var audio = _audio as AudioManager;
            if (audio == null)
                return false;

            var path = Path.Combine(AssetPaths.SoundsRoot, "network", "win.ogg");
            if (!audio.TryResolvePath(path, out var fullPath))
                return false;

            try
            {
                handle = audio.AcquireCachedSource(fullPath, streamFromDisk: false);
                return handle != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
