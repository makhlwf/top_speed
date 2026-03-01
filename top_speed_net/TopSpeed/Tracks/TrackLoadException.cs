using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Data;

namespace TopSpeed.Tracks
{
    internal sealed class TrackLoadException : Exception
    {
        public TrackLoadException(string trackReference, IReadOnlyList<string> details)
            : base($"Failed to load track '{trackReference}'.")
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
                ? "Track"
                : Path.GetFileName(trackReference);
            details.Add($"Track: {label}");

            if (issues != null)
            {
                for (var i = 0; i < issues.Count; i++)
                    details.Add(issues[i].ToString());
            }

            if (details.Count == 1)
                details.Add("Failed to load this track file.");

            return new TrackLoadException(trackReference, details);
        }
    }
}
