using FluentAssertions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// Floating-recovery probes power-cycle the camera, so after one pass over the candidate codecs
/// they slow down.
/// </summary>
public class FloatingProbePolicyTests
{
    private static readonly TimeSpan Base = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Max = TimeSpan.FromMinutes(10);

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(4, 4)]
    [InlineData(5, 8)]
    [InlineData(6, 10)]
    [InlineData(12, 10)]
    public void Three_Codecs_First_Pass_At_Base_Then_Doubling_To_The_Cap(int attempt, int expectedMinutes)
    {
        FloatingProbePolicy.GetRetryDelay(attempt, 3, Base, Max).Should().Be(TimeSpan.FromMinutes(expectedMinutes));
    }

    [Fact]
    public void Single_Candidate_Slows_After_The_First_Probe()
    {
        FloatingProbePolicy.GetRetryDelay(1, 1, Base, Max).Should().Be(Base);
        FloatingProbePolicy.GetRetryDelay(2, 1, Base, Max).Should().Be(TimeSpan.FromMinutes(4));
    }

    [Fact]
    public void No_Candidates_Behaves_Like_One()
    {
        FloatingProbePolicy.GetRetryDelay(1, 0, Base, Max).Should().Be(Base);
        FloatingProbePolicy.GetRetryDelay(2, 0, Base, Max).Should().Be(TimeSpan.FromMinutes(4));
    }

    [Fact]
    public void Max_Below_Base_Never_Shortens_The_Base_Delay()
    {
        FloatingProbePolicy.GetRetryDelay(10, 1, Base, TimeSpan.FromMinutes(1)).Should().Be(Base);
    }

    [Fact]
    public void Large_Attempt_Numbers_Do_Not_Overflow()
    {
        FloatingProbePolicy.GetRetryDelay(int.MaxValue, 3, Base, Max).Should().Be(Max);
    }

    private static readonly string[] Abc = { "A", "B", "C" };

    private static string? Select(string[] candidates, string[] held, int attempt, out int index, out int total)
        => FloatingProbePolicy.SelectProbeCodec(candidates, c => held.Contains(c), attempt, out index, out total);

    [Fact]
    public void Rotation_With_Nothing_Held_Covers_Every_Candidate_In_Order()
    {
        Enumerable.Range(1, 4).Select(a => Select(Abc, Array.Empty<string>(), a, out _, out _))
            .Should().Equal("A", "B", "C", "A");
        Select(Abc, Array.Empty<string>(), 2, out var index, out var total);
        index.Should().Be(2);
        total.Should().Be(3);
    }

    [Fact]
    public void Held_Codec_Is_Removed_Before_Rotating_So_No_Candidate_Repeats()
    {
        // A, B, C with B held must go A, C, A, C (not A, C, C).
        Enumerable.Range(1, 4).Select(a => Select(Abc, new[] { "B" }, a, out _, out _))
            .Should().Equal("A", "C", "A", "C");
    }

    [Fact]
    public void Probe_Total_Counts_Only_Usable_Candidates()
    {
        // probeTotal drives pacing (one full pass at the base delay), so held codecs must not
        // lengthen the pass.
        Select(Abc, new[] { "B" }, 3, out var index, out var total).Should().Be("A");
        total.Should().Be(2);
        index.Should().Be(1);
        FloatingProbePolicy.GetRetryDelay(3, total, Base, Max).Should().Be(TimeSpan.FromMinutes(4));
    }

    [Fact]
    public void All_Held_Or_No_Candidates_Selects_Nothing()
    {
        Select(Abc, Abc, 1, out var index, out var total).Should().BeNull();
        index.Should().Be(0);
        total.Should().Be(0);
        Select(Array.Empty<string>(), Array.Empty<string>(), 1, out _, out _).Should().BeNull();
    }

    [Fact]
    public void Non_Positive_Attempt_Selects_Nothing()
    {
        Select(Abc, Array.Empty<string>(), 0, out _, out var total).Should().BeNull();
        total.Should().Be(3);
    }
}
