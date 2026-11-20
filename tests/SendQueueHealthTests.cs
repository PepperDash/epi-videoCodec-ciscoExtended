using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// The send path's state for codecperf and the send watchdog (#194): queue depth, how long a queue
/// has waited with no progress, what the worker is running, the JSON hold and the last send.
/// </summary>
public class SendQueueHealthTests
{
    private static readonly DateTime T0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void New_Health_Is_Idle_And_Empty()
    {
        var snap = new SendQueueHealth().Snapshot(T0);

        snap.CommandPending.Should().Be(0);
        snap.SystemPending.Should().Be(0);
        snap.LongestWaiting.Should().BeNull();
        snap.BusyFor.Should().BeNull();
        snap.JsonHoldFor.Should().BeNull();
        snap.SinceSent.Should().BeNull();
        snap.Format().Should().Be(
            "commands=0 (last taken never) system=0 (last taken never) worker=idle jsonHold=- lastSent=never");
    }

    [Fact]
    public void Waiting_Is_Measured_From_When_The_Queue_Stopped_Being_Empty()
    {
        var health = new SendQueueHealth();
        health.OnEnqueued(SendQueueKind.Command, T0);
        health.OnEnqueued(SendQueueKind.Command, T0.AddSeconds(5));

        var snap = health.Snapshot(T0.AddSeconds(12));
        snap.CommandPending.Should().Be(2);
        snap.CommandWaiting.Should().Be(TimeSpan.FromSeconds(12), "the first command has waited since T0");
        snap.LongestWaiting.Should().Be(TimeSpan.FromSeconds(12));
    }

    [Fact]
    public void Taking_An_Action_Off_The_Queue_Restarts_The_Wait()
    {
        var health = new SendQueueHealth();
        health.OnEnqueued(SendQueueKind.Command, T0);
        health.OnEnqueued(SendQueueKind.Command, T0);
        health.OnStarted(SendQueueKind.Command, "xCommand Dial Number: ...", T0.AddSeconds(10));
        health.OnFinished();

        var snap = health.Snapshot(T0.AddSeconds(13));
        snap.CommandPending.Should().Be(1);
        snap.CommandWaiting.Should().Be(TimeSpan.FromSeconds(3), "progress was made at T0+10");
        snap.CommandSinceDequeue.Should().Be(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Empty_Queue_Is_Not_Waiting()
    {
        var health = new SendQueueHealth();
        health.OnEnqueued(SendQueueKind.System, T0);
        health.OnStarted(SendQueueKind.System, "JsonMessageReceived", T0.AddSeconds(1));
        health.OnFinished();

        var snap = health.Snapshot(T0.AddMinutes(10));
        snap.SystemPending.Should().Be(0);
        snap.SystemWaiting.Should().BeNull("an empty queue waits for nothing, however long ago it was last used");
    }

    [Fact]
    public void Busy_Worker_Reports_What_It_Runs_And_For_How_Long()
    {
        var health = new SendQueueHealth();
        health.OnEnqueued(SendQueueKind.Command, T0);
        health.OnStarted(SendQueueKind.Command, "xCommand Camera FactoryReset ...", T0.AddSeconds(1));

        var busy = health.Snapshot(T0.AddSeconds(31));
        busy.BusyFor.Should().Be(TimeSpan.FromSeconds(30));
        busy.BusyWith.Should().Be("xCommand Camera FactoryReset ...");
        busy.Format().Should().Contain("worker=busy 30.0 s on 'xCommand Camera FactoryReset ...'");

        health.OnFinished();
        health.Snapshot(T0.AddSeconds(32)).BusyFor.Should().BeNull();
    }

    [Fact]
    public void Finishing_Records_A_Completion_But_Replacing_The_Worker_Does_Not()
    {
        var health = new SendQueueHealth();
        health.OnStarted(SendQueueKind.Command, "x", T0);
        health.OnFinished(T0.AddSeconds(1));
        health.Snapshot(T0.AddSeconds(3)).SinceCompleted.Should().Be(TimeSpan.FromSeconds(2));

        health.OnStarted(SendQueueKind.Command, "stuck write", T0.AddSeconds(4));
        health.OnWorkerReplaced();

        var snap = health.Snapshot(T0.AddSeconds(10));
        snap.BusyFor.Should().BeNull("the replaced worker's action no longer counts as running");
        snap.SinceCompleted.Should().Be(TimeSpan.FromSeconds(9), "it didn't finish either");
    }

    [Fact]
    public void Json_Hold_Is_Measured_From_Its_Start()
    {
        var health = new SendQueueHealth();
        health.OnJsonHold(T0);
        health.OnJsonHold(T0.AddSeconds(4)); // still holding: the start stays

        health.Snapshot(T0.AddSeconds(9)).JsonHoldFor.Should().Be(TimeSpan.FromSeconds(9));

        health.OnJsonHoldEnded();
        health.Snapshot(T0.AddSeconds(10)).JsonHoldFor.Should().BeNull();
    }

    [Fact]
    public void Cleared_Queue_Has_Nothing_Waiting()
    {
        var health = new SendQueueHealth();
        health.OnEnqueued(SendQueueKind.Command, T0);
        health.OnEnqueued(SendQueueKind.Command, T0);
        health.OnCleared(SendQueueKind.Command);

        var snap = health.Snapshot(T0.AddSeconds(30));
        snap.CommandPending.Should().Be(0);
        snap.CommandWaiting.Should().BeNull();
    }

    [Fact]
    public void Pending_Never_Goes_Negative()
    {
        var health = new SendQueueHealth();
        // A clear from another thread can land between enqueue and dequeue.
        health.OnEnqueued(SendQueueKind.Command, T0);
        health.OnCleared(SendQueueKind.Command);
        health.OnStarted(SendQueueKind.Command, "x", T0.AddSeconds(1));

        health.Snapshot(T0.AddSeconds(2)).CommandPending.Should().Be(0);
    }

    [Fact]
    public void Counting_Before_Queuing_Survives_The_Worker_Running_The_Action_First()
    {
        // Queuing thread: counts, then puts the action on the queue. The worker takes it off and runs it
        // straight away. Counting first means the start always finds the action already counted.
        var health = new SendQueueHealth();
        health.OnEnqueued(SendQueueKind.System, T0);
        health.OnStarted(SendQueueKind.System, "InitialSoftwareVersionMessageReceived", T0);
        health.OnFinished();

        var snap = health.Snapshot(T0.AddMinutes(5));
        snap.SystemPending.Should().Be(0);
        snap.SystemWaiting.Should().BeNull("nothing is waiting, however long the queue stays idle");
    }

    [Fact]
    public void Rejected_Enqueue_Is_Taken_Back()
    {
        var health = new SendQueueHealth();
        health.OnEnqueued(SendQueueKind.Command, T0);
        health.OnEnqueueRejected(SendQueueKind.Command);

        var snap = health.Snapshot(T0.AddMinutes(1));
        snap.CommandPending.Should().Be(0);
        snap.CommandWaiting.Should().BeNull();
    }

    [Fact]
    public void Rejected_Enqueue_Keeps_The_Others_Waiting()
    {
        var health = new SendQueueHealth();
        health.OnEnqueued(SendQueueKind.Command, T0);
        health.OnEnqueued(SendQueueKind.Command, T0.AddSeconds(5));
        health.OnEnqueueRejected(SendQueueKind.Command);

        var snap = health.Snapshot(T0.AddSeconds(10));
        snap.CommandPending.Should().Be(1);
        snap.CommandWaiting.Should().Be(TimeSpan.FromSeconds(10), "the first command still waits since T0");
    }

    [Fact]
    public void Queues_Found_Empty_Clear_Any_Miscount()
    {
        // Counted after queuing (the old order): the worker ran the action first, so one phantom
        // remains. The worker finding both queues empty clears it.
        var health = new SendQueueHealth();
        health.OnStarted(SendQueueKind.System, "x", T0);
        health.OnFinished();
        health.OnEnqueued(SendQueueKind.System, T0);
        health.OnEnqueued(SendQueueKind.Command, T0);
        health.Snapshot(T0.AddSeconds(31)).LongestWaiting.Should().Be(TimeSpan.FromSeconds(31));

        health.OnQueuesEmpty();

        var snap = health.Snapshot(T0.AddMinutes(5));
        snap.SystemPending.Should().Be(0);
        snap.CommandPending.Should().Be(0);
        snap.LongestWaiting.Should().BeNull();
    }

    [Fact]
    public void Longest_Waiting_Takes_The_Older_Queue()
    {
        var health = new SendQueueHealth();
        health.OnEnqueued(SendQueueKind.System, T0.AddSeconds(5));
        health.OnEnqueued(SendQueueKind.Command, T0);

        health.Snapshot(T0.AddSeconds(20)).LongestWaiting.Should().Be(TimeSpan.FromSeconds(20));
    }

    [Fact]
    public void Format_Shows_Every_Part()
    {
        var health = new SendQueueHealth();
        health.OnSent(T0);
        health.OnEnqueued(SendQueueKind.Command, T0);
        health.OnEnqueued(SendQueueKind.Command, T0);
        health.OnEnqueued(SendQueueKind.Command, T0);
        health.OnJsonHold(T0.AddSeconds(2));

        health.Snapshot(T0.AddSeconds(12)).Format().Should().Be(
            "commands=3 waiting 12.0 s (last taken never) system=0 (last taken never) worker=idle jsonHold=10.0 s lastSent=12.0 s ago");
    }

    [Fact]
    public void Clock_Going_Backwards_Reads_As_Zero()
    {
        var health = new SendQueueHealth();
        health.OnSent(T0.AddSeconds(5));

        health.Snapshot(T0).SinceSent.Should().Be(TimeSpan.Zero);
    }
}
