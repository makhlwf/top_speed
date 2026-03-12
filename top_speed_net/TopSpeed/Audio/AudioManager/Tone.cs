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

            var sampleRate = _output.SampleRate > 0 ? _output.SampleRate : 44100;
            var totalFrames = (int)((sampleRate * durationMs) / 1000.0);
            if (totalFrames <= 0)
                return;

            var frameCursor = 0;
            AudioSourceHandle? source = null;
            source = _output.CreateProceduralSource(
                (float[] buffer, int frames, int channels, ref ulong frameIndex) =>
                {
                    for (var i = 0; i < frames; i++)
                    {
                        float sample = 0f;
                        if (frameCursor < totalFrames)
                        {
                            var t = (double)frameCursor / sampleRate;
                            var cycle = (t * frequencyHz) % 1.0d;
                            var tri = 1.0d - (4.0d * Math.Abs(cycle - 0.5d));
                            sample = (float)(tri * 0.65d);
                            frameCursor++;
                        }

                        for (var c = 0; c < channels; c++)
                            buffer[(i * channels) + c] = sample;
                    }
                },
                channels: 1,
                sampleRate: (uint)sampleRate,
                useHrtf: false);

            source.SetVolume(volume);
            source.Play(loop: false);
            Task.Run(() =>
            {
                try
                {
                    Thread.Sleep(durationMs + 30);
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
