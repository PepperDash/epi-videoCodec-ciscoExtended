#nullable enable
using System;
using System.Globalization;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>The sync worker's two queues.</summary>
    public enum SendQueueKind
    {
        /// <summary>Sync steps and session handling; always taken first.</summary>
        System,

        /// <summary>Commands for the codec (EnqueueCommand).</summary>
        Command
    }

    /// <summary>
    /// What the codec's send path is doing, for <c>codecperf</c> and the send watchdog (#194): how many
    /// actions wait in each queue and since when nothing has been taken off it, what the worker is
    /// running and for how long, how long it has held off for an incoming JSON response, and when a
    /// command last went out. Recording only: nothing here changes how the plugin sends.
    /// Time is passed in, so it can be tested without a clock. Thread-safe.
    /// </summary>
    /// <remarks>
    /// Has no Crestron dependencies, so the test project compiles and tests it directly.
    /// </remarks>
    public sealed class SendQueueHealth
    {
        private readonly object _sync = new object();
        private readonly QueueState _system = new QueueState();
        private readonly QueueState _command = new QueueState();

        private DateTime? _busySinceUtc;
        private string? _busyWith;
        private DateTime? _jsonHoldSinceUtc;
        private DateTime? _lastJsonHoldUtc;
        private DateTime? _lastSentUtc;
        private DateTime? _lastCompletedUtc;

        /// <summary>
        /// An action is about to be queued. Call it <em>before</em> putting the action on the queue (and
        /// <see cref="OnEnqueueRejected"/> if that fails): the worker can take the action off and call
        /// <see cref="OnStarted"/> before the queuing thread gets here again, and counting afterwards
        /// would then leave one phantom action waiting for good.
        /// </summary>
        public void OnEnqueued(SendQueueKind kind, DateTime nowUtc)
        {
            lock (_sync)
            {
                var queue = Queue(kind);
                if (queue.Pending == 0)
                    queue.WaitingSinceUtc = nowUtc;
                queue.Pending++;
            }
        }

        /// <summary>The action counted by <see cref="OnEnqueued"/> didn't make it onto the queue (it was full).</summary>
        public void OnEnqueueRejected(SendQueueKind kind)
        {
            lock (_sync)
            {
                var queue = Queue(kind);
                queue.Pending = Math.Max(0, queue.Pending - 1);
                if (queue.Pending == 0)
                    queue.WaitingSinceUtc = null;
            }
        }

        /// <summary>
        /// The worker found both queues empty. Resets the counts, so a miscount from any race between
        /// the queues and this bookkeeping clears within one cycle instead of reading as a stall.
        /// </summary>
        public void OnQueuesEmpty()
        {
            lock (_sync)
            {
                foreach (var queue in new[] { _system, _command })
                {
                    queue.Pending = 0;
                    queue.WaitingSinceUtc = null;
                }
            }
        }

        /// <summary>The worker took an action off the queue and is about to run it.</summary>
        public void OnStarted(SendQueueKind kind, string? what, DateTime nowUtc)
        {
            lock (_sync)
            {
                var queue = Queue(kind);
                queue.Pending = Math.Max(0, queue.Pending - 1);
                queue.LastDequeuedUtc = nowUtc;
                // Progress: anything still waiting is measured from now.
                queue.WaitingSinceUtc = queue.Pending > 0 ? nowUtc : (DateTime?)null;

                _busySinceUtc = nowUtc;
                _busyWith = what;
            }
        }

        /// <summary>The worker finished the action it was running.</summary>
        public void OnFinished() => OnFinished(DateTime.UtcNow);

        /// <summary>The worker finished the action it was running, at <paramref name="nowUtc"/>.</summary>
        public void OnFinished(DateTime nowUtc)
        {
            lock (_sync)
            {
                _busySinceUtc = null;
                _busyWith = null;
                _lastCompletedUtc = nowUtc;
            }
        }

        /// <summary>
        /// The worker was replaced while it may still be running an action. Clears the busy state
        /// without counting it as completed: the action may never return.
        /// </summary>
        public void OnWorkerReplaced()
        {
            lock (_sync)
            {
                _busySinceUtc = null;
                _busyWith = null;
            }
        }

        /// <summary>A queue was emptied without running its actions.</summary>
        public void OnCleared(SendQueueKind kind)
        {
            lock (_sync)
            {
                var queue = Queue(kind);
                queue.Pending = 0;
                queue.WaitingSinceUtc = null;
            }
        }

        /// <summary>The worker is holding off while a JSON response is being received.</summary>
        public void OnJsonHold(DateTime nowUtc)
        {
            lock (_sync)
            {
                if (_jsonHoldSinceUtc == null)
                    _jsonHoldSinceUtc = nowUtc;
                _lastJsonHoldUtc = nowUtc;
            }
        }

        /// <summary>The worker is no longer holding off for a JSON response.</summary>
        public void OnJsonHoldEnded()
        {
            lock (_sync)
                _jsonHoldSinceUtc = null;
        }

        /// <summary>Text was written to the connection.</summary>
        public void OnSent(DateTime nowUtc)
        {
            lock (_sync)
                _lastSentUtc = nowUtc;
        }

        /// <summary>A consistent copy of the current state.</summary>
        public SendQueueSnapshot Snapshot(DateTime nowUtc)
        {
            lock (_sync)
            {
                return new SendQueueSnapshot(
                    nowUtc,
                    _system.Pending, Since(_system.WaitingSinceUtc, nowUtc), Since(_system.LastDequeuedUtc, nowUtc),
                    _command.Pending, Since(_command.WaitingSinceUtc, nowUtc), Since(_command.LastDequeuedUtc, nowUtc),
                    Since(_busySinceUtc, nowUtc), _busyWith,
                    Since(_jsonHoldSinceUtc, nowUtc),
                    Since(_lastSentUtc, nowUtc),
                    Since(_lastJsonHoldUtc, nowUtc),
                    Since(_lastCompletedUtc, nowUtc));
            }
        }

        private QueueState Queue(SendQueueKind kind) => kind == SendQueueKind.System ? _system : _command;

        private static TimeSpan? Since(DateTime? thenUtc, DateTime nowUtc) =>
            thenUtc == null ? (TimeSpan?)null : (nowUtc > thenUtc.Value ? nowUtc - thenUtc.Value : TimeSpan.Zero);

        private sealed class QueueState
        {
            public int Pending;
            public DateTime? WaitingSinceUtc;
            public DateTime? LastDequeuedUtc;
        }
    }

    /// <summary>A copy of <see cref="SendQueueHealth"/> at one moment. Durations are null when not applicable.</summary>
    public sealed class SendQueueSnapshot
    {
        public SendQueueSnapshot(DateTime takenUtc,
            int systemPending, TimeSpan? systemWaiting, TimeSpan? systemSinceDequeue,
            int commandPending, TimeSpan? commandWaiting, TimeSpan? commandSinceDequeue,
            TimeSpan? busyFor, string? busyWith, TimeSpan? jsonHoldFor, TimeSpan? sinceSent,
            TimeSpan? sinceJsonHold = null, TimeSpan? sinceCompleted = null)
        {
            SinceJsonHold = sinceJsonHold;
            SinceCompleted = sinceCompleted;
            TakenUtc = takenUtc;
            SystemPending = systemPending;
            SystemWaiting = systemWaiting;
            SystemSinceDequeue = systemSinceDequeue;
            CommandPending = commandPending;
            CommandWaiting = commandWaiting;
            CommandSinceDequeue = commandSinceDequeue;
            BusyFor = busyFor;
            BusyWith = busyWith;
            JsonHoldFor = jsonHoldFor;
            SinceSent = sinceSent;
        }

        public DateTime TakenUtc { get; }

        /// <summary>System actions waiting.</summary>
        public int SystemPending { get; }

        /// <summary>How long system actions have waited with none taken off the queue (null when empty).</summary>
        public TimeSpan? SystemWaiting { get; }

        public TimeSpan? SystemSinceDequeue { get; }

        /// <summary>Commands waiting.</summary>
        public int CommandPending { get; }

        /// <summary>How long commands have waited with none taken off the queue (null when empty).</summary>
        public TimeSpan? CommandWaiting { get; }

        public TimeSpan? CommandSinceDequeue { get; }

        /// <summary>How long the worker has been running its current action (null when idle).</summary>
        public TimeSpan? BusyFor { get; }

        /// <summary>A short description of the action being run.</summary>
        public string? BusyWith { get; }

        /// <summary>How long the worker has held off for an incoming JSON response (null when not).</summary>
        public TimeSpan? JsonHoldFor { get; }

        /// <summary>Time since text was last written to the connection (null when never).</summary>
        public TimeSpan? SinceSent { get; }

        /// <summary>
        /// Time since the worker last held off for a JSON response (null when never). A hold that keeps
        /// switching on and off can read as "not holding" at any one moment; this shows it was recent.
        /// </summary>
        public TimeSpan? SinceJsonHold { get; }

        /// <summary>Time since the worker last finished an action (null when never).</summary>
        public TimeSpan? SinceCompleted { get; }

        /// <summary>The longest any queue has waited with no progress (null when both are empty).</summary>
        public TimeSpan? LongestWaiting =>
            SystemWaiting == null ? CommandWaiting
            : CommandWaiting == null ? SystemWaiting
            : (SystemWaiting > CommandWaiting ? SystemWaiting : CommandWaiting);

        /// <summary>
        /// One line for the console, for example
        /// <c>commands=3 waiting 12.0 s (last taken 12.0 s ago) system=0 worker=busy 11.8 s on 'xCommand Camera FactoryReset ...' jsonHold=- lastSent=12.1 s ago</c>.
        /// </summary>
        public string Format()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "commands={0}{1} (last taken {2}) system={3}{4} (last taken {5}) worker={6} jsonHold={7} lastSent={8}",
                CommandPending, Waiting(CommandWaiting), Ago(CommandSinceDequeue),
                SystemPending, Waiting(SystemWaiting), Ago(SystemSinceDequeue),
                BusyFor == null ? "idle" : "busy " + Seconds(BusyFor.Value) + (BusyWith == null ? "" : " on '" + BusyWith + "'"),
                JsonHoldFor == null ? "-" : Seconds(JsonHoldFor.Value),
                Ago(SinceSent));
        }

        private static string Waiting(TimeSpan? waiting) => waiting == null ? "" : " waiting " + Seconds(waiting.Value);

        private static string Ago(TimeSpan? since) => since == null ? "never" : Seconds(since.Value) + " ago";

        private static string Seconds(TimeSpan span) => span.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s";
    }
}
