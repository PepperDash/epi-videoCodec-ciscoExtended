#nullable enable
using System;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{
    /// <summary>
    /// Where a camera serial is connected according to the codecs' own camera-status records.
    /// </summary>
    public readonly struct CameraPresenceMatch
    {
        /// <summary>Creates a match.</summary>
        public CameraPresenceMatch(string codecKey, uint cameraId)
        {
            CodecKey = codecKey;
            CameraId = cameraId;
        }

        /// <summary>Codec reporting the camera connected.</summary>
        public string CodecKey { get; }

        /// <summary>Slot the codec reports it in (0 when unknown).</summary>
        public uint CameraId { get; }
    }

    /// <summary>Result of <see cref="CameraPresence.Evaluate"/>.</summary>
    public enum CameraPresenceState
    {
        /// <summary>Connected on its target codec (any slot).</summary>
        OnTarget,

        /// <summary>Connected on a codec other than its target.</summary>
        OnOtherCodec,

        /// <summary>Every codec was sampled and none reports it connected.</summary>
        Absent,

        /// <summary>Not found, but at least one codec could not be sampled: no conclusion.</summary>
        Unknown
    }

    /// <summary>
    /// Allows an action at most once per interval per key (e.g. one camera-status refresh per codec
    /// per minute). Thread-safe.
    /// </summary>
    public sealed class KeyedRateLimiter
    {
        private readonly object sync = new object();
        private readonly Dictionary<string, DateTime> lastUtc = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan interval;

        /// <summary>Creates the limiter.</summary>
        public KeyedRateLimiter(TimeSpan interval)
        {
            this.interval = interval;
        }

        /// <summary>True (and records the time) when the key has not been allowed within the interval.</summary>
        public bool TryAcquire(string key, DateTime nowUtc)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            lock (sync)
            {
                if (lastUtc.TryGetValue(key, out var last) && nowUtc - last < interval)
                {
                    return false;
                }

                lastUtc[key] = nowUtc;
                return true;
            }
        }
    }

    /// <summary>
    /// Finds a camera by serial in the codecs' camera-status records, in any slot. The camera
    /// manager's own view (control wrappers matched by configured slot) can miss a camera that is
    /// connected in a different slot than configured; before probing a "lost" camera the manager
    /// checks here, so a camera that is actually connected is never power-cycled.
    /// </summary>
    public static class CameraPresence
    {
        /// <summary>
        /// Classifies where a camera is, from the codecs' camera-status records.
        /// <para>
        /// When not every codec could be sampled (<paramref name="allCodecsSampled"/> false), not
        /// finding the camera proves nothing: it may be connected on the codec that was missed. That
        /// case is <see cref="CameraPresenceState.Unknown"/>, never <see cref="CameraPresenceState.Absent"/>,
        /// so the caller defers instead of power-cycling a camera that may be connected.
        /// </para>
        /// </summary>
        public static CameraPresenceState Evaluate(
            IEnumerable<KeyValuePair<string, IReadOnlyList<CodecCameraStatusSample>>> codecs,
            bool allCodecsSampled,
            string serialNumber,
            string? targetCodecKey,
            out CameraPresenceMatch match)
        {
            if (TryFindConnected(codecs, serialNumber, targetCodecKey, out match))
            {
                return string.Equals(match.CodecKey, targetCodecKey, StringComparison.OrdinalIgnoreCase)
                    ? CameraPresenceState.OnTarget
                    : CameraPresenceState.OnOtherCodec;
            }

            return allCodecsSampled ? CameraPresenceState.Absent : CameraPresenceState.Unknown;
        }

        /// <summary>
        /// Looks for <paramref name="serialNumber"/> connected on any codec, preferring
        /// <paramref name="preferredCodecKey"/> (the camera's target) when several report it.
        /// </summary>
        /// <param name="codecs">Codec key to that codec's camera-status records.</param>
        /// <param name="serialNumber">Camera serial to look for.</param>
        /// <param name="preferredCodecKey">Codec to prefer when more than one reports the serial.</param>
        /// <param name="match">Where the camera was found.</param>
        /// <returns>True when a codec reports the serial connected.</returns>
        public static bool TryFindConnected(
            IEnumerable<KeyValuePair<string, IReadOnlyList<CodecCameraStatusSample>>> codecs,
            string serialNumber,
            string? preferredCodecKey,
            out CameraPresenceMatch match)
        {
            match = default;
            var serial = serialNumber?.Trim();
            if (string.IsNullOrEmpty(serial))
            {
                return false;
            }

            var found = false;
            foreach (var codec in codecs)
            {
                if (codec.Value == null)
                {
                    continue;
                }

                foreach (var camera in codec.Value)
                {
                    if (!camera.Connected
                        || !string.Equals(camera.SerialNumber?.Trim(), serial, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (string.Equals(codec.Key, preferredCodecKey, StringComparison.OrdinalIgnoreCase))
                    {
                        match = new CameraPresenceMatch(codec.Key, camera.CameraId);
                        return true;
                    }

                    if (!found)
                    {
                        match = new CameraPresenceMatch(codec.Key, camera.CameraId);
                        found = true;
                    }
                }
            }

            return found;
        }
    }
}
