using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Data;
using TopSpeed.Localization;

namespace TopSpeed.Tracks
{
    internal sealed class TrackLoadException : Exception
    {
        public TrackLoadException(string trackReference, IReadOnlyList<string> details)
            : base(LocalizationService.Format(
                LocalizationService.Mark("Failed to load track '{0}'."),
                trackReference))
        {
            TrackReference = trackReference ?? string.Empty;
            Details = details ?? Array.Empty<string>();
        }

        public string TrackReference { get; }
        public IReadOnlyList<string> Details { get; }

        public static TrackLoadException FromIssues(string trackReference, IReadOnlyList<TrackTsmIssue> issues)
        {
            var details = new List<string>();
            var label = string.IsNullOrWhiteSpace(trackReference)
                ? LocalizationService.Translate(LocalizationService.Mark("Track"))
                : Path.GetFileName(trackReference);
            details.Add(LocalizationService.Format(
                LocalizationService.Mark("Track: {0}"),
                label));

            if (issues != null)
            {
                for (var i = 0; i < issues.Count; i++)
                    details.Add(issues[i].ToString());
            }

            if (details.Count == 1)
                details.Add(LocalizationService.Mark("Failed to load this track file."));

            return new TrackLoadException(trackReference, details);
        }
    }
}

