using FluentAssertions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// Floating-recovery probes must not start while the switch session is failing; they back off
/// until a switch command succeeds.
/// </summary>
public class SwitchCommandHealthTests
{
    private static readonly DateTime T0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Stale = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan Base = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Max = TimeSpan.FromMinutes(10);

    private static SwitchCommandHealth Create() => new SwitchCommandHealth(Stale, Base, Max);

    [Fact]
    public void Healthy_Switch_Grants_Every_Probe()
    {
        var health = Create();
        health.OnCommandStarted(T0).Should().Be(SwitchHealthTransition.None);
        health.OnCommandCompleted(T0.AddSeconds(3)).Should().Be(SwitchHealthTransition.None);

        health.IsFailing.Should().BeFalse();
        health.TryAcquireProbe(T0.AddSeconds(4), out _).Should().BeTrue();
        health.TryAcquireProbe(T0.AddSeconds(5), out _).Should().BeTrue();
    }

    [Fact]
    public void Switch_Without_Progress_Events_Is_Never_Failing()
    {
        // A driver that only raises completion events (or none) must not block probes.
        var health = Create();
        health.Evaluate(T0.AddHours(1)).Should().Be(SwitchHealthTransition.None);
        health.IsFailing.Should().BeFalse();
        health.TryAcquireProbe(T0.AddHours(1), out _).Should().BeTrue();
    }

    [Fact]
    public void New_Session_Starting_While_Previous_Never_Completed_Is_A_Failure()
    {
        // Session timed out / aborted: no completion event, the queue moves to the next session.
        var health = Create();
        health.OnCommandStarted(T0);
        health.OnCommandStarted(T0.AddSeconds(15)).Should().Be(SwitchHealthTransition.Failing);

        health.IsFailing.Should().BeTrue();
        health.ConsecutiveFailures.Should().Be(1);
        health.FailingSinceUtc.Should().Be(T0.AddSeconds(15));
    }

    [Fact]
    public void Session_That_Never_Completes_Fails_After_The_Stale_Window()
    {
        var health = Create();
        health.OnCommandStarted(T0);
        health.Evaluate(T0.AddSeconds(59)).Should().Be(SwitchHealthTransition.None);
        health.Evaluate(T0.AddSeconds(60)).Should().Be(SwitchHealthTransition.Failing);

        // Counted once, not on every tick.
        health.Evaluate(T0.AddSeconds(61)).Should().Be(SwitchHealthTransition.None);
        health.ConsecutiveFailures.Should().Be(1);
    }

    [Fact]
    public void Failing_Switch_Holds_Probes_Until_The_Backoff_Elapses_Then_Allows_One_Trial()
    {
        var health = Create();
        health.OnCommandStarted(T0);
        health.OnCommandStarted(T0.AddSeconds(15)); // failure #1 at T0+15s, backoff 1 min

        health.TryAcquireProbe(T0.AddSeconds(30), out var retryAt).Should().BeFalse();
        retryAt.Should().Be(T0.AddSeconds(75));

        health.TryAcquireProbe(T0.AddSeconds(75), out var nextTrial).Should().BeTrue();
        nextTrial.Should().Be(T0.AddSeconds(135));

        // Only one trial per window.
        health.TryAcquireProbe(T0.AddSeconds(76), out _).Should().BeFalse();
    }

    [Fact]
    public void Backoff_Doubles_Per_Failure_And_Is_Capped()
    {
        var health = Create();
        var now = T0;
        health.OnCommandStarted(now);
        var expected = new[] { 1, 2, 4, 8, 10, 10 };
        foreach (var minutes in expected)
        {
            now = now.AddSeconds(15);
            health.OnCommandStarted(now);
            health.CurrentBackoff.Should().Be(TimeSpan.FromMinutes(minutes));
        }
    }

    [Fact]
    public void Any_Completed_Command_Clears_The_Failure_And_Probes_Resume()
    {
        var health = Create();
        health.OnCommandStarted(T0);
        health.OnCommandStarted(T0.AddSeconds(15));
        health.OnCommandStarted(T0.AddSeconds(30));
        health.ConsecutiveFailures.Should().Be(2);

        health.OnCommandCompleted(T0.AddSeconds(33)).Should().Be(SwitchHealthTransition.Recovered);

        health.IsFailing.Should().BeFalse();
        health.ConsecutiveFailures.Should().Be(0);
        health.FailingSinceUtc.Should().Be(DateTime.MinValue);
        health.TryAcquireProbe(T0.AddSeconds(34), out _).Should().BeTrue();
        health.OnCommandCompleted(T0.AddSeconds(40)).Should().Be(SwitchHealthTransition.None);
    }
}
