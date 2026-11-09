#nullable enable
using System;

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
    }
}
