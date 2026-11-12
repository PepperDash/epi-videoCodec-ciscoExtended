#nullable enable
using System;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>An action for the send worker, with a short description so a stuck worker can say what it's stuck on.</summary>
    public sealed class QueuedAction
    {
        public QueuedAction(Action run, string what)
        {
            Run = run ?? throw new ArgumentNullException(nameof(run));
            What = what ?? string.Empty;
        }

        public Action Run { get; }

        public string What { get; }
    }

    /// <summary>A bounded, thread-safe FIFO that never blocks. On the processor: a CrestronQueue.</summary>
    public interface IActionQueue
    {
        /// <summary>Adds <paramref name="action"/>; false when the queue is full.</summary>
        bool TryEnqueue(QueuedAction action);

        bool TryDequeue(out QueuedAction? action);

        void Clear();
    }

    /// <summary>The worker's wake-up signal (auto-reset). On the processor: a CEvent.</summary>
    public interface IWorkerSignal
    {
        void Set();

        /// <summary>Waits until <see cref="Set"/>.</summary>
        void Wait();

        /// <summary>Waits until <see cref="Set"/> or <paramref name="timeoutMs"/>.</summary>
        void Wait(int timeoutMs);
    }

    /// <summary>
    /// The codec's send worker (#194): one thread that takes system actions (sync steps, always
    /// first) and then commands off two queues and runs them, holding off while a JSON response is
    /// being received. It records what it does in <see cref="SendQueueHealth"/> and can be restarted
    /// or have its commands drained by the send watchdog.
    /// <para>
    /// Threads, queues and the signal are passed in, so the same code runs on the processor (Crestron
    /// Thread, CrestronQueue, CEvent) and in tests (.NET threads and collections).
    /// </para>
    /// </summary>
    /// <remarks>
    /// Has no Crestron dependencies, so the test project compiles and tests it directly.
    /// </remarks>
    public sealed class SendWorker
    {
        private const int Idle = 0;
        private const int Processing = 1;

        /// <summary>How often the worker re-checks while holding off for a JSON response.</summary>
        public const int JsonHoldPollMs = 20;

        /// <summary>
        /// The longest an idle worker sleeps before checking the queues again, so a lost wake-up can
        /// only delay work, never leave it queued for good.
        /// </summary>
        public const int IdleRecheckMs = 5000;

        private readonly IActionQueue _system;
        private readonly IActionQueue _command;
        private readonly IWorkerSignal _signal;
        private readonly Action<Action> _startThread;
        private readonly Func<bool> _isReceivingJson;
        private readonly Action<string, Exception> _onActionError;
        private readonly Func<DateTime> _utcNow;

        private int _state = Idle;

        // Each worker runs only while the generation it started with is current: after RestartWorker
        // an old worker that wakes up again exits, so two never take actions together.
        private int _generation;

        // Every change to the queues, the generation and the worker state happens under this lock,
        // together with its SendQueueHealth update, so the bookkeeping always matches the queues and a
        // replaced worker can't take another action. Actions themselves run outside it.
        private readonly object _gate = new object();

        /// <param name="system">System actions; always taken first.</param>
        /// <param name="command">Commands for the codec.</param>
        /// <param name="signal">Wakes the worker when something is queued.</param>
        /// <param name="startThread">Runs the given loop on a new thread.</param>
        /// <param name="isReceivingJson">True while a JSON response is being received (the worker holds off).</param>
        /// <param name="health">Where the worker records what it does.</param>
        /// <param name="onActionError">Called when an action throws: (label, exception).</param>
        /// <param name="utcNow">The clock; defaults to <see cref="DateTime.UtcNow"/>.</param>
        public SendWorker(IActionQueue system, IActionQueue command, IWorkerSignal signal, Action<Action> startThread,
            Func<bool> isReceivingJson, SendQueueHealth health, Action<string, Exception> onActionError,
            Func<DateTime>? utcNow = null)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
            _command = command ?? throw new ArgumentNullException(nameof(command));
            _signal = signal ?? throw new ArgumentNullException(nameof(signal));
            _startThread = startThread ?? throw new ArgumentNullException(nameof(startThread));
            _isReceivingJson = isReceivingJson ?? throw new ArgumentNullException(nameof(isReceivingJson));
            Health = health ?? throw new ArgumentNullException(nameof(health));
            _onActionError = onActionError ?? throw new ArgumentNullException(nameof(onActionError));
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        /// <summary>What the send path is doing, for codecperf and the send watchdog.</summary>
        public SendQueueHealth Health { get; }

        /// <summary>The current worker generation (it changes on <see cref="RestartWorker"/>).</summary>
        public int Generation
        {
            get { lock (_gate) return _generation; }
        }

        /// <summary>
        /// Puts <paramref name="action"/> on a queue without waking the worker (call <see cref="Schedule"/>).
        /// False when the queue is full: the action was dropped. <paramref name="onResult"/>, if given,
        /// runs under the same lock with the result, so a caller's own bookkeeping of drops stays in the
        /// order the queue saw them; it must not block or log.
        /// </summary>
        public bool Enqueue(SendQueueKind kind, Action action, string what, Action<bool>? onResult = null)
        {
            lock (_gate)
            {
                Health.OnEnqueued(kind, _utcNow());
                var accepted = QueueOf(kind).TryEnqueue(new QueuedAction(action, what));
                if (!accepted)
                    Health.OnEnqueueRejected(kind);
                onResult?.Invoke(accepted);
                return accepted;
            }
        }

        /// <summary>
        /// Queues a system action that must not be lost, such as a session reset. If the system queue
        /// is full, the actions waiting in it are dropped first (they belong to the session being
        /// reset, and must not run after the reset either) and their count returned; otherwise 0.
        /// Never blocks.
        /// </summary>
        public int EnqueueReset(Action action, string what)
        {
            lock (_gate)
            {
                var discarded = 0;
                var queued = new QueuedAction(action, what);
                if (!_system.TryEnqueue(queued))
                {
                    while (_system.TryDequeue(out _))
                        discarded++;
                    Health.OnCleared(SendQueueKind.System);
                    _system.TryEnqueue(queued); // the queue is empty now
                }

                Health.OnEnqueued(SendQueueKind.System, _utcNow());
                return discarded;
            }
        }

        /// <summary>Starts the worker if it isn't running, and wakes it.</summary>
        public void Schedule()
        {
            int? start = null;
            lock (_gate)
            {
                if (_state == Idle)
                {
                    _state = Processing;
                    start = _generation;
                }
            }

            if (start != null)
            {
                var generation = start.Value;
                _startThread(() => Run(generation));
            }

            _signal.Set();
        }

        /// <summary>
        /// Starts a new worker when the current one has stopped taking actions off the queues. The old
        /// one, if it ever returns, exits without taking another action or touching the new one's state.
        /// </summary>
        public void RestartWorker()
        {
            lock (_gate)
            {
                _generation++;
                _state = Idle;
                Health.OnWorkerReplaced(); // its action may never return: not a completion
            }

            Schedule();
        }

        /// <summary>Stops the worker (program stopping).</summary>
        public void Stop()
        {
            lock (_gate)
                _state = Idle;
            _signal.Set();
        }

        /// <summary>Empties the command queue without running anything.</summary>
        public void ClearCommands()
        {
            lock (_gate)
            {
                _command.Clear();
                Health.OnCleared(SendQueueKind.Command);
            }
        }

        /// <summary>
        /// Empties the command queue without running anything. Returns how many were dropped and
        /// describes the first <paramref name="describe"/>.
        /// </summary>
        public int DrainCommands(int describe, out List<string> described)
        {
            described = new List<string>();
            var dropped = 0;
            lock (_gate)
            {
                while (_command.TryDequeue(out var action))
                {
                    dropped++;
                    if (action != null && described.Count < describe)
                        described.Add(action.What);
                }

                Health.OnCleared(SendQueueKind.Command);
            }

            return dropped;
        }

        private IActionQueue QueueOf(SendQueueKind kind) => kind == SendQueueKind.Command ? _command : _system;

        private enum Next
        {
            Action,
            Empty,
            Superseded
        }

        /// <summary>
        /// Under the lock: checks this worker is still current, then takes the next action (system
        /// first) and records its start, or records that both queues are empty.
        /// </summary>
        private Next TakeNext(int generation, out QueuedAction? action, out SendQueueKind kind)
        {
            action = null;
            kind = SendQueueKind.System;
            lock (_gate)
            {
                if (_state != Processing || generation != _generation)
                    return Next.Superseded;

                if (_system.TryDequeue(out action) && action != null)
                    kind = SendQueueKind.System;
                else if (_command.TryDequeue(out action) && action != null)
                    kind = SendQueueKind.Command;
                else
                {
                    // Both queues are empty: whatever the bookkeeping says, nothing is waiting.
                    Health.OnQueuesEmpty();
                    return Next.Empty;
                }

                Health.OnStarted(kind, action.What, _utcNow());
                return Next.Action;
            }
        }

        /// <summary>
        /// A replaced worker exiting may have taken a wake-up meant for its replacement (both wait on
        /// the same auto-reset signal): it passes one on before it goes.
        /// </summary>
        private void PassWakeOn() => _signal.Set();

        private void Run(int generation)
        {
            while (true)
            {
                // Hold off sending anything to the codec while a JSON response is currently being
                // accumulated - the codec can echo in-flight commands back into the response stream,
                // which corrupts the JSON buffer being parsed.
                if (_isReceivingJson())
                {
                    lock (_gate)
                    {
                        if (_state != Processing || generation != _generation)
                        {
                            PassWakeOn();
                            return;
                        }
                        Health.OnJsonHold(_utcNow());
                    }
                    _signal.Wait(JsonHoldPollMs);
                    continue;
                }

                Health.OnJsonHoldEnded();

                switch (TakeNext(generation, out var next, out var kind))
                {
                    case Next.Superseded:
                        PassWakeOn();
                        return;
                    case Next.Empty:
                        _signal.Wait(IdleRecheckMs);
                        continue;
                }

                try
                {
                    next!.Run();
                }
                catch (Exception ex)
                {
                    _onActionError(kind == SendQueueKind.System ? "system action" : "user action", ex);
                }
                finally
                {
                    // A worker replaced by RestartWorker while blocked here must not clear the busy
                    // state of the worker that replaced it, which would hide that one being blocked.
                    lock (_gate)
                    {
                        if (generation == _generation)
                            Health.OnFinished(_utcNow());
                    }
                }
            }
        }
    }
}
