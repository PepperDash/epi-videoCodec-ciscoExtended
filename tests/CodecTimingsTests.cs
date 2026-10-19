using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// CodecTimings has no Crestron dependencies, so its source is compiled into this project
/// (see the linked Compile item in the .csproj) and tested directly.
/// </summary>
public class CodecTimingsTests
{
    [Fact]
    public void Summary_Reports_Count_Last_Min_Max_And_Percentiles()
    {
        var timings = new CodecTimings();
        foreach (var ms in new[] { 50.0, 10, 40, 20, 30 })
            timings.Record("parse.document", ms);

        var s = timings.Summary("parse.document")!;

        s.Count.Should().Be(5);
        s.LastMs.Should().Be(30);
        s.MinMs.Should().Be(10);
        s.MaxMs.Should().Be(50);
        s.MedianMs.Should().Be(30);
        s.P95Ms.Should().Be(50);
    }

    [Fact]
    public void Percentiles_Use_Only_The_Most_Recent_Samples()
    {
        var timings = new CodecTimings();
        for (var i = 0; i < CodecTimings.MaxSamples; i++)
            timings.Record("x", 1000);
        for (var i = 0; i < CodecTimings.MaxSamples; i++)
            timings.Record("x", 1);

        var s = timings.Summary("x")!;

        s.Count.Should().Be(2 * CodecTimings.MaxSamples);
        s.MedianMs.Should().Be(1);
        s.P95Ms.Should().Be(1);
        s.MaxMs.Should().Be(1000, "max covers every sample since the last reset");
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void Invalid_Samples_Are_Ignored(double ms)
    {
        var timings = new CodecTimings();
        timings.Record("x", ms);
        timings.Record("", 5);

        timings.Summaries().Should().BeEmpty();
    }

    [Fact]
    public void Stop_Records_A_Started_Operation_Once()
    {
        var timings = new CodecTimings();
        timings.Start("bookings.list");

        timings.IsPending("bookings.list").Should().BeTrue();
        timings.Stop("bookings.list").Should().BeTrue();
        timings.Stop("bookings.list").Should().BeFalse("it was already stopped");

        timings.Summary("bookings.list")!.Count.Should().Be(1);
    }

    [Fact]
    public void Stop_Without_Start_Records_Nothing()
    {
        var timings = new CodecTimings();

        timings.Stop("call.dialToConnected").Should().BeFalse();
        timings.Summaries().Should().BeEmpty();
    }

    [Fact]
    public void Overlapping_Operations_Are_Told_Apart_By_Id()
    {
        var timings = new CodecTimings();
        timings.Start("phonebook.search", "a");
        timings.Start("phonebook.search", "b");

        timings.Stop("phonebook.search", "b").Should().BeTrue();
        timings.IsPending("phonebook.search", "a").Should().BeTrue();
        timings.Stop("phonebook.search", "a", "phonebook.searchFailed").Should().BeTrue();

        timings.Summary("phonebook.search")!.Count.Should().Be(1);
        timings.Summary("phonebook.searchFailed")!.Count.Should().Be(1);
    }

    [Fact]
    public void Pending_Operations_That_Never_Stop_Do_Not_Pile_Up()
    {
        var timings = new CodecTimings();
        for (var i = 0; i < 1000; i++)
            timings.Start("phonebook.search", i.ToString());

        timings.IsPending("phonebook.search", "0").Should().BeFalse("the oldest are dropped");
        timings.IsPending("phonebook.search", "999").Should().BeTrue();
    }

    [Fact]
    public void Reset_Clears_Samples_And_Pending_Operations()
    {
        var timings = new CodecTimings();
        timings.Record("x", 1);
        timings.Start("y");
        var before = timings.SinceUtc;

        timings.Reset();

        timings.Summaries().Should().BeEmpty();
        timings.IsPending("y").Should().BeFalse();
        timings.SinceUtc.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Concurrent_Records_Are_All_Counted()
    {
        var timings = new CodecTimings();

        Parallel.For(0, 10_000, i => timings.Record(i % 2 == 0 ? "a" : "b", i % 7));

        timings.Summary("a")!.Count.Should().Be(5_000);
        timings.Summary("b")!.Count.Should().Be(5_000);
    }

    [Fact]
    public void FormatTable_Lists_Every_Name_In_Order()
    {
        var timings = new CodecTimings();
        timings.Record("sync.total", 14231.4);
        timings.Record("call.feedbackLatency", 3.25);

        var table = timings.FormatTable("\n");
        var lines = table.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(3);
        lines[1].Should().StartWith("call.feedbackLatency");
        lines[2].Should().StartWith("sync.total").And.Contain("14231.4");
    }

    [Fact]
    public void FormatTable_Says_So_When_Empty()
    {
        new CodecTimings().FormatTable("\n").Should().Be("No timings recorded.\n");
    }

    [Fact]
    public void ElapsedMs_Converts_Stopwatch_Ticks()
    {
        CodecTimings.ElapsedMs(0, System.Diagnostics.Stopwatch.Frequency).Should().BeApproximately(1000, 0.001);
    }
}
