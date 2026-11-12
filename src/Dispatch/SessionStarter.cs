#nullable enable
using System;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Claims the codec's session for the sync and sends what sets it up: echo off, JSON mode and, for a
    /// new session, the tagged full xStatus request. A claim is kept only when that setup actually went
    /// out.
    /// </summary>
    /// <remarks>
    /// <para>An SSH client can deliver the codec's first output (the login banner, a poll echo) a moment
    /// before it reports itself connected, and sends made in that window are dropped. A session claimed
    /// anyway was never set up: nothing asked for the full status again, and the sync waited for its
    /// 60 s retry. So a new session whose setup was dropped stays unclaimed, and the next output the codec
    /// sends starts it again, full status included.</para>
    /// <para>Runs on the sync worker only. Has no Crestron dependencies, so the test project compiles
    /// and tests it directly.</para>
    /// </remarks>
    public sealed class SessionStarter
    {
        private readonly Func<string, bool> _send;
        private readonly Func<bool> _requestFullStatus;
        private readonly Func<bool> _fullStatusPending;

        /// <param name="send">Sends a command now; returns false when it didn't go out.</param>
        /// <param name="requestFullStatus">Sends the tagged full xStatus request; returns false when it didn't go out.</param>
        /// <param name="fullStatusPending">True while a full xStatus request that went out still waits for its reply.</param>
        public SessionStarter(Func<string, bool> send, Func<bool> requestFullStatus, Func<bool> fullStatusPending)
        {
            _send = send ?? throw new ArgumentNullException(nameof(send));
            _requestFullStatus = requestFullStatus ?? throw new ArgumentNullException(nameof(requestFullStatus));
            _fullStatusPending = fullStatusPending ?? throw new ArgumentNullException(nameof(fullStatusPending));
        }

        /// <summary>The sync has claimed the codec's session (the login was seen and its setup sent).</summary>
        public bool Claimed { get; set; }

        /// <summary>
        /// Claims the session if it isn't yet and, unless JSON mode is already confirmed, sends echo off
        /// and JSON mode, plus the tagged full status for a new session that hasn't had one and isn't
        /// waiting for one (a setup started again after only part of it was dropped asks only once).
        /// </summary>
        /// <returns>What happened: whether the session was new, and whether its setup was dropped (then
        /// it stays unclaimed).</returns>
        public SessionStartResult Start(bool jsonModeSet, bool fullStatusReceived)
        {
            var isNew = !Claimed;
            Claimed = true;

            if (jsonModeSet)
                return new SessionStartResult(isNew, setupDropped: false);

            // Every send is tried: a partial setup would leave the session in an unknown state.
            var sent = _send("echo off");
            sent &= _send("xPreferences outputmode json");
            if (isNew)
                sent &= RequestFullStatusIfNeeded(fullStatusReceived);

            return Settle(isNew, sent);
        }

        /// <summary>
        /// JSON arrived from a session the sync hadn't claimed (one set up before a program restart, or
        /// one whose setup was only partly sent): claims it, sends echo off (it may still have echo on)
        /// and requests the full status unless it has it or is waiting for it, since the JSON may be a
        /// poll reply that leaves call, standby and presentation state unloaded.
        /// </summary>
        public SessionStartResult Adopt(bool fullStatusReceived)
        {
            Claimed = true;
            var sent = _send("echo off");
            sent &= RequestFullStatusIfNeeded(fullStatusReceived);
            return Settle(isNew: true, sent);
        }

        // A request that went out and still waits counts as sent: asking again would only make the codec
        // send its large status twice.
        private bool RequestFullStatusIfNeeded(bool fullStatusReceived) =>
            fullStatusReceived || _fullStatusPending() || _requestFullStatus();

        private SessionStartResult Settle(bool isNew, bool sent)
        {
            // Only a new session gives the claim back: an existing one keeps it (its setup went out
            // before), and a retry of echo off or JSON mode that is dropped is retried by the sync.
            var dropped = isNew && !sent;
            if (dropped)
                Claimed = false;
            return new SessionStartResult(isNew, dropped);
        }
    }

    /// <summary>What <see cref="SessionStarter.Start"/> or <see cref="SessionStarter.Adopt"/> did.</summary>
    public readonly struct SessionStartResult
    {
        public SessionStartResult(bool isNewSession, bool setupDropped)
        {
            IsNewSession = isNewSession;
            SetupDropped = setupDropped;
        }

        /// <summary>The session wasn't claimed before this call.</summary>
        public bool IsNewSession { get; }

        /// <summary>A new session's setup didn't go out (the connection reported not connected), so it
        /// was left unclaimed for the next codec output to start again.</summary>
        public bool SetupDropped { get; }
    }
}
