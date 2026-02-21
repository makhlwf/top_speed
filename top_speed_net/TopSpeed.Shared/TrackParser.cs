using System;
using System.Collections.Generic;
using System.IO;

namespace TopSpeed.Data
{
    public static partial class TrackTsmParser
    {
        private const int Types = 9;
        private const int Surfaces = 5;
        private const int Noises = 12;

        public static bool TryLoad(string nameOrPath, out TrackData data, float minPartLengthMeters = 50.0f)
        {
            return TryLoad(nameOrPath, out data, out _, minPartLengthMeters);
        }

        public static bool TryLoad(
            string nameOrPath,
            out TrackData data,
            out IReadOnlyList<TrackTsmIssue> issues,
            float minPartLengthMeters = 50.0f)
        {
            data = null!;
            issues = Array.Empty<TrackTsmIssue>();
            var resolvedPath = ResolveTrackPath(nameOrPath);
            if (resolvedPath == null)
            {
                issues = new[]
                {
                    new TrackTsmIssue(TrackTsmIssueSeverity.Error, 0, $"Track file not found: {nameOrPath}")
                };
                return false;
            }

            return TryLoadFromFile(resolvedPath, out data, out issues, minPartLengthMeters);
        }

        public static bool TryLoadFromFile(string filename, out TrackData data, float minPartLengthMeters = 50.0f)
        {
            return TryLoadFromFile(filename, out data, out _, minPartLengthMeters);
        }

        public static bool TryLoadFromFile(
            string filename,
            out TrackData data,
            out IReadOnlyList<TrackTsmIssue> issues,
            float minPartLengthMeters = 50.0f)
        {
            data = null!;
            var issueList = new List<TrackTsmIssue>();
            issues = issueList;
            if (!File.Exists(filename))
            {
                issueList.Add(new TrackTsmIssue(TrackTsmIssueSeverity.Error, 0, $"Track file not found: {filename}"));
                return false;
            }

            var fullPath = Path.GetFullPath(filename);
            if (!IsFolderTrackPath(fullPath))
            {
                issueList.Add(new TrackTsmIssue(
                    TrackTsmIssueSeverity.Error,
                    0,
                    $"Track path must point to a .tsm file inside a track folder: {filename}"));
                return false;
            }

            var minPart = Math.Max(50.0f, minPartLengthMeters);
            if (!ValidateFile(fullPath, minPart, issueList))
                return false;

            var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var segments = new List<TrackDefinition>();
            var rooms = new Dictionary<string, TrackRoomDefinition>(StringComparer.OrdinalIgnoreCase);
            var sounds = new Dictionary<string, TrackSoundSourceDefinition>(StringComparer.OrdinalIgnoreCase);

            var sectionKind = string.Empty;
            SegmentBuilder? pendingSegment = null;
            RoomBuilder? pendingRoom = null;
            SoundBuilder? pendingSound = null;

            foreach (var raw in File.ReadLines(fullPath))
            {
                var line = StripInlineComment(raw).Trim();
                if (line.Length == 0)
                    continue;

                if (TryParseSectionHeader(line, out var nextKind, out var nextId, out var impliedSoundType))
                {
                    FlushPending(ref pendingSegment, segments, minPart);
                    FlushPending(ref pendingRoom, rooms);
                    FlushPending(ref pendingSound, sounds);
                    sectionKind = nextKind;

                    if (sectionKind == "segment")
                        pendingSegment = SegmentBuilder.Create(nextId);
                    else if (sectionKind == "room")
                        pendingRoom = RoomBuilder.Create(nextId);
                    else if (sectionKind == "sound")
                    {
                        pendingSound = SoundBuilder.Create(nextId);
                        if (impliedSoundType.HasValue)
                        {
                            var builder = pendingSound.Value;
                            builder.Type = impliedSoundType.Value;
                            pendingSound = builder;
                        }
                    }

                    continue;
                }

                if (!TryParseKeyValue(line, out var rawKey, out var rawValue))
                    continue;

                var key = NormalizeIdentifier(rawKey);
                var value = rawValue.Trim();

                switch (sectionKind)
                {
                    case "meta":
                        meta[key] = value;
                        break;
                    case "segment":
                        if (pendingSegment.HasValue)
                        {
                            var builder = pendingSegment.Value;
                            ParseSegmentKey(ref builder, key, value, minPart);
                            pendingSegment = builder;
                        }
                        break;
                    case "room":
                        if (pendingRoom.HasValue)
                        {
                            var builder = pendingRoom.Value;
                            ParseRoomKey(ref builder, key, value);
                            pendingRoom = builder;
                        }
                        break;
                    case "sound":
                        if (pendingSound.HasValue)
                        {
                            var builder = pendingSound.Value;
                            ParseSoundKey(ref builder, key, value);
                            pendingSound = builder;
                        }
                        break;
                }
            }

            FlushPending(ref pendingSegment, segments, minPart);
            FlushPending(ref pendingRoom, rooms);
            FlushPending(ref pendingSound, sounds);

            if (segments.Count == 0)
                return false;

            var weather = ParseWeather(meta);
            var ambience = ParseAmbience(meta);
            meta.TryGetValue("name", out var name);
            meta.TryGetValue("version", out var version);

            data = new TrackData(
                userDefined: true,
                weather: weather,
                ambience: ambience,
                definitions: segments.ToArray(),
                name: name,
                version: version,
                metadata: meta,
                roomProfiles: rooms,
                soundSources: sounds,
                sourcePath: fullPath);

            return true;
        }
    }
}
