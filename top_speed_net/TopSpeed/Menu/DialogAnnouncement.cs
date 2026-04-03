using System;
using System.Globalization;
using TopSpeed.Localization;

namespace TopSpeed.Menu
{
    internal static class DialogAnnouncement
    {
        private static readonly string TitleTemplate = LocalizationService.Mark("{0}  dialog");
        private static readonly string TitleCaptionTemplate = LocalizationService.Mark("{0}  dialog  {1}");

        public static string Compose(string? title, string? caption)
        {
            var titleText = title ?? string.Empty;
            var captionText = caption ?? string.Empty;
            var hasCaption = !string.IsNullOrWhiteSpace(captionText);
            var announcement = hasCaption
                ? LocalizationService.Format(TitleCaptionTemplate, titleText, captionText)
                : LocalizationService.Format(TitleTemplate, titleText);

            if (!NeedsFallback(announcement, titleText, captionText, hasCaption))
                return announcement;

            return hasCaption
                ? string.Format(CultureInfo.CurrentCulture, TitleCaptionTemplate, titleText, captionText)
                : string.Format(CultureInfo.CurrentCulture, TitleTemplate, titleText);
        }

        private static bool NeedsFallback(string announcement, string title, string caption, bool hasCaption)
        {
            if (string.IsNullOrWhiteSpace(announcement))
                return true;

            if (string.Equals(announcement, title, StringComparison.Ordinal))
                return true;

            if (announcement.IndexOf(title, StringComparison.Ordinal) < 0)
                return true;

            if (hasCaption && announcement.IndexOf(caption, StringComparison.Ordinal) < 0)
                return true;

            return false;
        }
    }
}

