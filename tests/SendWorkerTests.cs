using System.Collections.Concurrent;
using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// The send worker on real threads, and the send watchdog's recovery of each stall cause, end to end
/// without a processor (#194): a worker that isn't running, a blocked write, a JSON response that
/// never ends, a full queue, and the bookkeeping under concurrent use.
/// </summary>
public class SendWorkerTests : IDisposable
{
    private static readonly TimeSpan StallAfter = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly List<ManualResetEventSlim> _gates = new();
    private readonly List<Harness> _harnesses = new();

    public void Dispose()
    {
        // Release anything a test left blocked, stop every worker, and wait for their threads to end.
        foreach (var gate in _gates)
            gate.Set();
        foreach (var harness in _harnesses)
            harness.Worker.Stop();
        foreach (var harness in _harnesses)
            harness.JoinThreads(TimeSpan.FromSeconds(5));
        foreach (var gate in _gates)
            gate.Dispose();
    }

    private Harness Track(Harness harness)
    {
        _harnesses.Add(harness);
        return harness;
    }

    [Fact]
    public void Runs_System_Actions_Before_Commands_In_Order()
    {
        var h = Track(new Harness());
        var ran = new ConcurrentQueue<string>();
        h.Worker.Enqueue(SendQueueKind.Command, () => ran.Enqueue("c1"), "c1");
        h.Worker.Enqueue(SendQueueKind.Command, () => ran.Enqueue("c2"), "c2");
        h.Worker.Enqueue(SendQueueKind.System, () => ran.Enqueue("s1"), "s1");

        h.Worker.Schedule();

        WaitUntil(() => ran.Count == 3);
        ran.Should().Equal("s1", "c1", "c2");
        WaitUntil(() => h.Worker.Health.Snapshot(DateTime.UtcNow).LongestWaiting == null);
    }

    [Fact]
    public void An_Action_That_Throws_Is_Reported_And_The_Worker_Carries_On()
    {
        var h = Track(new Harness());
        var ran = false;
        h.Worker.Enqueue(SendQueueKind.Command, () => throw new InvalidOperationException("boom"), "bad");
        h.Worker.Enqueue(SendQueueKind.Command, () => ran = true, "good");
        h.Worker.Schedule();

        WaitUntil(() => ran);
        h.Errors.Should().ContainSingle().Which.Should().Be("user action: boom");
    }

    [Fact]
    public void Full_Queue_Drops_Instead_Of_Blocking_The_Caller()
    {
        var h = Track(new Harness(capacity: 5));
        var gate = Gate();
        h.Worker.Enqueue(SendQueueKind.Command, () => gate.Wait(), "blocking write");
        h.Worker.Schedule();
        WaitUntil(() => h.Worker.Health.Snapshot(DateTime.UtcNow).BusyFor != null);

        var accepted = Enumerable.Range(0, 10)
            .Count(i => h.Worker.Enqueue(SendQueueKind.Command, () => { }, "c" + i));

        accepted.Should().Be(5, "the queue holds 5; the rest are dropped at once, not waited on");
        h.Worker.Health.Snapshot(DateTime.UtcNow).CommandPending.Should().Be(5);
    }

    [Fact]
    public void Worker_That_Is_Not_Running_Is_Detected_And_Restarted()
    {
        // The first worker thread never runs (as if it died): commands pile up.
        var h = Track(new Harness(startFirstWorker: false));
        var ran = new ConcurrentQueue<string>();
        for (var i = 0; i < 3; i++)
        {
            var n = i;
            h.Worker.Enqueue(SendQueueKind.Command, () => ran.Enqueue("old" + n), "old" + n);
        }
        h.Worker.Schedule();

        var check = h.WatchUntil(c => c.Cause != SendStallCause.None);
        check.Cause.Should().Be(SendStallCause.WorkerNotRunning);
        check.Action.Should().Be(SendStallAction.RestartWorker);
        check.Dropped.Should().Be(3, "commands that waited out the stall are dropped, not sent late");
        ran.Should().BeEmpty();

        // The new worker sends what comes next, and the stall ends.
        h.Worker.Enqueue(SendQueueKind.Command, () => ran.Enqueue("new"), "new");
        h.Worker.Schedule();
        WaitUntil(() => ran.Contains("new"));
        h.WatchUntil(c => c.Transition == SendStallTransition.Recovered);
    }

    [Fact]
    public void Blocked_Write_Is_Detected_And_A_Reconnect_Ends_It()
    {
        var h = Track(new Harness());
        var write = Gate();
        var ran = new ConcurrentQueue<string>();
        h.Worker.Enqueue(SendQueueKind.Command, () => { write.Wait(); ran.Enqueue("blocked"); }, "xCommand Camera FactoryReset ...");
        h.Worker.Enqueue(SendQueueKind.Command, () => ran.Enqueue("behind"), "behind");
        h.Worker.Schedule();

        // A reconnect ends the write (the socket closes under it).
        h.OnReconnect = () => write.Set();
        var check = h.WatchUntil(c => c.Cause != SendStallCause.None);

        check.Cause.Should().Be(SendStallCause.WorkerBlocked);
        check.Action.Should().Be(SendStallAction.Reconnect);
        check.Busy.Should().Be("xCommand Camera FactoryReset ...");
        WaitUntil(() => ran.Contains("blocked"));
        ran.Should().NotContain("behind", "the command stuck behind the write was dropped by the recovery");
        h.WatchUntil(c => c.Transition == SendStallTransition.Recovered);
    }

    [Fact]
    public void Blocked_Write_Over_Serial_Keeps_The_Worker_And_Piles_Up_No_Threads()
    {
        // Over RS-232 there is no session to reconnect, and a new worker would block on the same send
        // lock: the watchdog keeps the worker, however many times it retries.
        var h = Track(new Harness(canReconnect: false));
        var write = Gate();
        var ran = new ConcurrentQueue<string>();
        h.Worker.Enqueue(SendQueueKind.Command, () => { write.Wait(); ran.Enqueue("stuck"); }, "stuck");
        h.Worker.Schedule();

        var check = h.WatchUntil(c => c.Action != null);
        check.Cause.Should().Be(SendStallCause.WorkerBlocked);
        check.Action.Should().Be(SendStallAction.KeepWorker);
        for (var i = 0; i < 5; i++)
        {
            h.Check(); // retries (no recovery interval in the harness)
            Thread.Sleep(20);
        }
        h.Starts.Should().Be(1, "no replacement workers pile up behind the blocked write");

        // The write finishes: the same worker carries on, the probe gets through, the stall ends.
        write.Set();
        WaitUntil(() => ran.Contains("stuck"));
        h.WatchUntil(c => c.Transition == SendStallTransition.Recovered);
        h.Starts.Should().Be(1);
    }

    [Fact]
    public void Write_Blocked_Under_The_Send_Lock_Over_Serial_Is_Reported_As_Unrecovered()
    {
        // The write holds the send lock and never returns. Over RS-232 nothing can end it: the stall
        // must stay on and be reported as unrecovered, not "recovered" every 5 minutes.
        var h = Track(new Harness(canReconnect: false));
        var write = Gate();
        h.Worker.Enqueue(SendQueueKind.Command, () => { lock (h.SendLock) write.Wait(); }, "xCommand Standby Activate");
        h.Worker.Schedule();

        var check = h.WatchUntil(c => c.Action != null);
        check.Cause.Should().Be(SendStallCause.WorkerBlocked);
        check.Action.Should().Be(SendStallAction.KeepWorker);

        // Past the failure window the stall is still on and the recovery counts as failed.
        Thread.Sleep(StallAfter + TimeSpan.FromMilliseconds(300));
        var after = h.Check();
        after.Transition.Should().NotBe(SendStallTransition.Recovered);
        h.Detector.IsStalled.Should().BeTrue();
        h.Detector.RecoveryFailed(DateTime.UtcNow).Should().BeTrue();
        after.Busy.Should().Be("xCommand Standby Activate", "the worker is still in the blocked write");
        h.Starts.Should().Be(1);

        // The write returns at last: the probe gets through and the stall ends.
        write.Set();
        h.WatchUntil(c => c.Transition == SendStallTransition.Recovered);
    }

    [Fact]
    public void Json_Response_That_Never_Ends_Is_Detected_And_A_Reconnect_Clears_It()
    {
        var h = Track(new Harness { ReceivingJson = true });
        var ran = new ConcurrentQueue<string>();
        h.Worker.Enqueue(SendQueueKind.Command, () => ran.Enqueue("held"), "held");
        h.Worker.Schedule();

        // A reconnect resets the receive state.
        h.OnReconnect = () => h.ReceivingJson = false;
        var check = h.WatchUntil(c => c.Cause != SendStallCause.None);

        check.Cause.Should().Be(SendStallCause.JsonHold);
        check.Action.Should().Be(SendStallAction.Reconnect);
        check.Dropped.Should().Be(1);

        h.Worker.Enqueue(SendQueueKind.Command, () => ran.Enqueue("after"), "after");
        h.Worker.Schedule();
        WaitUntil(() => ran.Contains("after"));
        ran.Should().NotContain("held");
        h.WatchUntil(c => c.Transition == SendStallTransition.Recovered);
    }

    [Fact]
    public void Healthy_Busy_Worker_Never_Reads_As_A_Stall()
    {
        var h = Track(new Harness());
        var done = 0;
        for (var i = 0; i < 20; i++)
            h.Worker.Enqueue(SendQueueKind.Command, () => { Thread.Sleep(10); Interlocked.Increment(ref done); }, "c");
        h.Worker.Schedule();

        while (Volatile.Read(ref done) < 20)
        {
            h.Check().Cause.Should().Be(SendStallCause.None);
            Thread.Sleep(20);
        }
    }

    [Fact]
    public void Bookkeeping_Stays_Right_Under_Concurrent_Queuing_Draining_And_Holds()
    {
        // Many threads queue at once while the worker runs, the watchdog drains and the JSON hold
        // flickers. Once everything has settled, nothing may read as waiting, so no false stall.
        var h = Track(new Harness(capacity: 1000));
        var producers = Enumerable.Range(0, 8).Select(p => new Thread(() =>
        {
            for (var i = 0; i < 2000; i++)
            {
                h.Worker.Enqueue(i % 3 == 0 ? SendQueueKind.System : SendQueueKind.Command, () => { }, "x");
                h.Worker.Schedule();
            }
        })).ToList();
        var stop = false;
        var chaos = new Thread(() =>
        {
            var r = new Random(194);
            while (!Volatile.Read(ref stop))
            {
                if (r.Next(4) == 0) h.Worker.DrainCommands(0, out _);
                h.ReceivingJson = r.Next(10) == 0;
                Thread.Sleep(1);
            }
            h.ReceivingJson = false;
        }) { IsBackground = true };
        producers.ForEach(t => t.IsBackground = true);

        chaos.Start();
        try
        {
            producers.ForEach(t => t.Start());
            producers.ForEach(t => t.Join(Timeout).Should().BeTrue());
        }
        finally
        {
            Volatile.Write(ref stop, true);
            chaos.Join(Timeout).Should().BeTrue();
        }

        h.Worker.Schedule();
        WaitUntil(() =>
        {
            var s = h.Worker.Health.Snapshot(DateTime.UtcNow);
            return s.BusyFor == null && s.LongestWaiting == null && s.SystemPending == 0 && s.CommandPending == 0;
        });

        Thread.Sleep(StallAfter + StallAfter);
        h.Check().Cause.Should().Be(SendStallCause.None, "an idle worker with empty queues is not stalled");
    }

    [Fact]
    public void Reset_Arriving_At_A_Full_System_Queue_Replaces_The_Stale_Actions()
    {
        var h = Track(new Harness(capacity: 3, startFirstWorker: false));
        var ran = new ConcurrentQueue<string>();
        for (var i = 0; i < 3; i++)
        {
            var n = i;
            h.Worker.Enqueue(SendQueueKind.System, () => ran.Enqueue("stale" + n), "stale" + n).Should().BeTrue();
        }
        h.Worker.Enqueue(SendQueueKind.System, () => ran.Enqueue("dropped"), "dropped").Should().BeFalse("the queue is full");

        var discarded = h.Worker.EnqueueReset(() => ran.Enqueue("reset"), "CodecDisconnected");

        discarded.Should().Be(3);
        h.Worker.Health.Snapshot(DateTime.UtcNow).SystemPending.Should().Be(1, "only the reset waits");
        h.Worker.Schedule(); // the first worker "dies" without running
        h.Worker.RestartWorker();
        WaitUntil(() => ran.Contains("reset"));
        ran.Should().Equal(new[] { "reset" }, "the stale actions of the old session never run, before or after the reset");
    }

    [Fact]
    public void Reset_With_Room_In_The_Queue_Keeps_Its_Place()
    {
        var h = Track(new Harness(startFirstWorker: false));
        var ran = new ConcurrentQueue<string>();
        h.Worker.Enqueue(SendQueueKind.System, () => ran.Enqueue("before"), "before");
        h.Worker.EnqueueReset(() => ran.Enqueue("reset"), "SessionLost").Should().Be(0);
        h.Worker.Enqueue(SendQueueKind.System, () => ran.Enqueue("after"), "after");

        h.Worker.Schedule(); // the first worker "dies" without running
        h.Worker.RestartWorker();
        WaitUntil(() => ran.Count == 3);
        ran.Should().Equal("before", "reset", "after");
    }

    [Fact]
    public void A_Replaced_Worker_Never_Takes_Another_Action()
    {
        var h = Track(new Harness(canReconnect: false));
        var oldWrite = Gate();
        var oldThread = 0;
        h.Worker.Enqueue(SendQueueKind.Command, () => { oldThread = Environment.CurrentManagedThreadId; oldWrite.Wait(); }, "stuck");
        h.Worker.Schedule();
        WaitUntil(() => Volatile.Read(ref oldThread) != 0);

        h.Worker.RestartWorker();

        // While the old worker is still blocked, and after it returns: every action runs on a newer worker.
        var threads = new ConcurrentBag<int>();
        void Queue(int count)
        {
            for (var i = 0; i < count; i++)
                h.Worker.Enqueue(SendQueueKind.Command, () => threads.Add(Environment.CurrentManagedThreadId), "c");
            h.Worker.Schedule();
        }

        Queue(50);
        WaitUntil(() => threads.Count == 50);
        oldWrite.Set();
        Queue(50);
        WaitUntil(() => threads.Count == 100);

        threads.Should().NotContain(oldThread);
    }

    [Fact]
    public void A_Replaced_Worker_Passes_On_A_Wake_Up_It_Took()
    {
        // The old worker found the queues empty and is about to wait when it is replaced; then both
        // workers wait on the same auto-reset signal. Whichever gets the next wake-up, the queued
        // action must run promptly, well before the idle recheck.
        var signal = new PausingSignal();
        var h = Track(new Harness(signal: signal));
        h.Worker.Schedule();
        WaitUntil(() => signal.PausedCount == 1);   // the old worker, between its empty check and its wait

        h.Worker.RestartWorker();
        WaitUntil(() => signal.PausedCount == 2);   // the replacement, at the same point

        // Both Schedule calls left a wake-up pending; drop it so each worker really waits. The old
        // worker waits first, then the replacement: waits are woken roughly in order, so the next
        // wake-up goes to the old worker, which is no longer current.
        signal.Reset();
        signal.Resume(0);
        WaitUntil(() => signal.Waiting == 1);
        Thread.Sleep(50);
        signal.Resume(1);
        WaitUntil(() => signal.Waiting == 2);

        var ran = false;
        h.Worker.Enqueue(SendQueueKind.Command, () => Volatile.Write(ref ran, true), "c");
        h.Worker.Schedule();

        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(SendWorker.IdleRecheckMs / 2);
        while (!Volatile.Read(ref ran) && DateTime.UtcNow < deadline)
            Thread.Sleep(5);
        Volatile.Read(ref ran).Should().BeTrue("the wake-up reached a current worker");
    }

    [Fact]
    public void The_Probe_Survives_A_Full_System_Queue_And_A_Session_Reset()
    {
        // A stall before the sync completes can fill the system queue: the probe must still get in,
        // and the reset a reconnect brings must not throw it away with the stale actions.
        var h = Track(new Harness(capacity: 3, startFirstWorker: false));
        for (var i = 0; i < 3; i++)
            h.Worker.Enqueue(SendQueueKind.System, () => { }, "sync step").Should().BeTrue();
        h.Worker.Enqueue(SendQueueKind.System, () => { }, "one too many").Should().BeFalse("the queue is full");

        var probed = 0;
        h.Worker.RequestProbe(() => Interlocked.Increment(ref probed), "SendWatchdogProbe");
        h.Worker.EnqueueReset(() => { }, "CodecDisconnected").Should().Be(3, "the reset clears the stale sync steps");
        h.Worker.Health.Snapshot(DateTime.UtcNow).SystemPending.Should().Be(2, "the probe and the reset both wait");

        h.Worker.Schedule(); // the first worker "dies" without running
        h.Worker.RestartWorker();
        WaitUntil(() => Volatile.Read(ref probed) == 1);
        WaitUntil(() => h.Worker.Health.Snapshot(DateTime.UtcNow).LongestWaiting == null);
    }

    [Fact]
    public void Probe_Requests_Wait_As_One_And_Run_Before_Queued_Actions()
    {
        var h = Track(new Harness(startFirstWorker: false));
        var ran = new ConcurrentQueue<string>();
        h.Worker.Enqueue(SendQueueKind.System, () => ran.Enqueue("sync"), "sync");
        h.Worker.RequestProbe(() => ran.Enqueue("probe"), "SendWatchdogProbe");
        h.Worker.RequestProbe(() => ran.Enqueue("probe again"), "SendWatchdogProbe");
        h.Worker.Health.Snapshot(DateTime.UtcNow).SystemPending.Should().Be(2, "one sync action and one probe");

        h.Worker.Schedule(); // the first worker "dies" without running
        h.Worker.RestartWorker();
        WaitUntil(() => ran.Count == 2);
        ran.Should().Equal("probe", "sync");
    }

    [Fact]
    public void A_Stalled_Worker_Does_Not_Fill_The_Queue_With_Copies_Of_A_Sync_Step()
    {
        // Every JSON document, status reply or text line asks for a sync step. While the worker is
        // stalled, each step waits once, however many messages ask for it.
        var h = Track(new Harness(capacity: 100, startFirstWorker: false));
        // The keys with the session modes production gives them.
        var keys = new (string Key, string? Mode)[]
        {
            ("LoginMessageReceived", "text"), ("TextOutputReceived", "text"), ("InitialStatusMessageReceived", null),
            ("InitialSoftwareVersionMessageReceived", null), ("CheckSyncProgress", null)
        };
        for (var i = 0; i < 1000; i++)
            h.Worker.EnqueueSystemOnce(keys[i % keys.Length].Key, () => { }, keys[i % keys.Length].Mode).Should().BeTrue();

        h.Worker.Health.Snapshot(DateTime.UtcNow).SystemPending.Should().Be(keys.Length);
        h.Worker.Enqueue(SendQueueKind.System, () => { }, "room for more").Should().BeTrue("the queue is far from full");
    }

    [Fact]
    public void A_Sync_Step_Runs_Once_For_Requests_Made_While_It_Waits_And_Again_For_Later_Ones()
    {
        var h = Track(new Harness(startFirstWorker: false));
        var runs = 0;
        for (var i = 0; i < 10; i++)
            h.Worker.EnqueueSystemOnce("JsonMessageReceived", () => Interlocked.Increment(ref runs));

        h.Worker.Schedule(); // the first worker "dies" without running
        h.Worker.RestartWorker();
        WaitUntil(() => Volatile.Read(ref runs) == 1);
        Thread.Sleep(50);
        Volatile.Read(ref runs).Should().Be(1, "one copy served all ten requests");

        // Once it has started, a new request queues it again.
        h.Worker.EnqueueSystemOnce("JsonMessageReceived", () => Interlocked.Increment(ref runs));
        h.Worker.Schedule();
        WaitUntil(() => Volatile.Read(ref runs) == 2);
    }

    [Fact]
    public void A_Step_Requested_While_It_Runs_Is_Queued_Again()
    {
        var h = Track(new Harness());
        var running = Gate();
        var release = Gate();
        var runs = 0;
        void Step()
        {
            if (Interlocked.Increment(ref runs) == 1)
            {
                running.Set();
                release.Wait();
            }
        }

        h.Worker.EnqueueSystemOnce("InitialStatusMessageReceived", Step);
        h.Worker.Schedule();
        running.Wait(Timeout).Should().BeTrue();

        // A message arrives while the step runs: its state may be newer than what the step read.
        h.Worker.EnqueueSystemOnce("InitialStatusMessageReceived", Step);
        h.Worker.Schedule();
        release.Set();
        WaitUntil(() => Volatile.Read(ref runs) == 2);
    }

    [Fact]
    public void A_Step_Dropped_By_A_Full_Queue_Is_Not_Marked_Waiting()
    {
        var h = Track(new Harness(capacity: 1, startFirstWorker: false));
        h.Worker.Enqueue(SendQueueKind.System, () => { }, "filler").Should().BeTrue();

        h.Worker.EnqueueSystemOnce("FeedbackRegistered", () => { }).Should().BeFalse("the queue is full");
        // Still not waiting, so the next request tries again rather than counting on a copy that isn't there.
        h.Worker.EnqueueSystemOnce("FeedbackRegistered", () => { }).Should().BeFalse();
        h.Worker.Health.Snapshot(DateTime.UtcNow).SystemPending.Should().Be(1);
    }

    [Fact]
    public void A_Step_Asked_For_After_A_Reset_Is_Queued_After_It()
    {
        // A copy waiting from before the reset runs before it, against the old session; the step must
        // also run after the reset for the new session.
        var h = Track(new Harness(startFirstWorker: false));
        var ran = new ConcurrentQueue<string>();
        h.Worker.EnqueueSystemOnce("JsonMessageReceived", () => ran.Enqueue("step"));
        h.Worker.EnqueueReset(() => ran.Enqueue("reset"), "CodecDisconnected");
        h.Worker.EnqueueSystemOnce("JsonMessageReceived", () => ran.Enqueue("step"));

        h.Worker.Schedule(); // the first worker "dies" without running
        h.Worker.RestartWorker();
        WaitUntil(() => ran.Count == 3);
        ran.Should().Equal("step", "reset", "step");
    }

    [Fact]
    public void Steps_Keep_Their_Order_Across_A_Session_Mode_Change()
    {
        // JSON, then text output (the session left JSON mode), then JSON again: the second JSON must
        // run after the text step, or JSON mode is left unset.
        var h = Track(new Harness(startFirstWorker: false));
        var ran = new ConcurrentQueue<string>();
        h.Worker.EnqueueSystemOnce("JsonMessageReceived", () => ran.Enqueue("json"), sessionMode: "json");
        h.Worker.EnqueueSystemOnce("TextOutputReceived", () => ran.Enqueue("text"), sessionMode: "text");
        h.Worker.EnqueueSystemOnce("JsonMessageReceived", () => ran.Enqueue("json"), sessionMode: "json");

        h.Worker.Schedule(); // the first worker "dies" without running
        h.Worker.RestartWorker();
        WaitUntil(() => ran.Count == 3);
        ran.Should().Equal("json", "text", "json");
    }

    [Fact]
    public void Steps_Still_Coalesce_Across_Steps_That_Are_Not_Mode_Changes()
    {
        // JSON documents that are also status replies: JSON, status, JSON, status... The mode doesn't
        // change, so each step still waits once.
        var h = Track(new Harness(startFirstWorker: false));
        for (var i = 0; i < 200; i++)
        {
            h.Worker.EnqueueSystemOnce("JsonMessageReceived", () => { }, sessionMode: "json").Should().BeTrue();
            h.Worker.EnqueueSystemOnce("InitialStatusMessageReceived", () => { }).Should().BeTrue();
        }

        h.Worker.Health.Snapshot(DateTime.UtcNow).SystemPending.Should().Be(2);
    }

    [Fact]
    public void An_Older_Copy_Does_Not_Release_A_Newer_Copys_Mark()
    {
        // K, reset, K: when the old K starts it must leave the new K's mark alone, so a request made
        // while it runs is served by the new K instead of queuing a third copy.
        var h = Track(new Harness(startFirstWorker: false));
        var oldRunning = Gate();
        var releaseOld = Gate();
        var runs = 0;
        h.Worker.EnqueueSystemOnce("JsonMessageReceived", () =>
        {
            if (Interlocked.Increment(ref runs) == 1)
            {
                oldRunning.Set();
                releaseOld.Wait();
            }
        });
        h.Worker.EnqueueReset(() => { }, "CodecDisconnected");
        h.Worker.EnqueueSystemOnce("JsonMessageReceived", () => Interlocked.Increment(ref runs));

        h.Worker.Schedule(); // the first worker "dies" without running
        h.Worker.RestartWorker();
        oldRunning.Wait(Timeout).Should().BeTrue();

        // The old copy is running; the reset and the new copy wait.
        h.Worker.EnqueueSystemOnce("JsonMessageReceived", () => Interlocked.Increment(ref runs));
        h.Worker.Health.Snapshot(DateTime.UtcNow).SystemPending.Should().Be(2, "the reset and the new copy, no third copy");

        releaseOld.Set();
        WaitUntil(() => Volatile.Read(ref runs) == 2);
        Thread.Sleep(50);
        Volatile.Read(ref runs).Should().Be(2);
    }

    [Fact]
    public void Steps_Of_The_Same_Session_Mode_Still_Collapse_When_They_Alternate()
    {
        // Text mode with echo on and a stalled worker: every poll brings an echoed poll line (login
        // echo) and text output. Both mean "text mode", so they must not fence each other off.
        var h = Track(new Harness(startFirstWorker: false));
        for (var poll = 0; poll < 100; poll++)
        {
            h.Worker.EnqueueSystemOnce("LoginMessageReceived", () => { }, sessionMode: "text").Should().BeTrue();
            h.Worker.EnqueueSystemOnce("TextOutputReceived", () => { }, sessionMode: "text").Should().BeTrue();
        }

        h.Worker.Health.Snapshot(DateTime.UtcNow).SystemPending.Should().Be(2);
    }

    [Fact]
    public void Enqueue_Reports_Its_Result_Under_The_Lock()
    {
        var h = Track(new Harness(capacity: 1, startFirstWorker: false));
        var results = new List<bool>();

        h.Worker.Enqueue(SendQueueKind.Command, () => { }, "a", results.Add);
        h.Worker.Enqueue(SendQueueKind.Command, () => { }, "b", results.Add);

        results.Should().Equal(true, false);
    }

    // ---- helpers -------------------------------------------------------------------------------

    private ManualResetEventSlim Gate()
    {
        var gate = new ManualResetEventSlim(false);
        _gates.Add(gate);
        return gate;
    }

    private static void WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("condition not met in " + Timeout);
            Thread.Sleep(5);
        }
    }

    /// <summary>One watchdog check: what it saw and what it did.</summary>
    private sealed record Check(SendStallTransition Transition, SendStallCause Cause, SendStallAction? Action, int Dropped, string? Busy);

    /// <summary>A worker on .NET threads and queues, with the watchdog's steps from CiscoCodec.CheckSendPath.</summary>
    private sealed class Harness
    {
        private readonly bool _canReconnect;
        private readonly bool _startFirstWorker;
        private readonly SendStallDetector _detector = new(StallAfter, TimeSpan.Zero);
        private int _starts;
        private volatile bool _receivingJson;

        private readonly ConcurrentBag<Thread> _threads = new();

        public Harness(int capacity = 100, bool canReconnect = true, bool startFirstWorker = true, IWorkerSignal? signal = null)
        {
            _canReconnect = canReconnect;
            _startFirstWorker = startFirstWorker;
            Worker = new SendWorker(new BoundedQueue(capacity), new BoundedQueue(capacity), signal ?? new AutoSignal(), StartThread,
                () => _receivingJson, new SendQueueHealth(), (label, ex) => Errors.Add(label + ": " + ex.Message));
        }

        public void JoinThreads(TimeSpan timeout)
        {
            foreach (var thread in _threads)
                thread.Join(timeout);
        }

        public SendWorker Worker { get; }

        public ConcurrentBag<string> Errors { get; } = new();

        public bool ReceivingJson
        {
            get => _receivingJson;
            set => _receivingJson = value;
        }

        public Action OnReconnect { get; set; } = () => { };

        /// <summary>Stands in for CiscoCodec's send lock: a write holds it while it runs.</summary>
        public object SendLock { get; } = new();

        public SendStallDetector Detector => _detector;

        public int Starts => Volatile.Read(ref _starts);

        private void StartThread(Action loop)
        {
            if (Interlocked.Increment(ref _starts) == 1 && !_startFirstWorker)
                return; // this worker "died" before taking anything

            var thread = new Thread(() => loop()) { IsBackground = true };
            _threads.Add(thread);
            thread.Start();
        }

        /// <summary>The watchdog's steps: evaluate, and recover once per interval while stalled.</summary>
        public Check Check()
        {
            var snapshot = Worker.Health.Snapshot(DateTime.UtcNow);
            var transition = _detector.Evaluate(snapshot, out var cause, out _);
            if (!_detector.IsStalled || !_detector.TryAcquireRecovery(snapshot.TakenUtc))
                return new Check(transition, cause, null, 0, snapshot.BusyWith);

            var dropped = Worker.DrainCommands(5, out _);
            var action = SendStallRecovery.Choose(cause, _canReconnect);
            switch (action)
            {
                case SendStallAction.RestartWorker:
                    Worker.RestartWorker();
                    break;
                case SendStallAction.Reconnect:
                    OnReconnect();
                    break;
                case SendStallAction.KeepWorker:
                    break; // RS-232: nothing to reconnect, and a new worker would block the same way
            }

            // As CiscoCodec does: a probe whose completion confirms the recovery. It waits for the
            // send lock, like a real command (CiscoCodec.WaitForSendPath), then counts itself.
            Worker.RequestProbe(() =>
            {
                lock (SendLock) { }
                Worker.Health.OnProbeCompleted();
            }, "SendWatchdogProbe");
            Worker.Schedule();

            return new Check(transition, cause, action, dropped, snapshot.BusyWith);
        }

        /// <summary>Runs the watchdog every 50 ms until a check matches.</summary>
        public Check WatchUntil(Func<Check, bool> match)
        {
            var deadline = DateTime.UtcNow + Timeout;
            while (true)
            {
                var check = Check();
                if (match(check))
                    return check;
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("watchdog never saw the expected state; last: " + check);
                Thread.Sleep(50);
            }
        }
    }

    /// <summary>A bounded, non-blocking queue like CrestronQueue with TryToEnqueue.</summary>
    private sealed class BoundedQueue : IActionQueue
    {
        private readonly ConcurrentQueue<QueuedAction> _queue = new();
        private readonly int _capacity;
        private int _count;

        public BoundedQueue(int capacity) => _capacity = capacity;

        public bool TryEnqueue(QueuedAction action)
        {
            if (Interlocked.Increment(ref _count) > _capacity)
            {
                Interlocked.Decrement(ref _count);
                return false;
            }
            _queue.Enqueue(action);
            return true;
        }

        public bool TryDequeue(out QueuedAction? action)
        {
            if (!_queue.TryDequeue(out action))
                return false;
            Interlocked.Decrement(ref _count);
            return true;
        }

        public void Clear()
        {
            while (TryDequeue(out _)) { }
        }
    }

    /// <summary>An auto-reset signal like CEvent.</summary>
    private class AutoSignal : IWorkerSignal
    {
        private readonly AutoResetEvent _event = new(false);
        private int _waiting;

        /// <summary>How many threads are waiting on the event right now.</summary>
        public int Waiting => Volatile.Read(ref _waiting);

        public void Set() => _event.Set();

        /// <summary>Drops a pending wake-up (test setup only).</summary>
        public void Reset() => _event.Reset();

        public void Wait() => Wait(System.Threading.Timeout.Infinite);

        public void Wait(int timeoutMs)
        {
            BeforeWait();
            Interlocked.Increment(ref _waiting);
            try { _event.WaitOne(timeoutMs); }
            finally { Interlocked.Decrement(ref _waiting); }
        }

        protected virtual void BeforeWait() { }
    }

    /// <summary>
    /// Pauses the first two threads that wait, each before it waits: workers caught between their empty
    /// check and their wait. Each is resumed on its own, so the test decides who waits first.
    /// </summary>
    private sealed class PausingSignal : AutoSignal
    {
        private readonly ManualResetEventSlim[] _resume = { new(false), new(false) };
        private int _paused;

        public int PausedCount => Math.Min(Volatile.Read(ref _paused), _resume.Length);

        public void Resume(int index) => _resume[index].Set();

        protected override void BeforeWait()
        {
            var index = Interlocked.Increment(ref _paused) - 1;
            if (index < _resume.Length)
                _resume[index].Wait();
        }
    }
}
