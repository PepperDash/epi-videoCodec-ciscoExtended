using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// FullStatusReplyGate: the sync's full xStatus reply is matched once, and a reply from a session that
/// has been reset doesn't count in the next one.
/// </summary>
public class FullStatusReplyGateTests
{
    private const string Kind = "sync.xstatus";

    private static (XapiRequestTracker Requests, FullStatusReplyGate Gate) New()
    {
        var requests = new XapiRequestTracker(TimeSpan.FromSeconds(120));
        return (requests, new FullStatusReplyGate(requests, Kind));
    }

    [Fact]
    public void A_Matched_Reply_Is_Arriving_And_Leaves_The_Tracker()
    {
        var (requests, gate) = New();
        var id = requests.Begin(Kind);

        gate.TryComplete(id).Should().BeTrue();

        gate.Arriving.Should().BeTrue();
        requests.PendingCount(Kind).Should().Be(0);
        gate.TryComplete(id).Should().BeFalse("a reply completes its request once");
    }

    [Fact]
    public void Other_Replies_Match_Nothing()
    {
        var (requests, gate) = New();
        var search = requests.Begin("phonebook.search");

        gate.TryComplete(search).Should().BeFalse("another kind of request");
        gate.TryComplete("unknown").Should().BeFalse();
        gate.TryComplete(null).Should().BeFalse("status feedback carries no resultId");

        gate.Arriving.Should().BeFalse();
        requests.PendingCount("phonebook.search").Should().Be(1, "left for its own handler");
    }

    [Fact]
    public void Reset_Cancels_The_Request_So_Its_Late_Reply_Matches_Nothing()
    {
        var (requests, gate) = New();
        var id = requests.Begin(Kind);

        gate.Reset().Should().Be(1);

        gate.TryComplete(id).Should().BeFalse();
        gate.Arriving.Should().BeFalse("the next session must ask for its own full status");
    }

    [Fact]
    public void Reset_After_A_Match_Clears_Arriving()
    {
        // Matched just before the reset, applied on the worker after it: the sync sees Arriving false
        // and ignores it.
        var (requests, gate) = New();
        gate.TryComplete(requests.Begin(Kind)).Should().BeTrue();

        gate.Reset().Should().Be(0);

        gate.Arriving.Should().BeFalse();
    }

    [Fact]
    public void A_Reply_In_The_New_Session_Counts()
    {
        var (requests, gate) = New();
        gate.TryComplete(requests.Begin(Kind));
        gate.Reset();

        gate.TryComplete(requests.Begin(Kind)).Should().BeTrue();

        gate.Arriving.Should().BeTrue();
    }

    [Fact]
    public void A_Reset_Waits_Until_A_Match_In_Progress_Has_Marked_The_Reply()
    {
        // The race Copilot found: a reset on the sync worker landing after the match found the request
        // but before it marked the reply left Arriving set for a cancelled request. The reset is started
        // at exactly that point; it must wait for the match, then clear what it set.
        var (requests, gate) = New();
        var id = requests.Begin(Kind);
        Task<int>? reset = null;
        var resetFinishedInsideTheMatch = false;
        gate.AfterMatchForTest = () =>
        {
            reset = Task.Run(() => gate.Reset());
            resetFinishedInsideTheMatch = reset.Wait(200);
        };

        gate.TryComplete(id).Should().BeTrue();

        resetFinishedInsideTheMatch.Should().BeFalse("the reset waits for the match to finish");
        reset!.Wait(5000).Should().BeTrue();
        gate.Arriving.Should().BeFalse("the reset came last");
        requests.PendingCount(Kind).Should().Be(0);
    }
}
