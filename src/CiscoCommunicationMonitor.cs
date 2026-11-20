using System;
using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>Why the communication monitor is held in error.</summary>
    public enum CommunicationHoldReason
    {
        /// <summary>The RS-232 session is at a login prompt (#115).</summary>
        SerialLoginPrompt,

        /// <summary>Commands to the codec are not going out, and recovering didn't help (#194).</summary>
        SendStalled
    }

    /// <summary>
    /// The codec's communication monitor, which can also be held in error (#115, #194). The base
    /// monitor counts any received text as a good reply, so the device stayed IsOk at a serial login
    /// prompt (the codec echoes whatever is typed) and while commands to the codec weren't going out
    /// (status kept arriving). While held, the monitor sends no polls and ignores received text.
    /// A hold has a reason, and the monitor stays held until every reason is released, so ending one
    /// condition can't hide another.
    /// </summary>
    public class CiscoCommunicationMonitor : GenericCommunicationMonitor
    {
        private readonly object _holdLock = new object();
        private readonly HashSet<CommunicationHoldReason> _holds = new HashSet<CommunicationHoldReason>();

        public CiscoCommunicationMonitor(IKeyed parent, IBasicCommunication client, long pollTime,
            long warningTime, long errorTime, Action pollAction)
            : base(parent, client, pollTime, warningTime, errorTime, pollAction)
        {
        }

        /// <summary>True while any hold is in effect.</summary>
        public bool IsHeld
        {
            get { lock (_holdLock) return _holds.Count > 0; }
        }

        /// <summary>Whether a hold for <paramref name="reason"/> is in effect.</summary>
        public bool IsHeldFor(CommunicationHoldReason reason)
        {
            lock (_holdLock) return _holds.Contains(reason);
        }

        /// <summary>Holds the monitor for the RS-232 login prompt (#115).</summary>
        public void HoldInError() => HoldInError(CommunicationHoldReason.SerialLoginPrompt);

        /// <summary>Releases the RS-232 login prompt hold (#115).</summary>
        public void ReleaseHold() => ReleaseHold(CommunicationHoldReason.SerialLoginPrompt);

        /// <summary>Stops polling and reports InError until every reason is released.</summary>
        public void HoldInError(CommunicationHoldReason reason)
        {
            // Stop/Start and the status change stay inside the lock, so a hold and a release on two
            // threads can't interleave. Nothing in the base monitor takes this lock.
            lock (_holdLock)
            {
                if (!_holds.Add(reason) || _holds.Count > 1)
                    return; // already held for this reason, or already held for another

                Stop();
                Status = MonitorStatus.InError;
            }
        }

        /// <summary>Releases one reason; once none is left, polling resumes and the next reply sets the status.</summary>
        public void ReleaseHold(CommunicationHoldReason reason)
        {
            lock (_holdLock)
            {
                if (!_holds.Remove(reason) || _holds.Count > 0)
                    return; // wasn't held for this reason, or still held for another

                Status = MonitorStatus.StatusUnknown;
                Start();
            }
        }

        /// <inheritdoc />
        public override void Start()
        {
            // Activation or a reconnect must not restart polling while held.
            if (IsHeld)
                return;

            base.Start();
        }
    }
}
