using System;
using System.Threading;
using System.Threading.Tasks;
using TS.Audio;

namespace TopSpeed.Audio
{
    internal sealed partial class AudioManager
    {
        public void PlayTriangleTone(double frequencyHz, int durationMs, float volume = 0.35f)
        {
            if (frequencyHz <= 0d || durationMs <= 0)
                return;

            var sampleRate = _engine.PrimaryOutput.SampleRate > 0 ? _engine.PrimaryOutput.SampleRate : 44100;
            var samplesPerCycle = Math.Max(1, (int)Math.Round(sampleRate / frequencyHz));
            var totalFrames = (int)((sampleRate * durationMs) / 1000.0);
            if (totalFrames <= 0)
                return;
            var remainder = totalFrames % samplesPerCycle;
            if (remainder != 0)
                totalFrames += samplesPerCycle - remainder;

            var frameCursor = 0;
            Source? source = null;
            source = CreateProceduralSource(
                (float[] buffer, int frames, int channels, ref ulong frameIndex) =>
                {
                    for (var i = 0; i < frames; i++)
                    {
                        float sample = 0f;
                        if (frameCursor < totalFrames)
                        {
                            var phase = (double)(frameCursor % samplesPerCycle) / samplesPerCycle;
                            double triangle;
                            if (phase < 0.25d)
                            {
                                triangle = phase * 4.0d;
                            }
                            else if (phase < 0.75d)
                            {
                                triangle = 2.0d - (phase * 4.0d);
                            }
                            else
                            {
                                triangle = (phase * 4.0d) - 4.0d;
                            }

                            sample = (float)(triangle * 0.65d);
                            frameCursor++;
                        }

                        for (var c = 0; c < channels; c++)
                            buffer[(i * channels) + c] = sample;
                    }
                },
                channels: 1,
                sampleRate: (uint)sampleRate,
                busName: AudioEngineOptions.UiBusName,
                spatialize: false,
                useHrtf: false);

            source.SetVolume(volume);
            source.Play(loop: false);
            Task.Run(() =>
            {
                try
                {
                    var alignedDurationMs = (int)Math.Ceiling((totalFrames * 1000.0d) / sampleRate);
                    Thread.Sleep(alignedDurationMs + 30);
                    source.Stop();
                    source.Dispose();
                }
                catch
                {
                    // Ignore tone cleanup errors.
                }
            });
        }
    }
}

