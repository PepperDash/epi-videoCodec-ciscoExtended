using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// The camera manager selects a codec's primary camera when a call starts: only on the change from
/// no call to a call, never for a call already up when it starts watching.
/// </summary>
public class CallStartDetectorTests
{
    [Fact]
    public void A_Call_Starting_Is_Reported_Once()
    {
        var d = new CallStartDetector();
        d.Observe("codecA", false).Should().BeFalse("the first reading only records the state");
        d.Observe("codecA", true).Should().BeTrue();
        d.Observe("codecA", true).Should().BeFalse("still the same call");
        d.Observe("codecA", false).Should().BeFalse();
        d.Observe("codecA", true).Should().BeTrue("the next call");
    }

    [Fact]
    public void A_Call_Already_Up_At_The_First_Reading_Is_Left_Alone()
    {
        // Program restart in a meeting: changing the camera now would surprise the room.
        var d = new CallStartDetector();
        d.Observe("codecA", true).Should().BeFalse();
        d.Observe("codecA", true).Should().BeFalse();
    }

    [Fact]
    public void A_Forgotten_Codec_Starts_Over()
    {
        // The codec dropped off and came back with the call still up: not a new call.
        var d = new CallStartDetector();
        d.Observe("codecB", false);
        d.Forget("codecB");
        d.Observe("codecB", true).Should().BeFalse();
    }

    [Fact]
    public void Codecs_Are_Tracked_Separately()
    {
        var d = new CallStartDetector();
        d.Observe("codecA", false);
        d.Observe("codecB", true);
        d.Observe("codecA", true).Should().BeTrue();
        d.Observe("codecB", true).Should().BeFalse("codec B's call was already up");
    }

    [Fact]
    public void Keys_Are_Case_Insensitive()
    {
        // A first reading under one spelling counts for the other: otherwise "CODECC" would be a new,
        // unknown codec and the call start would be missed.
        var d = new CallStartDetector();
        d.Observe("codecC", false);
        d.Observe("CODECC", true).Should().BeTrue();
    }

    [Fact]
    public void An_Empty_Key_Is_Ignored()
    {
        var d = new CallStartDetector();
        d.Observe("", false).Should().BeFalse();
        d.Observe("", true).Should().BeFalse();
    }
}
