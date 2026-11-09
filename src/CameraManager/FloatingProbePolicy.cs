#nullable enable
using System;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{
    /// <summary>
    /// Pacing for floating-camera recovery probes. Each probe moves the camera's switch port to a
    /// candidate codec's VLAN and power-cycles the camera, so probes must be few and far apart.
    /// <para>
    /// The first full pass over the candidate codecs runs at the base delay (one probe per codec).
    /// If that pass did not find the camera, each further probe waits twice as long as the one
    /// before, capped at the max delay.
    /// </para>
    /// </summary>
    public static class FloatingProbePolicy
    {
        /// <summary>
        /// Delay before the probe that follows probe number <paramref name="attempt"/>.
        /// </summary>
        /// <param name="attempt">1-based number of the probe just issued.</param>
        /// <param name="probeTotal">Number of candidate codecs in one full pass.</param>
        /// <param name="baseDelay">Delay between probes during the first pass.</param>
        /// <param name="maxDelay">Upper bound on the delay.</param>
        public static TimeSpan GetRetryDelay(int attempt, int probeTotal, TimeSpan baseDelay, TimeSpan maxDelay)
        {
            if (maxDelay < baseDelay)
            {
                maxDelay = baseDelay;
            }

            var firstPass = Math.Max(probeTotal, 1);
            if (attempt <= firstPass)
            {
                return baseDelay;
            }

            var ticks = baseDelay.Ticks;
            for (var i = firstPass; i < attempt && ticks < maxDelay.Ticks; i++)
            {
                ticks *= 2;
            }

            return ticks > maxDelay.Ticks ? maxDelay : TimeSpan.FromTicks(ticks);
        }

        /// <summary>
        /// Picks the codec for probe number <paramref name="attempt"/>. Held codecs (in outage) are
        /// removed first, then the remaining candidates are rotated in order, so with A, B, C and
        /// B held the probes go A, C, A, C rather than repeating C. <paramref name="probeTotal"/> is
        /// the number of usable candidates, which is also the length of one full pass for pacing.
        /// </summary>
        /// <param name="candidates">Candidate codec keys in preference order.</param>
        /// <param name="isHeld">True for a codec that must not be probed through.</param>
        /// <param name="attempt">1-based probe number.</param>
        /// <param name="probeIndex">1-based position of the selection among the usable candidates, 0 when none.</param>
        /// <param name="probeTotal">Number of usable candidates.</param>
        /// <returns>The selected codec key, or null when no candidate is usable.</returns>
        public static string? SelectProbeCodec(
            IReadOnlyList<string> candidates,
            Func<string, bool> isHeld,
            int attempt,
            out int probeIndex,
            out int probeTotal)
        {
            var usable = new List<string>();
            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate) && !isHeld(candidate))
                {
                    usable.Add(candidate);
                }
            }

            probeTotal = usable.Count;
            if (usable.Count == 0 || attempt <= 0)
            {
                probeIndex = 0;
                return null;
            }

            var selectedIndex = (attempt - 1) % usable.Count;
            probeIndex = selectedIndex + 1;
            return usable[selectedIndex];
        }
    }
}
