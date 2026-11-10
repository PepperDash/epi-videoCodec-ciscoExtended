#nullable enable
using System;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{
    /// <summary>
    /// Remembers when the camera manager itself last touched a camera's switch port (PoE off/on or
    /// a VLAN change, which some switch drivers do with a PoE cycle). For a boot window after that,
    /// the camera is expected to be offline while it reboots, so floating recovery must not treat
    /// it as lost; otherwise the next probe's PoE cycle restarts the boot and recovery feeds itself.
    /// Ports are compared case-insensitively. Thread-safe.
    /// </summary>
    public sealed class CameraBootWindow
    {
        private readonly object sync = new object();
        private readonly Dictionary<string, DateTime> lastPortActionUtc = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan window;

        /// <summary>Creates the tracker with the given boot window.</summary>
        public CameraBootWindow(TimeSpan window)
        {
            this.window = window < TimeSpan.Zero ? TimeSpan.Zero : window;
        }

        /// <summary>The boot window length.</summary>
        public TimeSpan Window => window;

        /// <summary>Records that the manager sent a power-affecting command to the port.</summary>
        public void RecordPortAction(string port, DateTime nowUtc)
        {
            var key = Normalize(port);
            if (key == null)
            {
                return;
            }

            lock (sync)
            {
                lastPortActionUtc[key] = nowUtc;
            }
        }

        /// <summary>
        /// A switch command on the port completed. If the manager has acted on this port before,
        /// restart its boot window from now: a queued PoE-on (or a VLAN change done with a PoE
        /// cycle) can complete long after it was sent, and the camera only starts booting when
        /// power actually returns. Ports the manager never touched are ignored, so the switch
        /// reporting a port's current state (e.g. at startup) does not open a window.
        /// </summary>
        /// <returns>True when the window was restarted.</returns>
        public bool RefreshOnCompletion(string port, DateTime nowUtc)
        {
            var key = Normalize(port);
            if (key == null)
            {
                return false;
            }

            lock (sync)
            {
                if (!lastPortActionUtc.TryGetValue(key, out var last))
                {
                    return false;
                }

                if (nowUtc > last)
                {
                    lastPortActionUtc[key] = nowUtc;
                }

                return true;
            }
        }

        /// <summary>
        /// True while the camera on <paramref name="port"/> may still be booting from a manager
        /// port action; <paramref name="bootingUntilUtc"/> is when the window closes.
        /// </summary>
        public bool IsBooting(string port, DateTime nowUtc, out DateTime bootingUntilUtc)
        {
            bootingUntilUtc = DateTime.MinValue;
            var key = Normalize(port);
            if (key == null || window == TimeSpan.Zero)
            {
                return false;
            }

            lock (sync)
            {
                if (!lastPortActionUtc.TryGetValue(key, out var last))
                {
                    return false;
                }

                bootingUntilUtc = last + window;
                return nowUtc < bootingUntilUtc;
            }
        }

        private static string? Normalize(string port)
        {
            return string.IsNullOrWhiteSpace(port) ? null : port.Trim();
        }
    }
}
