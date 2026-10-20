using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>XapiRequestTracker (#129, stage 4): replies are matched to their own request by resultId.</summary>
public class XapiRequestTrackerTests
{
    private static XapiRequestTracker New(Func<DateTime>? clock = null) => new(TimeSpan.FromSeconds(20), clock);

    [Fact]
    public void A_Reply_Completes_Its_Own_Request_Once()
    {
        var t = New();
        var id = t.Begin("phonebook.search");

        t.TryComplete(id, out var kind).Should().BeTrue();
        kind.Should().Be("phonebook.search");
        t.TryComplete(id, out _).Should().BeFalse("it was already completed");
    }

    [Fact]
    public void Replies_Without_A_ResultId_Complete_Nothing_And_Drop_Nothing()
    {
        // #109: an untagged start-up reply used to dequeue and drop a pending user search.
        var t = New();
        var search = t.Begin("phonebook.search");

        t.TryComplete(null, out _).Should().BeFalse();
        t.TryComplete("", out _).Should().BeFalse();
        t.TryComplete(Guid.Empty.ToString(), out _).Should().BeFalse();

        t.PendingCount("phonebook.search").Should().Be(1);
        t.TryComplete(search, out _).Should().BeTrue();
    }

    [Fact]
    public void Replies_May_Arrive_In_Any_Order()
    {
        var t = New();
        var first = t.Begin("phonebook.search");
        var second = t.Begin("phonebook.search");

        t.TryComplete(second, out _).Should().BeTrue();
        t.PendingCount("phonebook.search").Should().Be(1);
        t.TryComplete(first, out _).Should().BeTrue();
    }

    [Fact]
    public void An_Unknown_Id_Changes_Nothing()
    {
        var t = New();
        t.Begin("phonebook.search");

        t.TryComplete(Guid.NewGuid().ToString(), out _).Should().BeFalse();

        t.PendingCount("phonebook.search").Should().Be(1);
    }

    [Fact]
    public void Requests_Without_A_Reply_Expire_After_The_Timeout()
    {
        var now = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        var t = New(() => now);
        var old = t.Begin("phonebook.search");
        now = now.AddSeconds(15);
        var recent = t.Begin("xstatus");

        t.Expire().Should().BeEmpty();

        now = now.AddSeconds(6);
        var expired = t.Expire();

        expired.Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, string>(old, "phonebook.search"));
        t.TryComplete(old, out _).Should().BeFalse("an expired request can't be completed");
        t.TryComplete(recent, out _).Should().BeTrue();
    }

    [Fact]
    public void A_Request_Can_Have_Its_Own_Timeout()
    {
        var now = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        var t = New(() => now);                       // default 20 s
        var search = t.Begin("phonebook.search");
        var status = t.Begin("sync.xstatus", TimeSpan.FromSeconds(120));

        now = now.AddSeconds(30);

        t.Expire().Select(e => e.Key).Should().Equal(search);
        t.TryComplete(status, out var kind).Should().BeTrue("its own timeout is longer");
        kind.Should().Be("sync.xstatus");
        ((Action)(() => t.Begin("k", TimeSpan.Zero))).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Clear_Drops_Everything()
    {
        var t = New();
        var id = t.Begin("phonebook.search");

        t.Clear();

        t.TryComplete(id, out _).Should().BeFalse();
        t.PendingCount("phonebook.search").Should().Be(0);
    }

    [Fact]
    public void Begin_And_Complete_Are_Thread_Safe()
    {
        var t = New();
        var ids = new System.Collections.Concurrent.ConcurrentBag<string>();

        Parallel.For(0, 2000, _ => ids.Add(t.Begin("k")));
        var completed = 0;
        Parallel.ForEach(ids, id => { if (t.TryComplete(id, out _)) Interlocked.Increment(ref completed); });

        completed.Should().Be(2000);
        t.PendingCount("k").Should().Be(0);
    }

    [Fact]
    public void Timeout_Must_Be_Positive()
    {
        ((Action)(() => new XapiRequestTracker(TimeSpan.Zero))).Should().Throw<ArgumentOutOfRangeException>();
    }
}
