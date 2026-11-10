#nullable enable
using System;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{
    /// <summary>
    /// Result of feeding a switch event (or a clock tick) into <see cref="SwitchCommandHealth"/>.
    /// </summary>
    public enum SwitchHealthTransition
    {
        /// <summary>No change in health.</summary>
        None,

        /// <summary>The switch session just started failing.</summary>
        Failing,

        /// <summary>A switch command just succeeded after a failure.</summary>
        Recovered
    }

    /// <summary>
    /// Tracks whether the network switch session is completing commands, from the switch's own
    /// port events, so the camera manager can stop starting floating-recovery probes while the
    /// switch is not answering.
    /// <para>
    /// A switch driver raises an "in progress" event when a port command session starts and a
    /// completion event (VlanChanged, PoEEnabled, PoEDisabled) when it succeeds. A session that
    /// times out or is aborted raises no completion. So:
    /// <list type="bullet">
    /// <item>a new session starting while the previous one never completed counts as a failure;</item>
    /// <item>a session with no completion after <c>staleAfter</c> counts as a failure;</item>
    /// <item>any completion clears the failures (the switch is answering again).</item>
    /// </list>
    /// A driver that raises no "in progress" events is never seen as failing.
    /// </para>
    /// <para>
    /// While failing, <see cref="TryAcquireProbe"/> allows one trial probe per back-off window
    /// (base * 2^(failures-1), capped at max), so the switch is not flooded and a recovered switch
    /// is still noticed. Thread-safe.
    /// </para>
    /// </summary>
    public sealed class SwitchCommandHealth
    {
        private readonly object sync = new object();
        private readonly TimeSpan staleAfter;
        private readonly TimeSpan baseBackoff;
        private readonly TimeSpan maxBackoff;

        private DateTime? pendingSinceUtc;
        private int consecutiveFailures;
        private DateTime nextTrialUtc = DateTime.MinValue;
        private DateTime failingSinceUtc = DateTime.MinValue;

        /// <summary>
        /// Creates the tracker.
        /// </summary>
        /// <param name="staleAfter">How long a started session may go without completing before it counts as failed.</param>
        /// <param name="baseBackoff">Back-off after the first failure.</param>
        /// <param name="maxBackoff">Upper bound on the back-off.</param>
        public SwitchCommandHealth(TimeSpan staleAfter, TimeSpan baseBackoff, TimeSpan maxBackoff)
        {
            this.staleAfter = staleAfter;
            this.baseBackoff = baseBackoff;
            this.maxBackoff = maxBackoff < baseBackoff ? baseBackoff : maxBackoff;
        }

        /// <summary>True while at least one switch command has failed since the last success.</summary>
        public bool IsFailing
        {
            get { lock (sync) { return consecutiveFailures > 0; } }
        }

        /// <summary>Failures since the last successful command.</summary>
        public int ConsecutiveFailures
        {
            get { lock (sync) { return consecutiveFailures; } }
        }

        /// <summary>UTC time the current failing episode began, or <see cref="DateTime.MinValue"/>.</summary>
        public DateTime FailingSinceUtc
        {
            get { lock (sync) { return consecutiveFailures > 0 ? failingSinceUtc : DateTime.MinValue; } }
        }

        /// <summary>Current back-off window for the failure count.</summary>
        public TimeSpan CurrentBackoff
        {
            get { lock (sync) { return BackoffFor(consecutiveFailures); } }
        }

        /// <summary>A switch command session started (the driver's "in progress" event).</summary>
        public SwitchHealthTransition OnCommandStarted(DateTime nowUtc)
        {
            lock (sync)
            {
                var transition = SwitchHealthTransition.None;
                if (pendingSinceUtc.HasValue)
                {
                    // The previous session never completed: it timed out or was aborted.
                    transition = RecordFailure(nowUtc);
                }

                pendingSinceUtc = nowUtc;
                return transition;
            }
        }

        /// <summary>A switch command completed successfully.</summary>
        public SwitchHealthTransition OnCommandCompleted(DateTime nowUtc)
        {
            lock (sync)
            {
                pendingSinceUtc = null;
                var wasFailing = consecutiveFailures > 0;
                consecutiveFailures = 0;
                nextTrialUtc = DateTime.MinValue;
                failingSinceUtc = DateTime.MinValue;
                return wasFailing ? SwitchHealthTransition.Recovered : SwitchHealthTransition.None;
            }
        }

        /// <summary>Clock tick: counts a session that has gone too long without completing as failed.</summary>
        public SwitchHealthTransition Evaluate(DateTime nowUtc)
        {
            lock (sync)
            {
                if (pendingSinceUtc.HasValue && nowUtc - pendingSinceUtc.Value >= staleAfter)
                {
                    pendingSinceUtc = null;
                    return RecordFailure(nowUtc);
                }

                return SwitchHealthTransition.None;
            }
        }

        /// <summary>
        /// Asks to start a floating-recovery probe. Always granted while the switch is healthy.
        /// While failing, granted once per back-off window as a trial; <paramref name="retryAtUtc"/>
        /// is when the next trial may run.
        /// </summary>
        public bool TryAcquireProbe(DateTime nowUtc, out DateTime retryAtUtc)
        {
            lock (sync)
            {
                if (consecutiveFailures == 0)
                {
                    retryAtUtc = nowUtc;
                    return true;
                }

                if (nowUtc < nextTrialUtc)
                {
                    retryAtUtc = nextTrialUtc;
                    return false;
                }

                nextTrialUtc = nowUtc + BackoffFor(consecutiveFailures);
                retryAtUtc = nextTrialUtc;
                return true;
            }
        }

        private SwitchHealthTransition RecordFailure(DateTime nowUtc)
        {
            var wasHealthy = consecutiveFailures == 0;
            consecutiveFailures++;
            if (wasHealthy)
            {
                failingSinceUtc = nowUtc;
            }

            var candidate = nowUtc + BackoffFor(consecutiveFailures);
            if (candidate > nextTrialUtc)
            {
                nextTrialUtc = candidate;
            }

            return wasHealthy ? SwitchHealthTransition.Failing : SwitchHealthTransition.None;
        }

        private TimeSpan BackoffFor(int failures)
        {
            if (failures <= 0)
            {
                return TimeSpan.Zero;
            }

            var ticks = baseBackoff.Ticks;
            for (var i = 1; i < failures && ticks < maxBackoff.Ticks; i++)
            {
                ticks *= 2;
            }

            return ticks > maxBackoff.Ticks ? maxBackoff : TimeSpan.FromTicks(ticks);
        }
    }
}
