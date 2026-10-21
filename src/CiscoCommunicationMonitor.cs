using System;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// The codec's communication monitor, which can also be held in error (#115). The base monitor
    /// counts any received text as a good reply, and at a serial login prompt the codec echoes
    /// whatever is typed, so the device stayed IsOk with no session. While held, the monitor sends no
    /// polls and ignores received text.
    /// </summary>
    public class CiscoCommunicationMonitor : GenericCommunicationMonitor
    {
        public CiscoCommunicationMonitor(IKeyed parent, IBasicCommunication client, long pollTime,
            long warningTime, long errorTime, Action pollAction)
            : base(parent, client, pollTime, warningTime, errorTime, pollAction)
        {
        }

        /// <summary>True while <see cref="HoldInError"/> is in effect.</summary>
        public bool IsHeld { get; private set; }

        /// <summary>Stops polling and reports InError until <see cref="ReleaseHold"/>.</summary>
        public void HoldInError()
        {
            if (IsHeld)
                return;

            IsHeld = true;
            Stop();
            Status = MonitorStatus.InError;
        }

        /// <summary>Resumes polling; the next reply sets the status again.</summary>
        public void ReleaseHold()
        {
            if (!IsHeld)
                return;

            IsHeld = false;
            Status = MonitorStatus.StatusUnknown;
            Start();
        }

        /// <inheritdoc />
        public override void Start()
        {
            // Activation or a reconnect must not restart polling into a login prompt.
            if (IsHeld)
                return;

            base.Start();
        }
    }
}
