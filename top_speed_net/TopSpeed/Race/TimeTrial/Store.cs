using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TopSpeed.Core;

namespace TopSpeed.Race.TimeTrial
{
    internal sealed class Store
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("TSTS");
        private const int Version = 1;
        private const string FileName = "time_trial.dat";
        private readonly string _path;

        public Store(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public static Store CreateDefault()
        {
            return new Store(AppData.File(FileName));
        }

        public StatsSnapshot Read(string trackId, int laps)
        {
            if (string.IsNullOrWhiteSpace(trackId) || laps <= 0)
                return new StatsSnapshot();

            var file = Load();
            return BuildSnapshot(file, trackId, laps);
        }

        public StatsSnapshot RecordRun(string trackId, string displayName, int laps, int runTimeMs, int[] lapTimesMs)
        {
            if (string.IsNullOrWhiteSpace(trackId))
                throw new ArgumentException("Track id is required.", nameof(trackId));
            if (laps <= 0)
                throw new ArgumentOutOfRangeException(nameof(laps));

            var file = Load();
            if (!file.Tracks.TryGetValue(trackId, out var track))
            {
                track = new TrackStats();
                file.Tracks[trackId] = track;
            }

            if (!string.IsNullOrWhiteSpace(displayName))
                track.DisplayName = displayName;

            if (!track.Runs.TryGetValue(laps, out var runs))
            {
                runs = new SampleStats();
                track.Runs[laps] = runs;
            }

            runs.Add(runTimeMs);

            if (lapTimesMs != null)
            {
                for (var i = 0; i < lapTimesMs.Length; i++)
                    track.Laps.Add(lapTimesMs[i]);
            }

            Save(file);
            return BuildSnapshot(file, trackId, laps);
        }

        private StatsSnapshot BuildSnapshot(StatsFile file, string trackId, int laps)
        {
            if (!file.Tracks.TryGetValue(trackId, out var track))
                return new StatsSnapshot();

            var snapshot = new StatsSnapshot
            {
                LapBestMs = track.Laps.BestMs,
                LapAverageMs = track.Laps.AverageMs,
                LapCount = track.Laps.Count
            };

            if (track.Runs.TryGetValue(laps, out var runs))
            {
                snapshot.RunBestMs = runs.BestMs;
                snapshot.RunAverageMs = runs.AverageMs;
                snapshot.RunCount = runs.Count;
            }

            return snapshot;
        }

        private StatsFile Load()
        {
            if (!File.Exists(_path))
                return new StatsFile();

            try
            {
                using (var stream = File.OpenRead(_path))
                using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false))
                {
                    var magic = reader.ReadBytes(Magic.Length);
                    if (!MatchesMagic(magic))
                        throw new InvalidDataException("Invalid time trial stats header.");

                    var version = reader.ReadInt32();
                    if (version != Version)
                        throw new InvalidDataException("Unsupported time trial stats version.");

                    var payloadLength = reader.ReadInt32();
                    if (payloadLength < 0)
                        throw new InvalidDataException("Invalid time trial stats payload length.");

                    var checksum = reader.ReadBytes(32);
                    var payload = reader.ReadBytes(payloadLength);
                    if (payload.Length != payloadLength)
                        throw new EndOfStreamException("Incomplete time trial stats payload.");

                    ValidateChecksum(payload, checksum);
                    return ReadPayload(payload);
                }
            }
            catch (InvalidDataException)
            {
                QuarantineBrokenFile();
                return new StatsFile();
            }
            catch (EndOfStreamException)
            {
                QuarantineBrokenFile();
                return new StatsFile();
            }
            catch (CryptographicException)
            {
                QuarantineBrokenFile();
                return new StatsFile();
            }
            catch (IOException)
            {
                QuarantineBrokenFile();
                return new StatsFile();
            }
            catch (UnauthorizedAccessException)
            {
                return new StatsFile();
            }
        }

        private void Save(StatsFile file)
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var payload = WritePayload(file);
            using (var sha = SHA256.Create())
            using (var stream = File.Create(_path))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false))
            {
                writer.Write(Magic);
                writer.Write(Version);
                writer.Write(payload.Length);
                writer.Write(sha.ComputeHash(payload));
                writer.Write(payload);
            }
        }

        private static StatsFile ReadPayload(byte[] payload)
        {
            var file = new StatsFile();
            using (var stream = new MemoryStream(payload, writable: false))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false))
            {
                var trackCount = reader.ReadInt32();
                for (var i = 0; i < trackCount; i++)
                {
                    var trackId = reader.ReadString();
                    var track = new TrackStats
                    {
                        DisplayName = reader.ReadString()
                    };

                    ReadSample(reader, track.Laps);

                    var runCount = reader.ReadInt32();
                    for (var j = 0; j < runCount; j++)
                    {
                        var lapCount = reader.ReadInt32();
                        var sample = new SampleStats();
                        ReadSample(reader, sample);
                        track.Runs[lapCount] = sample;
                    }

                    file.Tracks[trackId] = track;
                }
            }

            return file;
        }

        private static byte[] WritePayload(StatsFile file)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(file.Tracks.Count);
                foreach (var pair in file.Tracks)
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value.DisplayName ?? string.Empty);
                    WriteSample(writer, pair.Value.Laps);
                    writer.Write(pair.Value.Runs.Count);
                    foreach (var run in pair.Value.Runs)
                    {
                        writer.Write(run.Key);
                        WriteSample(writer, run.Value);
                    }
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        private static void ReadSample(BinaryReader reader, SampleStats sample)
        {
            sample.BestMs = reader.ReadInt32();
            sample.TotalMs = reader.ReadInt64();
            sample.Count = reader.ReadInt32();
        }

        private static void WriteSample(BinaryWriter writer, SampleStats sample)
        {
            writer.Write(sample.BestMs);
            writer.Write(sample.TotalMs);
            writer.Write(sample.Count);
        }

        private static bool MatchesMagic(byte[] magic)
        {
            if (magic.Length != Magic.Length)
                return false;

            for (var i = 0; i < magic.Length; i++)
            {
                if (magic[i] != Magic[i])
                    return false;
            }

            return true;
        }

        private static void ValidateChecksum(byte[] payload, byte[] checksum)
        {
            if (checksum.Length != 32)
                throw new InvalidDataException("Invalid time trial stats checksum.");

            using (var sha = SHA256.Create())
            {
                var expected = sha.ComputeHash(payload);
                for (var i = 0; i < expected.Length; i++)
                {
                    if (expected[i] != checksum[i])
                        throw new CryptographicException("Time trial stats checksum mismatch.");
                }
            }
        }

        private void QuarantineBrokenFile()
        {
            if (!File.Exists(_path))
                return;

            var brokenPath = $"{_path}.{DateTime.UtcNow:yyyyMMddHHmmssfff}.broken";
            try
            {
                File.Move(_path, brokenPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
