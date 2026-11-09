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
}
