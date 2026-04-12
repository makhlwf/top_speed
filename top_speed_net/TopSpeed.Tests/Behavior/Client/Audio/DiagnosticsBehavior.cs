using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using FluentAssertions;
using TS.Audio;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class DiagnosticsBehaviorTests
{
    [Fact]
    public void Emit_Stores_Only_Events_That_Match_Global_Filter()
    {
        var diagnostics = new AudioDiagnostics();
        diagnostics.Configure(new AudioDiagnosticConfig
        {
            Enabled = true,
            HistoryCapacity = 8,
            Filter = new AudioDiagnosticFilter
            {
                MinimumLevel = AudioDiagnosticLevel.Warn
            }
        });

        diagnostics.Emit(AudioDiagnosticLevel.Info, AudioDiagnosticKind.SourceCreated, AudioDiagnosticEntityType.Source, "main", "ui", 1, "ignored");
        diagnostics.Emit(AudioDiagnosticLevel.Warn, AudioDiagnosticKind.AnomalyClippingRisk, AudioDiagnosticEntityType.Output, "main", null, null, "stored");

        diagnostics.GetHistory().Should().ContainSingle();
        diagnostics.GetHistory()[0].Kind.Should().Be(AudioDiagnosticKind.AnomalyClippingRisk);
    }

    [Fact]
    public void Subscribe_Uses_Subscription_Filter()
    {
        var diagnostics = new AudioDiagnostics();
        diagnostics.Configure(new AudioDiagnosticConfig
        {
            Enabled = true,
            HistoryCapacity = 8
        });

        AudioDiagnosticEvent? captured = null;
        using var gate = new ManualResetEventSlim();
        using var subscription = diagnostics.Subscribe(
            diagnosticEvent =>
            {
                captured = diagnosticEvent;
                gate.Set();
            },
            new AudioDiagnosticFilter
            {
                MinimumLevel = AudioDiagnosticLevel.Trace,
                Kinds = { AudioDiagnosticKind.SourceStarted },
                OutputNames = { "main" },
                SourceIds = { 7 }
            });

        diagnostics.Emit(AudioDiagnosticLevel.Debug, AudioDiagnosticKind.SourceCreated, AudioDiagnosticEntityType.Source, "main", "ui", 7, "wrong kind");
        diagnostics.Emit(AudioDiagnosticLevel.Debug, AudioDiagnosticKind.SourceStarted, AudioDiagnosticEntityType.Source, "other", "ui", 7, "wrong output");
        diagnostics.Emit(AudioDiagnosticLevel.Debug, AudioDiagnosticKind.SourceStarted, AudioDiagnosticEntityType.Source, "main", "ui", 7, "matched");

        gate.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Message.Should().Be("matched");
    }

    [Fact]
    public void History_Uses_Bounded_Capacity()
    {
        var diagnostics = new AudioDiagnostics();
        diagnostics.Configure(new AudioDiagnosticConfig
        {
            Enabled = true,
            HistoryCapacity = 2
        });

        diagnostics.Emit(AudioDiagnosticLevel.Info, AudioDiagnosticKind.OutputCreated, AudioDiagnosticEntityType.Output, "main", null, null, "first");
        diagnostics.Emit(AudioDiagnosticLevel.Info, AudioDiagnosticKind.BusCreated, AudioDiagnosticEntityType.Bus, "main", "ui", null, "second");
        diagnostics.Emit(AudioDiagnosticLevel.Info, AudioDiagnosticKind.SourceCreated, AudioDiagnosticEntityType.Source, "main", "ui", 1, "third");

        diagnostics.GetHistory().Should().HaveCount(2);
        diagnostics.GetHistory()[0].Message.Should().Be("second");
        diagnostics.GetHistory()[1].Message.Should().Be("third");
    }

    [Fact]
    public void JsonlSink_Writes_Structured_Event_With_Snapshot()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "topspeed-audio-diagnostics-" + Guid.NewGuid().ToString("N") + ".jsonl");
        try
        {
            var diagnostics = new AudioDiagnostics();
            diagnostics.Configure(new AudioDiagnosticConfig
            {
                Enabled = true,
                HistoryCapacity = 8
            });
            using (var sink = new AudioDiagnosticJsonlSink(path))
            {
                diagnostics.AddSink(sink);

                diagnostics.Emit(
                    AudioDiagnosticLevel.Warn,
                    AudioDiagnosticKind.AnomalySilentStart,
                    AudioDiagnosticEntityType.Source,
                    "main",
                    "vehicles",
                    42,
                    "Audio source did not report playback shortly after start.",
                    new Dictionary<string, object?>
                    {
                        ["startDelayMs"] = 250.0
                    },
                    new AudioDiagnosticSnapshot(
                        output: new AudioOutputSnapshot("main", 48000, 2, 1f, 0f, 1.2f, 1.58f, 0.98f, -0.17f, false, 1, 0, 0, 0, Array.Empty<AudioBusSnapshot>(), Array.Empty<AudioSourceSnapshot>()),
                        source: new AudioSourceSnapshot(42, "vehicles", false, true, false, 2, 48000, true, 1f, 0f, 1f, 0f, 0.8f, -1.94f, 0.8f, -1.94f, Array.Empty<AudioGainStageSnapshot>(), 1.25f),
                        mix: new AudioDiagnosticMixSnapshot("main", 1f, 0f, 1.2f, 1.58f, 0.98f, -0.17f, 0.9f, -0.92f, new[]
                        {
                            new AudioSourceSnapshot(42, "vehicles", false, true, false, 2, 48000, true, 1f, 0f, 1f, 0f, 0.8f, -1.94f, 0.8f, -1.94f, Array.Empty<AudioGainStageSnapshot>(), 1.25f)
                        })));

                Thread.Sleep(250);
            }

            var text = File.ReadAllText(path);
            text.Should().Contain("\"kind\":\"AnomalySilentStart\"");
            text.Should().Contain("\"sourceId\":42");
            text.Should().Contain("\"busName\":\"vehicles\"");
            text.Should().Contain("\"startDelayMs\":250");
            text.Should().Contain("\"estimatedMixVolumeDb\":-1.94");
            text.Should().Contain("\"preLimiterPeakDbfs\":1.58");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void GetHistory_Can_Filter_By_Kind_And_Source()
    {
        var diagnostics = new AudioDiagnostics();
        diagnostics.Configure(new AudioDiagnosticConfig
        {
            Enabled = true,
            HistoryCapacity = 8
        });

        diagnostics.Emit(AudioDiagnosticLevel.Debug, AudioDiagnosticKind.SourceVolumeChanged, AudioDiagnosticEntityType.Source, "main", "ui", 1, "source 1");
        diagnostics.Emit(AudioDiagnosticLevel.Debug, AudioDiagnosticKind.SourceVolumeChanged, AudioDiagnosticEntityType.Source, "main", "ui", 2, "source 2");
        diagnostics.Emit(AudioDiagnosticLevel.Debug, AudioDiagnosticKind.SourcePitchChanged, AudioDiagnosticEntityType.Source, "main", "ui", 1, "other kind");

        var filtered = diagnostics.GetHistory(new AudioDiagnosticFilter
        {
            Kinds = { AudioDiagnosticKind.SourceVolumeChanged },
            SourceIds = { 2 }
        });

        filtered.Should().ContainSingle();
        filtered[0].Message.Should().Be("source 2");
    }
}
