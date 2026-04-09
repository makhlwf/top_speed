using TopSpeed.Localization;

namespace TopSpeed.Game
{
    internal sealed class ResultCatalog
    {
        public static readonly string[] WinnerTitles =
        {
            LocalizationService.Mark("Congratulations! You have made it to the first position."),
            LocalizationService.Mark("Outstanding drive. You secured first position."),
            LocalizationService.Mark("First position achieved. Great race.")
        };

        public static readonly string[] NonWinnerTitles =
        {
            LocalizationService.Mark("Race complete."),
            LocalizationService.Mark("The race has finished."),
            LocalizationService.Mark("Final results are ready.")
        };

        public static readonly string[] WinnerCaptions =
        {
            LocalizationService.Mark("The following are the details of all players."),
            LocalizationService.Mark("Here are the final standings for everyone.")
        };

        public static readonly string[] NonWinnerCaptions =
        {
            LocalizationService.Mark("Here are the final standings."),
            LocalizationService.Mark("The following are the race details for all players.")
        };

        public static readonly string[] TimeTrialRecordTitles =
        {
            LocalizationService.Mark("Outstanding run! New personal record."),
            LocalizationService.Mark("Excellent driving. You beat your previous best time."),
            LocalizationService.Mark("Brilliant result! You set a new best time.")
        };

        public static readonly string[] TimeTrialNoRecordTitles =
        {
            LocalizationService.Mark("Time trial complete."),
            LocalizationService.Mark("Run finished. Previous best remains unbeaten."),
            LocalizationService.Mark("Run complete. Better luck on the next attempt.")
        };

        public static readonly string[] TimeTrialRecordCaptions =
        {
            LocalizationService.Mark("Your new result details:"),
            LocalizationService.Mark("Summary of your latest time trial run:")
        };

        public static readonly string[] TimeTrialNoRecordCaptions =
        {
            LocalizationService.Mark("Summary of this run and your previous best:"),
            LocalizationService.Mark("Your latest run did not beat the best record. Details:")
        };

        public static readonly string[] FirstPlaceLineTemplates =
        {
            LocalizationService.Mark("{0}: position {1}, time {2}."),
            LocalizationService.Mark("{0}: finished in position {1} with {2}.")
        };

        public static readonly string[] PodiumLineTemplates =
        {
            LocalizationService.Mark("{0}: position {1}, time {2}."),
            LocalizationService.Mark("{0}: crossed in position {1} after {2}.")
        };

        public static readonly string[] FieldLineTemplates =
        {
            LocalizationService.Mark("{0}: position {1}, time {2}."),
            LocalizationService.Mark("{0}: completed in position {1} with a time of {2}.")
        };

        public static readonly string[] TimeTrialCurrentLineTemplates =
        {
            LocalizationService.Mark("Your time: {0}."),
            LocalizationService.Mark("You finished in {0}.")
        };

        public static readonly string[] TimeTrialBestRunLineTemplates =
        {
            LocalizationService.Mark("Best time so far: {0}."),
            LocalizationService.Mark("Best recorded run: {0}.")
        };

        public static readonly string[] TimeTrialAverageRunLineTemplates =
        {
            LocalizationService.Mark("Average {0}-lap time for this track: {1}."),
            LocalizationService.Mark("Average time for this track over {0} laps: {1}.")
        };

        public static readonly string[] TimeTrialLapSummaryTitles =
        {
            LocalizationService.Mark("Lap summary:"),
            LocalizationService.Mark("Lap statistics:")
        };

        public static readonly string[] TimeTrialRunBestLapLineTemplates =
        {
            LocalizationService.Mark("Best lap this run: {0}."),
            LocalizationService.Mark("Your quickest lap this run: {0}.")
        };

        public static readonly string[] TimeTrialBestLapLineTemplates =
        {
            LocalizationService.Mark("Best lap for this track: {0}."),
            LocalizationService.Mark("Fastest recorded lap on this track: {0}.")
        };

        public static readonly string[] TimeTrialAverageLapLineTemplates =
        {
            LocalizationService.Mark("Average lap time for this track: {0}."),
            LocalizationService.Mark("Track lap average: {0}.")
        };
    }
}

