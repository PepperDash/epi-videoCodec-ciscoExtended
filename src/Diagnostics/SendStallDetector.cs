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

                if (cause != SendStallCause.None)
                {
                    if (_stalledSinceUtc != null)
                        return SendStallTransition.None;

                    _stalledSinceUtc = now;
                    return SendStallTransition.Stalled;
                }

                if (_stalledSinceUtc == null)
                    return SendStallTransition.None;

                _stalledSinceUtc = null;
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
                return true;
            }
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
