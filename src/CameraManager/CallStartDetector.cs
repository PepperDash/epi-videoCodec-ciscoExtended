#nullable enable
using System;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Tells when a codec goes from no active call to an active call, from repeated readings of its
    /// in-call state (the codec's active-call count, which includes Teams and other platform calls).
    /// </summary>
    /// <remarks>
    /// <para>The first reading for a codec only records its state: after a program restart during a
    /// meeting, the call is already up, and acting on it would change the camera under the meeting.</para>
    /// <para>Thread-safe. Has no Crestron dependencies, so the test project compiles and tests it directly.</para>
    /// </remarks>
    public sealed class CallStartDetector
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, Reading> _last = new Dictionary<string, Reading>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Records <paramref name="inCall"/> for <paramref name="codecKey"/>. True only when the previous
        /// reading for that codec, in the same codec session, was "not in a call" and this one is "in a call".
        /// </summary>
        /// <param name="codecKey">The codec.</param>
        /// <param name="inCall">Whether the codec has an active call.</param>
        /// <param name="session">The codec's session (its sync generation). A reading from another session
        /// only records the state, like a first reading: the codec may have disconnected and re-synced
        /// between two readings, and a call that was up all along would look like a new one.</param>
        public bool Observe(string codecKey, bool inCall, int session = 0)
        {
            if (string.IsNullOrEmpty(codecKey))
                return false;

            lock (_lock)
            {
                var known = _last.TryGetValue(codecKey, out var last) && last.Session == session;
                _last[codecKey] = new Reading(inCall, session);
                return known && !last.InCall && inCall;
            }
        }

        private readonly struct Reading
        {
            public Reading(bool inCall, int session)
            {
                InCall = inCall;
                Session = session;
            }

            public bool InCall { get; }
            public int Session { get; }
        }

        /// <summary>
        /// Forgets a codec's last reading, for example while it is offline: its next reading only records
        /// the state again, so a call already up when it comes back doesn't count as a new one.
        /// </summary>
        public void Forget(string codecKey)
        {
            if (string.IsNullOrEmpty(codecKey))
                return;

            lock (_lock)
                _last.Remove(codecKey);
        }
    }
}
