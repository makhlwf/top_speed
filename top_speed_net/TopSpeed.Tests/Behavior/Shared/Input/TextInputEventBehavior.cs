using System;
using System.Runtime.InteropServices;
using System.Text;
using TS.Sdl.Events;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class TextInputEventBehaviorTests
{
    [Fact]
    public void TextInputEvent_Text_ReadsUtf8Payload()
    {
        var textPointer = AllocUtf8("مرحبا");
        try
        {
            var value = new TextInputEvent
            {
                Type = EventType.TextInput,
                Timestamp = 1,
                WindowId = 7,
                TextPointer = textPointer
            };

            value.Text.Should().Be("مرحبا");
        }
        finally
        {
            Marshal.FreeHGlobal(textPointer);
        }
    }

    [Fact]
    public void TextEditingEvent_Text_ReadsUtf8Payload()
    {
        var textPointer = AllocUtf8("compose");
        try
        {
            var value = new TextEditingEvent
            {
                Type = EventType.TextEditing,
                Timestamp = 2,
                WindowId = 8,
                TextPointer = textPointer,
                Start = 1,
                Length = 3
            };

            value.Text.Should().Be("compose");
            value.Start.Should().Be(1);
            value.Length.Should().Be(3);
        }
        finally
        {
            Marshal.FreeHGlobal(textPointer);
        }
    }

    [Fact]
    public void TextEditingCandidatesEvent_GetCandidates_ReadsAllCandidates()
    {
        var candidate1 = AllocUtf8("alpha");
        var candidate2 = AllocUtf8("beta");
        var array = Marshal.AllocHGlobal(IntPtr.Size * 2);

        try
        {
            Marshal.WriteIntPtr(array, 0, candidate1);
            Marshal.WriteIntPtr(array, IntPtr.Size, candidate2);

            var value = new TextEditingCandidatesEvent
            {
                Type = EventType.TextEditingCandidates,
                Timestamp = 3,
                WindowId = 9,
                CandidatesPointer = array,
                CandidateCount = 2,
                SelectedCandidate = 1,
                Horizontal = true
            };

            var candidates = value.GetCandidates();
            candidates.Should().Equal("alpha", "beta");
            value.SelectedCandidate.Should().Be(1);
            value.Horizontal.Should().BeTrue();
        }
        finally
        {
            Marshal.FreeHGlobal(array);
            Marshal.FreeHGlobal(candidate1);
            Marshal.FreeHGlobal(candidate2);
        }
    }

    private static IntPtr AllocUtf8(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var pointer = Marshal.AllocHGlobal(bytes.Length + 1);
        Marshal.Copy(bytes, 0, pointer, bytes.Length);
        Marshal.WriteByte(pointer, bytes.Length, 0);
        return pointer;
    }
}
