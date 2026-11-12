using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// The send watchdog sees a stalled send path, says why, reports start and end once each, and
/// recovers at most once per interval (#194).
/// </summary>
public class SendStallDetectorTests
{
    private static readonly DateTime T0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan StallAfter = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RecoveryInterval = TimeSpan.FromMinutes(5);

    private static SendStallDetector Create() => new SendStallDetector(StallAfter, RecoveryInterval);

    private static SendQueueSnapshot Snapshot(DateTime at, int commands = 0, double? waiting = null,
        double? busy = null, double? jsonHold = null, double? sinceJsonHold = null, double? sinceCompleted = null,
        long probes = 0) =>
        new SendQueueSnapshot(at,
            systemPending: 0, systemWaiting: null, systemSinceDequeue: null,
            commandPending: commands, commandWaiting: Seconds(waiting), commandSinceDequeue: Seconds(waiting),
            busyFor: Seconds(busy), busyWith: busy == null ? null : "xCommand Camera FactoryReset ...",
            jsonHoldFor: Seconds(jsonHold), sinceSent: null, sinceJsonHold: Seconds(sinceJsonHold),
            sinceCompleted: Seconds(sinceCompleted), probeCompletions: probes);

    private static TimeSpan? Seconds(double? s) => s == null ? null : TimeSpan.FromSeconds(s.Value);

    [Fact]
    public void Idle_Path_Is_Not_Stalled()
    {
        Create().Evaluate(Snapshot(T0), out var cause, out _).Should().Be(SendStallTransition.None);
        cause.Should().Be(SendStallCause.None);
    }

    [Fact]
    public void Commands_Waiting_Less_Than_The_Threshold_Are_Not_A_Stall()
    {
        Create().Evaluate(Snapshot(T0, commands: 4, waiting: 29), out var cause, out _)
            .Should().Be(SendStallTransition.None);
        cause.Should().Be(SendStallCause.None);
    }

    [Fact]
    public void Waiting_With_An_Idle_Worker_Means_The_Worker_Is_Not_Running()
    {
        var detector = Create();
        detector.Evaluate(Snapshot(T0, commands: 4, waiting: 30), out var cause, out _)
            .Should().Be(SendStallTransition.Stalled);
        cause.Should().Be(SendStallCause.WorkerNotRunning);
        detector.IsStalled.Should().BeTrue();
    }

    [Fact]
    public void Worker_Running_One_Action_Too_Long_Is_Blocked_Even_With_Nothing_Waiting()
    {
        Create().Evaluate(Snapshot(T0, busy: 45), out var cause, out _).Should().Be(SendStallTransition.Stalled);
        cause.Should().Be(SendStallCause.WorkerBlocked);
    }

    [Fact]
    public void Busy_Worker_With_A_Short_Wait_Is_Fine()
    {
        Create().Evaluate(Snapshot(T0, commands: 2, waiting: 5, busy: 5), out var cause, out _)
            .Should().Be(SendStallTransition.None);
        cause.Should().Be(SendStallCause.None);
    }

    [Fact]
    public void Waiting_Behind_A_Json_Hold_Is_A_Json_Hold_Stall()
    {
        Create().Evaluate(Snapshot(T0, commands: 3, waiting: 40, jsonHold: 40), out var cause, out _)
            .Should().Be(SendStallTransition.Stalled);
        cause.Should().Be(SendStallCause.JsonHold);
    }

    [Fact]
    public void A_Hold_That_Switches_Off_And_On_Still_Counts_As_A_Json_Hold()
    {
        // At this instant the worker is not holding, but it was 0.2 s ago.
        Create().Evaluate(Snapshot(T0, commands: 3, waiting: 40, sinceJsonHold: 0.2), out var cause, out _)
            .Should().Be(SendStallTransition.Stalled);
        cause.Should().Be(SendStallCause.JsonHold);
    }

    [Fact]
    public void An_Old_Json_Hold_Does_Not_Count()
    {
        Create().Evaluate(Snapshot(T0, commands: 3, waiting: 40, sinceJsonHold: 600), out var cause, out _);
        cause.Should().Be(SendStallCause.WorkerNotRunning);
    }

    [Fact]
    public void Stall_Is_Reported_Once_Then_Its_End_Once()
    {
        var detector = Create();
        detector.Evaluate(Snapshot(T0, commands: 1, waiting: 31), out _, out _).Should().Be(SendStallTransition.Stalled);
        detector.Evaluate(Snapshot(T0.AddSeconds(5), commands: 1, waiting: 36), out _, out _).Should().Be(SendStallTransition.None);

        detector.Evaluate(Snapshot(T0.AddSeconds(42)), out _, out var stalledFor).Should().Be(SendStallTransition.Recovered);
        stalledFor.Should().Be(TimeSpan.FromSeconds(42));
        detector.IsStalled.Should().BeFalse();

        detector.Evaluate(Snapshot(T0.AddSeconds(47)), out _, out _).Should().Be(SendStallTransition.None);
    }

    [Fact]
    public void Recovery_Is_Allowed_Once_Per_Interval_While_Stalled()
    {
        var detector = Create();
        detector.TryAcquireRecovery(T0).Should().BeFalse("not stalled yet");

        detector.Evaluate(Snapshot(T0, commands: 1, waiting: 31), out _, out _);
        detector.TryAcquireRecovery(T0).Should().BeTrue();
        detector.TryAcquireRecovery(T0.AddMinutes(4)).Should().BeFalse();
        detector.TryAcquireRecovery(T0.AddMinutes(5)).Should().BeTrue();
    }

    [Fact]
    public void No_Recovery_After_The_Stall_Ends()
    {
        var detector = Create();
        detector.Evaluate(Snapshot(T0, commands: 1, waiting: 31), out _, out _);
        detector.Evaluate(Snapshot(T0.AddSeconds(5)), out _, out _);

        detector.TryAcquireRecovery(T0.AddMinutes(10)).Should().BeFalse();
    }

    [Fact]
    public void Recovery_Has_Failed_Only_After_It_Had_Time_To_Work()
    {
        var detector = Create();
        detector.Evaluate(Snapshot(T0, commands: 1, waiting: 31), out _, out _);
        detector.RecoveryFailed(T0.AddMinutes(1)).Should().BeFalse("no recovery has been tried");

        detector.TryAcquireRecovery(T0).Should().BeTrue();
        detector.RecoveryFailed(T0.AddSeconds(29)).Should().BeFalse("the recovery is still within its window");
        detector.RecoveryFailed(T0.AddSeconds(30)).Should().BeTrue();
    }

    [Fact]
    public void Recovery_Failure_Clears_When_The_Stall_Ends()
    {
        var detector = Create();
        detector.Evaluate(Snapshot(T0, commands: 1, waiting: 31), out _, out _);
        detector.TryAcquireRecovery(T0);
        detector.RecoveryFailed(T0.AddSeconds(40)).Should().BeTrue();

        detector.Evaluate(Snapshot(T0.AddSeconds(45), probes: 1), out _, out _).Should().Be(SendStallTransition.Recovered);
        detector.RecoveryFailed(T0.AddSeconds(50)).Should().BeFalse();
    }

    [Fact]
    public void A_New_Stall_Starts_Without_The_Old_Recovery_Attempt()
    {
        var detector = Create();
        detector.Evaluate(Snapshot(T0, commands: 1, waiting: 31), out _, out _);
        detector.TryAcquireRecovery(T0);
        detector.Evaluate(Snapshot(T0.AddSeconds(10), probes: 1), out _, out _).Should().Be(SendStallTransition.Recovered);

        detector.Evaluate(Snapshot(T0.AddMinutes(1), commands: 1, waiting: 31), out _, out _)
            .Should().Be(SendStallTransition.Stalled);
        detector.RecoveryFailed(T0.AddMinutes(2)).Should().BeFalse("this stall hasn't had a recovery attempt yet");
    }

    [Fact]
    public void After_A_Recovery_An_Empty_Queue_Is_Not_Proof_Of_Recovery()
    {
        // The recovery drained the queue; nothing has finished since. The write may still be blocked.
        var detector = Create();
        detector.Evaluate(Snapshot(T0, commands: 2, waiting: 31), out _, out _);
        detector.TryAcquireRecovery(T0).Should().BeTrue();

        detector.Evaluate(Snapshot(T0.AddSeconds(5)), out var cause, out _).Should().Be(SendStallTransition.None);
        cause.Should().Be(SendStallCause.None);
        detector.IsStalled.Should().BeTrue("nothing has completed since the recovery");
        detector.RecoveryFailed(T0.AddSeconds(30)).Should().BeTrue("so the fault is shown");
    }

    [Fact]
    public void A_Probe_Completed_Before_The_Recovery_Does_Not_Count()
    {
        // One probe got through during an earlier stall; the count is taken at this recovery.
        var detector = Create();
        detector.Evaluate(Snapshot(T0, commands: 2, waiting: 31, probes: 1), out _, out _);
        detector.TryAcquireRecovery(T0);

        detector.Evaluate(Snapshot(T0.AddSeconds(10), probes: 1), out _, out _).Should().Be(SendStallTransition.None);
        detector.IsStalled.Should().BeTrue();
    }

    [Fact]
    public void An_Unrelated_Action_Finishing_Does_Not_End_The_Stall()
    {
        // e.g. a sync retry that sends nothing finishes before the probe gets the send lock.
        var detector = Create();
        detector.Evaluate(Snapshot(T0, commands: 2, waiting: 31), out _, out _);
        detector.TryAcquireRecovery(T0);

        detector.Evaluate(Snapshot(T0.AddSeconds(3), sinceCompleted: 1), out _, out _).Should().Be(SendStallTransition.None);
        detector.IsStalled.Should().BeTrue();
    }

    [Fact]
    public void The_Probe_Completing_After_The_Recovery_Ends_The_Stall()
    {
        var detector = Create();
        detector.Evaluate(Snapshot(T0, commands: 2, waiting: 31), out _, out _);
        detector.TryAcquireRecovery(T0);

        detector.Evaluate(Snapshot(T0.AddSeconds(3), probes: 1), out _, out var stalledFor)
            .Should().Be(SendStallTransition.Recovered);
        stalledFor.Should().Be(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void A_Probe_Finishing_In_The_Same_Clock_Tick_As_The_Recovery_Counts()
    {
        // Timestamps would tie here; the count doesn't.
        var detector = Create();
        detector.Evaluate(Snapshot(T0, commands: 2, waiting: 31), out _, out _);
        detector.TryAcquireRecovery(T0);

        detector.Evaluate(Snapshot(T0, probes: 1), out _, out _).Should().Be(SendStallTransition.Recovered);
    }

    [Fact]
    public void Retries_Do_Not_Postpone_The_Fault()
    {
        // A stall threshold longer than the recovery interval: each retry used to restart the window.
        var detector = new SendStallDetector(TimeSpan.FromMinutes(6), TimeSpan.FromMinutes(5));
        detector.Evaluate(Snapshot(T0, commands: 1, waiting: 400), out _, out _);
        detector.TryAcquireRecovery(T0).Should().BeTrue();
        detector.TryAcquireRecovery(T0.AddMinutes(5)).Should().BeTrue("a retry");

        detector.RecoveryFailed(T0.AddMinutes(6)).Should().BeTrue("measured from the first attempt");
    }

    [Fact]
    public void A_Stall_That_Clears_Without_A_Recovery_Needs_No_Completion()
    {
        // e.g. a slow action finished on its own before the watchdog acted.
        var detector = Create();
        detector.Evaluate(Snapshot(T0, busy: 31), out _, out _).Should().Be(SendStallTransition.Stalled);
        detector.Evaluate(Snapshot(T0.AddSeconds(5)), out _, out _).Should().Be(SendStallTransition.Recovered);
    }

    [Fact]
    public void Threshold_Must_Be_Positive()
    {
        var act = () => new SendStallDetector(TimeSpan.Zero, RecoveryInterval);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
