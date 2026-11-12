#nullable enable
using System;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>Result of <see cref="SendStallDetector.Evaluate"/>.</summary>
    public enum SendStallTransition
    {
        /// <summary>No change.</summary>
        None,

        /// <summary>The send path just stalled.</summary>
        Stalled,

        /// <summary>The send path is moving again after a stall.</summary>
        Recovered
    }

    /// <summary>Why the send path is stalled, which decides how to recover.</summary>
    public enum SendStallCause
    {
        /// <summary>Not stalled.</summary>
        None,

        /// <summary>The worker has been running one action too long, e.g. a write that never returns.</summary>
        WorkerBlocked,

        /// <summary>The worker has held off too long for a JSON response that keeps arriving or never ends.</summary>
        JsonHold,

        /// <summary>Actions wait, but the worker is neither running one nor holding off: it isn't running.</summary>
        WorkerNotRunning
    }

    /// <summary>How the send watchdog recovers a stall.</summary>
    public enum SendStallAction
    {
        /// <summary>Start a new send worker.</summary>
        RestartWorker,

        /// <summary>End the session and connect again (also ends a blocked write and resets the receive state).</summary>
        Reconnect,

        /// <summary>
        /// Keep the current worker (RS-232): there is no session to reconnect, and a new worker would
        /// block on the same send lock as a blocked write. A JSON hold is reset separately.
        /// </summary>
        KeepWorker
    }

    /// <summary>Chooses how to recover a stall (#194).</summary>
    public static class SendStallRecovery
    {
        /// <summary>
        /// A worker that isn't running is replaced, whatever the transport. A blocked write or a JSON
        /// response that never ends is cleared by a new session when there is one to reconnect. Over
        /// RS-232 the current worker is kept: a replacement would block on the same send lock.
        /// </summary>
        public static SendStallAction Choose(SendStallCause cause, bool canReconnect) =>
            cause == SendStallCause.WorkerNotRunning ? SendStallAction.RestartWorker
            : canReconnect ? SendStallAction.Reconnect
            : SendStallAction.KeepWorker;
    }

    /// <summary>
    /// Decides from <see cref="SendQueueSnapshot"/>s whether the codec's send path is stalled (#194):
    /// actions have waited <c>stallAfter</c> with none taken off a queue, or the worker has run one
    /// action that long. Reports the start and end of a stall once each, and allows one recovery
    /// attempt per <c>recoveryInterval</c> while it lasts.
    /// Time comes from the snapshot, so it can be tested without a clock. Thread-safe.
    /// </summary>
    /// <remarks>
    /// Has no Crestron dependencies, so the test project compiles and tests it directly.
    /// </remarks>
    public sealed class SendStallDetector
    {
        private readonly object _sync = new object();
        private readonly TimeSpan _stallAfter;
        private readonly TimeSpan _recoveryInterval;

        private DateTime? _stalledSinceUtc;
        private DateTime _nextRecoveryUtc = DateTime.MinValue;
        private DateTime? _firstRecoveryUtc;

        // Probe completions seen by the last Evaluate, and the count when this stall's first recovery
        // was tried: the stall ends only once a probe completes after that.
        private long _lastSeenProbes;
        private long? _probesAtFirstRecovery;

        /// <param name="stallAfter">How long without progress counts as a stall.</param>
        /// <param name="recoveryInterval">Least time between two recovery attempts.</param>
        public SendStallDetector(TimeSpan stallAfter, TimeSpan recoveryInterval)
        {
            if (stallAfter <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(stallAfter), "must be positive");
            _stallAfter = stallAfter;
            _recoveryInterval = recoveryInterval;
        }

        /// <summary>How long without progress counts as a stall.</summary>
        public TimeSpan StallAfter => _stallAfter;

        /// <summary>True from a <see cref="SendStallTransition.Stalled"/> until the next <see cref="SendStallTransition.Recovered"/>.</summary>
        public bool IsStalled
        {
            get { lock (_sync) return _stalledSinceUtc != null; }
        }

        /// <summary>
        /// Updates the state from <paramref name="snapshot"/>.
        /// </summary>
        /// <param name="snapshot">The send path's current state.</param>
        /// <param name="cause">Why it is stalled (<see cref="SendStallCause.None"/> when it isn't).</param>
        /// <param name="stalledFor">How long the stall has lasted, for <see cref="SendStallTransition.Recovered"/>.</param>
        public SendStallTransition Evaluate(SendQueueSnapshot snapshot, out SendStallCause cause, out TimeSpan stalledFor)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            cause = CauseOf(snapshot);
            var now = snapshot.TakenUtc;

            lock (_sync)
            {
                stalledFor = _stalledSinceUtc == null ? TimeSpan.Zero : now - _stalledSinceUtc.Value;
                _lastSeenProbes = snapshot.ProbeCompletions;

                if (cause != SendStallCause.None)
                {
                    if (_stalledSinceUtc != null)
                        return SendStallTransition.None;

                    _stalledSinceUtc = now;
                    return SendStallTransition.Stalled;
                }

                if (_stalledSinceUtc == null)
                    return SendStallTransition.None;

                // After a recovery attempt, an empty queue proves nothing: the attempt drained it, and an
                // unrelated action finishing proves nothing either. The stall is over only once the
                // watchdog's probe, which waits for the send lock like a real command, has got through
                // since the first attempt: counted, so a probe finishing in the same clock tick counts.
                if (_probesAtFirstRecovery != null && snapshot.ProbeCompletions <= _probesAtFirstRecovery.Value)
                    return SendStallTransition.None;

                _stalledSinceUtc = null;
                _firstRecoveryUtc = null; // the next stall starts without a recovery attempt
                _probesAtFirstRecovery = null;
                return SendStallTransition.Recovered;
            }
        }

        /// <summary>
        /// While stalled, returns true at most once per <c>recoveryInterval</c>: the caller should try to
        /// recover now. Always false when not stalled.
        /// </summary>
        public bool TryAcquireRecovery(DateTime nowUtc)
        {
            lock (_sync)
            {
                if (_stalledSinceUtc == null || nowUtc < _nextRecoveryUtc)
                    return false;

                _nextRecoveryUtc = nowUtc + _recoveryInterval;
                // The first attempt of the stall: later retries must not restart the failure window.
                if (_firstRecoveryUtc == null)
                {
                    _firstRecoveryUtc = nowUtc;
                    // Taken from this tick's Evaluate, before the recovery queues its probe.
                    _probesAtFirstRecovery = _lastSeenProbes;
                }
                return true;
            }
        }

        /// <summary>
        /// True when a recovery was first tried during this stall at least <c>stallAfter</c> ago and the
        /// path is still stalled: recovering didn't help, so the device should show a fault. Measured
        /// from the first attempt, so retries can't postpone it. False when not stalled, or before a
        /// recovery has had time to work.
        /// </summary>
        public bool RecoveryFailed(DateTime nowUtc)
        {
            lock (_sync)
                return _stalledSinceUtc != null && _firstRecoveryUtc != null && nowUtc - _firstRecoveryUtc.Value >= _stallAfter;
        }

        /// <summary>Why <paramref name="snapshot"/> counts as stalled, or <see cref="SendStallCause.None"/>.</summary>
        public SendStallCause CauseOf(SendQueueSnapshot snapshot)
        {
            if (snapshot.BusyFor >= _stallAfter)
                return SendStallCause.WorkerBlocked;

            // Nothing waiting: nothing is stuck, whatever the worker is doing.
            if (!(snapshot.LongestWaiting >= _stallAfter))
                return SendStallCause.None;

            if (snapshot.BusyFor != null)
                return SendStallCause.WorkerBlocked; // busy, but not yet long: the wait is its doing

            // A hold that keeps switching on and off reads as "not holding" at any one moment.
            var heldRecently = snapshot.JsonHoldFor != null || snapshot.SinceJsonHold < _stallAfter;
            return heldRecently ? SendStallCause.JsonHold : SendStallCause.WorkerNotRunning;
        }
    }
}
