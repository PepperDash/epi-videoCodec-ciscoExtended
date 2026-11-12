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
        private readonly Dictionary<string, bool> _inCall = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Records <paramref name="inCall"/> for <paramref name="codecKey"/>. True only when the previous
        /// reading for that codec was "not in a call" and this one is "in a call".
        /// </summary>
        public bool Observe(string codecKey, bool inCall)
        {
            if (string.IsNullOrEmpty(codecKey))
                return false;

            lock (_lock)
            {
                var known = _inCall.TryGetValue(codecKey, out var wasInCall);
                _inCall[codecKey] = inCall;
                return known && !wasInCall && inCall;
            }
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
                _inCall.Remove(codecKey);
        }
    }
}
