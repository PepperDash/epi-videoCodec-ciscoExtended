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

    public void Dispose()
    {
        // Release anything a test left blocked, so its threads end.
        foreach (var gate in _gates)
            gate.Set();
    }

    [Fact]
    public void Runs_System_Actions_Before_Commands_In_Order()
    {
        var h = new Harness();
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
        var h = new Harness();
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
        var h = new Harness(capacity: 5);
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
        var h = new Harness(startFirstWorker: false);
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
        var h = new Harness();
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
    public void Blocked_Write_Over_Serial_Gets_A_New_Worker_And_The_Old_One_Bows_Out()
    {
        var h = new Harness(canReconnect: false);
        var write = Gate();
        var ran = new ConcurrentQueue<string>();
        h.Worker.Enqueue(SendQueueKind.Command, () => write.Wait(), "stuck");
        h.Worker.Schedule();

        var check = h.WatchUntil(c => c.Cause != SendStallCause.None);
        check.Action.Should().Be(SendStallAction.RestartWorker, "there is no session to reconnect over RS-232");

        h.Worker.Enqueue(SendQueueKind.Command, () => ran.Enqueue("after restart"), "after restart");
        h.Worker.Schedule();
        WaitUntil(() => ran.Contains("after restart"));

        // The old worker's write returns: it must not run anything else, nor clear the new one's state.
        var busyBlocker = Gate();
        h.Worker.Enqueue(SendQueueKind.Command, () => busyBlocker.Wait(), "new worker busy");
        h.Worker.Schedule();
        WaitUntil(() => h.Worker.Health.Snapshot(DateTime.UtcNow).BusyWith == "new worker busy");
        write.Set();
        Thread.Sleep(100);
        h.Worker.Health.Snapshot(DateTime.UtcNow).BusyWith.Should().Be("new worker busy");
        h.Starts.Should().Be(2);
    }

    [Fact]
    public void Json_Response_That_Never_Ends_Is_Detected_And_A_Reconnect_Clears_It()
    {
        var h = new Harness { ReceivingJson = true };
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
        var h = new Harness();
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
        var h = new Harness(capacity: 1000);
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
        });

        chaos.Start();
        producers.ForEach(t => t.Start());
        producers.ForEach(t => t.Join(Timeout).Should().BeTrue());
        Volatile.Write(ref stop, true);
        chaos.Join(Timeout).Should().BeTrue();

        h.Worker.Schedule();
        WaitUntil(() =>
        {
            var s = h.Worker.Health.Snapshot(DateTime.UtcNow);
            return s.BusyFor == null && s.LongestWaiting == null && s.SystemPending == 0 && s.CommandPending == 0;
        });

        Thread.Sleep(StallAfter + StallAfter);
        h.Check().Cause.Should().Be(SendStallCause.None, "an idle worker with empty queues is not stalled");
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

        public Harness(int capacity = 100, bool canReconnect = true, bool startFirstWorker = true)
        {
            _canReconnect = canReconnect;
            _startFirstWorker = startFirstWorker;
            Worker = new SendWorker(new BoundedQueue(capacity), new BoundedQueue(capacity), new AutoSignal(), StartThread,
                () => _receivingJson, new SendQueueHealth(), (label, ex) => Errors.Add(label + ": " + ex.Message));
        }

        public SendWorker Worker { get; }

        public ConcurrentBag<string> Errors { get; } = new();

        public bool ReceivingJson
        {
            get => _receivingJson;
            set => _receivingJson = value;
        }

        public Action OnReconnect { get; set; } = () => { };

        public int Starts => Volatile.Read(ref _starts);

        private void StartThread(Action loop)
        {
            if (Interlocked.Increment(ref _starts) == 1 && !_startFirstWorker)
                return; // this worker "died" before taking anything

            new Thread(() => loop()) { IsBackground = true }.Start();
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
            if (action == SendStallAction.RestartWorker)
                Worker.RestartWorker();
            else
                OnReconnect();

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
    private sealed class AutoSignal : IWorkerSignal
    {
        private readonly AutoResetEvent _event = new(false);

        public void Set() => _event.Set();

        public void Wait() => _event.WaitOne();

        public void Wait(int timeoutMs) => _event.WaitOne(timeoutMs);
    }
}
